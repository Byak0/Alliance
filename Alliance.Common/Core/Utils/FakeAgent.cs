using System;
using System.Collections.Generic;
using System.Linq;
using Alliance.Common.Patch.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

#if !SERVER
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
#endif
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// (WIP) 
	/// A "fake" agent is a GameEntity dressed as a character, which can move and play raw animation clips.
	/// Used in cinematics, modding kit previews, revive system, as fake soldier, etc.
	/// Animation are raw clips driven by <see cref="TickAll"/>.
	/// Most data used comes from <see cref="AllianceData"/>.
	/// Fake agents can be created in two ways, chosen once in the constructor:
	/// - Full visuals (client only, requires a Game, bounded to <see cref="MaxAgentVisualsFakes"/>):
	///   native AgentVisuals from AgentVisualsData with procedural body, facial animations, native weapon placement.
	/// - Simple skeleton (default, cheaper cost): 
	///   a raw skeleton wearing body/clothing meshes + rigid weapons bound once to hand/holster bones.
	/// </summary>
	public class FakeAgent
	{
		private const string DefaultIdleClip = "inventory_idle";
		private const string DefaultMountedIdleClip = "horse_rider_stand_1";
		private const string DefaultMountIdleClip = "horse_stand_1";
		private const string DefaultMountWalkClip = "horse_walkfast";
		private const string DefaultMountTrotClip = "horse_gait_trot_2";
		private const float DefaultWalkSpeed = 1.6f;
		private const float MaxClipOffset = 0.1f;
		private const float MaxFastClipOffset = 0.01f;
		private const float MaxMoveClipOffset = 0.5f;

		// Standard action types shared by every humanoid action set
		private const string BaseIdleAction = "act_inventory_idle";
		private const string BaseWalkAction = "act_walk_forward_unarmed";
		private const string BaseRunAction = "act_run_forward_unarmed";
		private const string HumanActionSet = "as_human_warrior";
		// Fakes farther than this from the camera tick their clip loopers at a reduced rate
		private const float FarCameraDistance = 100f;
		// Neutral adult body type used by TaleWorlds' CharacterSpawner
		private const string DefaultBodyPropertiesString = "<BodyProperties version=\"4\" age=\"23.16\" weight=\"0.3333\" build=\"0\" key=\"00000C07000000010011111211151111000701000010000000111011000101000000500202111110000000000000000000000000000000000000000000A00000\" />";

		private static readonly List<FakeAgent> _activeAgents = new List<FakeAgent>();

		// Cached measured rider seat per mount skeleton.
		private static readonly Dictionary<string, MatrixFrame> _seatByMountSkeleton = new Dictionary<string, MatrixFrame>();

		/// <summary>The three default clips (idle / walk / run) a skeleton or a rider can play.</summary>
		private readonly struct DefaultClips
		{
			public DefaultClips(string idle, string walk, string run) { Idle = idle; Walk = walk; Run = run; }
			public string Idle { get; }
			public string Walk { get; }
			public string Run { get; }
			/// <summary>Move clip by pace; falls back down to the idle.</summary>
			public string Gait(bool fast) => fast ? (Run ?? Walk ?? Idle) : (Walk ?? Idle);
		}

		private static readonly Dictionary<string, DefaultClips> _bodyClipsByActionSet = new Dictionary<string, DefaultClips>(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, (DefaultClips Mount, DefaultClips Rider)> _mountedClipsByActionSet = new Dictionary<string, (DefaultClips, DefaultClips)>(StringComparer.OrdinalIgnoreCase);

		private static DefaultClips ResolveBodyClips(string actionSetId)
		{
			if (string.IsNullOrEmpty(actionSetId)) actionSetId = HumanActionSet;
			if (!_bodyClipsByActionSet.TryGetValue(actionSetId, out DefaultClips clips))
			{
				clips = new DefaultClips(
					AllianceData.Instance.GetAnimationOfAction(actionSetId, BaseIdleAction) ?? AllianceData.Instance.GetAnimationOfAction(HumanActionSet, BaseIdleAction) ?? DefaultIdleClip,
					AllianceData.Instance.GetAnimationOfAction(actionSetId, BaseWalkAction) ?? AllianceData.Instance.GetAnimationOfAction(HumanActionSet, BaseWalkAction),
					AllianceData.Instance.GetAnimationOfAction(actionSetId, BaseRunAction) ?? AllianceData.Instance.GetAnimationOfAction(HumanActionSet, BaseRunAction));
				_bodyClipsByActionSet[actionSetId] = clips;
				Log($"[FakeAgent] body clips for '{actionSetId}': idle '{clips.Idle}', walk '{clips.Walk}', run '{clips.Run}'.", LogLevel.Debug);
			}
			return clips;
		}

		// Native paces of the monster_usage movement data: 1 = stand, 2 = walk, 3 = trot,
		// 4 = canter, 5 = gallop. The fake's run speed (2x walk) matches a trot.
		private const int UsagePaceStand = 1;
		private const int UsagePaceWalk = 2;
		private const int UsagePaceTrot = 3;

		private static (DefaultClips Mount, DefaultClips Rider) ResolveMountedClips(string mountActionSetId, string riderActionSetId, string usageId)
		{
			string cacheKey = $"{mountActionSetId}|{riderActionSetId}|{usageId}";
			if (!_mountedClipsByActionSet.TryGetValue(cacheKey, out (DefaultClips Mount, DefaultClips Rider) resolved))
			{
				AllianceData data = AllianceData.Instance;
				AllianceData.MonsterUsageDefinition usage = data.GetMonsterUsage(usageId);
				string idleAction = usage?.GetUpperBodyAction(UsagePaceStand);
				string walkAction = usage?.GetMovementAction(UsagePaceWalk);
				string runAction = usage?.GetMovementAction(UsagePaceTrot);
				resolved.Mount = new DefaultClips(
					data.GetAnimationOfAction(mountActionSetId, idleAction) ?? OptionalClip(DefaultMountIdleClip),
					data.GetAnimationOfAction(mountActionSetId, walkAction) ?? OptionalClip(DefaultMountWalkClip),
					data.GetAnimationOfAction(mountActionSetId, runAction) ?? OptionalClip(DefaultMountTrotClip));
				resolved.Rider = new DefaultClips(
					data.GetAnimationOfAction(riderActionSetId, idleAction) ?? OptionalClip(DefaultMountedIdleClip),
					data.GetAnimationOfAction(riderActionSetId, walkAction),
					data.GetAnimationOfAction(riderActionSetId, runAction));
				_mountedClipsByActionSet[cacheKey] = resolved;
				Log($"[FakeAgent] mounted clips for '{cacheKey}': mount (idle '{resolved.Mount.Idle}', walk '{resolved.Mount.Walk}', run '{resolved.Mount.Run}'), rider (idle '{resolved.Rider.Idle}', walk '{resolved.Rider.Walk}', run '{resolved.Rider.Run}').", LogLevel.Debug);
			}
			return resolved;
		}

		/// <summary>Legacy native default clip, accepted only when it actually exists.</summary>
		private static string OptionalClip(string clipName) => AllianceData.Instance.ClipExists(clipName) ? clipName : null;

		// Shared parent per scene (a mission and a tableau scene can hold fakes at the same time)
		private static readonly Dictionary<Scene, GameEntity> _roots = new Dictionary<Scene, GameEntity>();

		private static GameEntity GetOrCreateRoot(Scene scene)
		{
			// WeakEntity.IsValid detects entities destroyed with their scene.
			if (!_roots.TryGetValue(scene, out GameEntity root) || root == null || !root.WeakEntity.IsValid)
			{
				// Prune roots destroyed with their scene - the dictionary would otherwise pin them.
				List<Scene> deadScenes = null;
				foreach (KeyValuePair<Scene, GameEntity> entry in _roots)
				{
					if (entry.Value == null || !entry.Value.WeakEntity.IsValid)
						(deadScenes ??= new List<Scene>()).Add(entry.Key);
				}
				if (deadScenes != null)
				{
					foreach (Scene dead in deadScenes) _roots.Remove(dead);
				}

				root = GameEntity.CreateEmpty(scene, true, false, false);
				root.EntityFlags |= EntityFlags.DontSaveToScene;
				root.Name = "FakeAgents";
				_roots[scene] = root;
			}
			return root;
		}

		private GameEntity _body;
		private GameEntity _mountEntity;
		private readonly Scene _scene;
		private bool _registered = true;
#if !SERVER
		private const int MaxAgentVisualsFakes = 64;
		private static int _agentVisualsCount;
		private TaleWorlds.MountAndBlade.View.AgentVisuals _agentVisuals;
		private TaleWorlds.MountAndBlade.View.AgentVisuals _mountVisuals;
		private bool _usesAgentVisuals;
		private bool _usesMountVisuals;
#endif
		private readonly float _walkSpeed;
		private readonly float _runSpeed;
		private readonly float _bodyScale;
		private MatrixFrame _frame;
		private Vec3? _moveDestination;
		private float _moveSpeed;
		private bool _movingWithClip;
		private bool _followGround;
		private ClipLooper _riderLoop;
		private ClipLooper _mountLoop;
		private readonly string _cultureId;
		private string _bodyActionSetId;
		private DefaultClips? _bodyClips;
		private (DefaultClips Mount, DefaultClips Rider)? _mountedClips;
		/// <summary>Static mount slot for preview fakes (native inventory convention): the mount
		/// spawns there beside the rider instead of carrying it.</summary>
		private readonly MatrixFrame? _mountFrame;
		/// <summary>Offset of the unseated preview mount relative to the character frame.</summary>
		private Vec3 _mountLocalOffset;
		private readonly int _lodBiasOverride;
		private bool _seatedOnMount;

		public bool IsValid => _body != null;
		public string Name { get; }

		/// <summary>Entity carrying the fake's world frame: the mount when mounted, the body otherwise.</summary>
		public GameEntity Root => MoveRoot;

		/// <summary>The rider's skeleton (raw clip channel / facial animations). Null when invalid.</summary>
		public Skeleton RiderSkeleton => _body?.Skeleton;

		/// <summary>The rider's action set id (e.g. "as_uruk_warrior") - resolves action-type names
		/// to this race's own clips (AllianceData.GetAnimationOfAction).</summary>
		public string BodyActionSetId => _bodyActionSetId;

#if !SERVER
		/// <summary>Whether the fake was built through the native AgentVisuals pipeline.</summary>
		public bool UsesAgentVisuals => _usesAgentVisuals;

		/// <summary>The rider's AgentVisuals (wielded weapon indices, AnimationSystem playback).
		/// Null on the simple-skeleton path.</summary>
		public TaleWorlds.MountAndBlade.View.AgentVisuals AgentVisuals => _agentVisuals;
#endif

		public override string ToString() => $"[FakeAgent] '{Name}'";

		/// <summary>Creates a fake agent from a character definition.</summary>
		/// <param name="cultureId">Pass a culture to use its colors/sigil.</param>
		/// <param name="useFullAgentVisuals">Use the full native visuals (facial animations...). Costly so only use when necessary.</param>
		/// <param name="weaponsDrawn">Wield weapons in hand instead of carrying them sheathed.</param>
		/// <param name="equipmentSeed">Deterministic pick among the character's equipment rosters.</param>
		/// <param name="bodyPropertiesCode">Body properties for the full-visuals path (default face when null).</param>
		/// <param name="clothColor1">Cloth color overrides (culture colors used when null).</param>
		/// <param name="clothColor2">Cloth color overrides (culture colors used when null).</param>
		/// <param name="register">Register in the global registry ticked by <see cref="TickAll"/>. Pass false
		/// when the owner ticks the fake manually (e.g. menu previews) and must not be removed by <see cref="DespawnAll"/>.</param>
		/// <param name="mountFrame">When set, the mount spawns at this frame BESIDE the rider instead of carrying it
		/// (native inventory-preview convention: standing character + horse next to it). The rider stays on foot; only seated fakes move as a mounted pair.</param>
		/// <param name="lodBiasOverride">When >= 0, replaces the per-mesh-type LOD biases (which force degraded LODs for crowds).</param>
		public FakeAgent(Scene scene, MatrixFrame frame, AllianceData.CharacterDefinition character, string cultureId = null, string name = null, bool useFullAgentVisuals = true, bool weaponsDrawn = false, int equipmentSeed = 0, string bodyPropertiesCode = null, uint? clothColor1 = null, uint? clothColor2 = null, bool register = true, bool swapColors = false, MatrixFrame? mountFrame = null, int lodBiasOverride = -1)
		{
			Name = name ?? $"Fake {character?.Id ?? "human"}";
			_frame = frame;
			_scene = scene;
			_registered = register;
			_mountFrame = mountFrame;
			_lodBiasOverride = lodBiasOverride;

			if (character == null)
			{
				Log($"{this}: no character definition - skipped.", LogLevel.Error);
				return;
			}

			string raceId = character.RaceId;
			AllianceData.SkinDefinition skin = AllianceData.Instance.GetSkin(raceId, character.IsFemale);
			if (string.IsNullOrEmpty(skin?.Skeleton))
			{
				Log($"{this}: no skins.xml entry with a skeleton for race '{raceId}' - skipped.", LogLevel.Error);
				return;
			}

			AllianceData.MonsterDefinition monster = AllianceData.Instance.GetMonster(raceId);
			if (monster == null)
			{
				Log($"{this}: monster '{raceId}' not found in monsters.xml - skipped.", LogLevel.Error);
				return;
			}

			_walkSpeed = monster.WalkingSpeed > 0.01f ? monster.WalkingSpeed : DefaultWalkSpeed;
			if (monster.WalkingSpeed <= 0.01f)
			{
				Log($"{this}: monster '{monster.Id}' has no walking_speed_limit - default {DefaultWalkSpeed} m/s used.", LogLevel.Warning);
			}
			_runSpeed = _walkSpeed * 2f;

			_bodyActionSetId = monster.ActionSetId;
			_bodyClips = ResolveBodyClips(monster.ActionSetId);
			_bodyScale = skin.MinScale > 0f ? skin.MinScale : 1f;

			uint color1 = uint.MaxValue;
			uint color2 = uint.MaxValue;
			_cultureId = string.IsNullOrEmpty(cultureId) ? character.CultureId : cultureId;
			BasicCultureObject culture = null;
			if(Factions.Instance.AvailableCultures.ContainsKey(cultureId))
			{
				culture = Factions.Instance.AvailableCultures[cultureId];
			}
			if (culture != null)
			{
				color1 = swapColors ? culture.Color : culture.Color2;
				color2 = swapColors ? culture.Color2 : culture.Color;
			}
			Dictionary<int, string> slotItems = character.SelectRoster(equipmentSeed);

#if !SERVER
			// Full-visuals path first: native AgentVisuals (procedural body, facial animations, native weapon placement).
			// Each instance is expensive, so their count is limited. Past the limit, fallback to the simple-skeleton path below.
			if (useFullAgentVisuals && Game.Current != null)
			{
				if (_agentVisualsCount >= MaxAgentVisualsFakes)
				{
					Log($"{this}: AgentVisuals budget reached ({MaxAgentVisualsFakes}) - raw entity fallback.", LogLevel.Debug);
				}
				else if (TryCreateWithAgentVisuals(scene, character, slotItems, bodyPropertiesCode, color1, color2, _cultureId))
				{
					_agentVisualsCount++;
					TrySetupMountWithAgentVisuals(scene, slotItems, color1, color2);
					PlayIdle();
					if (_registered) _activeAgents.Add(this);
					return;
				}
			}
#endif

			// Simple-skeleton path: a raw skeleton wearing the character's meshes. Relatively cheap, but no facial animations possible.
			CreateWithSimpleSkeleton(scene, character.SelectResolvedRoster(equipmentSeed), skin, monster, color1, color2, weaponsDrawn);
			
			PlayIdle();
			
			if (_registered) _activeAgents.Add(this);
		}

		/// <summary>Ticks this fake without the global registry - for owners that created it with
		/// register: false (e.g. menu previews driven by their own frame loop).</summary>
		public void TickOnce(float dt, Vec3? cameraOrigin = null) => Tick(dt, cameraOrigin);

#if !SERVER
		// Full-visuals path: builds the fake through the native AgentVisuals pipeline.
		private bool TryCreateWithAgentVisuals(Scene scene, AllianceData.CharacterDefinition character, Dictionary<int, string> slotItems, string bodyPropertiesCode, uint color1, uint color2, string cultureId)
		{
			try
			{
				string raceId = character.RaceId;
				Monster monster = MBObjectManager.Instance?.GetObject<Monster>(raceId);
				if (monster == null)
				{
					Log($"{this}: native monster for race '{raceId}' unavailable - raw fallback.", LogLevel.Error);
					return false;
				}
				// The facegen action-set variant (facial morphs live there).
				MBActionSet actionSet = MBGlobals.GetActionSetWithSuffix(monster, character.IsFemale, "_facegen");
				if (!actionSet.IsValid)
				{
					Log($"{this}: no facegen action set for monster '{raceId}' - raw fallback.", LogLevel.Error);
					return false;
				}

				Equipment equipment = new Equipment();
				foreach (KeyValuePair<int, string> entry in slotItems)
				{
					ItemObject item = MBObjectManager.Instance?.GetObject<ItemObject>(entry.Value);
					if (item == null)
					{
						Log($"{this}: item '{entry.Value}' not found in the object manager.", LogLevel.Warning);
						continue;
					}
					equipment[entry.Key] = new EquipmentElement(item);
				}

				BodyProperties.FromString(!string.IsNullOrEmpty(bodyPropertiesCode) ? bodyPropertiesCode : DefaultBodyPropertiesString, out BodyProperties bodyProperties);
				int race = TaleWorlds.Core.FaceGen.GetRaceOrDefault(raceId);

				_body = GameEntity.CreateEmpty(scene, false);
				GetOrCreateRoot(scene).AddChild(_body);
				_body.SetGlobalFrame(_frame);

				// Culture banner: banner items in the equipment render with the culture's sigils,
				// re-tinted with the (possibly swapped) side colors like the teams' banners.
				Banner banner = BannerToCultureHelper.GetBannerFromCulture(cultureId, color1, color2);

				AgentVisualsData data = new AgentVisualsData()
					.Equipment(equipment)
					.BodyProperties(bodyProperties)
					.Race(race)
					.Frame(_frame)
					.Scale(_bodyScale)
					.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
					.Entity(_body)
					.ActionSet(actionSet)
					.ActionCode(ActionIndexCache.act_inventory_idle_start)
					.Scene(scene)
					.Monster(monster)
					.Banner(banner)
					.ClothColor1(color1)
					.ClothColor2(color2)
					.UseMorphAnims(true)
					.PrepareImmediately(false);
				_agentVisuals = TaleWorlds.MountAndBlade.View.AgentVisuals.Create(data, Name, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
				if (_agentVisuals?.GetVisuals().IsValid() != true)
				{
					Log($"{this}: AgentVisuals creation failed - raw fallback.", LogLevel.Error);
					ReleaseAgentVisuals();
					_body.Remove(0);
					_body = null;
					return false;
				}
				_usesAgentVisuals = true;
				_body.CheckResources(addToQueue: true, checkFaceResources: true);
				return true;
			}
			catch (Exception ex)
			{
				Log($"{this}: AgentVisuals creation failed ({ex.Message}) .", LogLevel.Error);
				ReleaseAgentVisuals();
				if (_body != null)
				{
					_body.Remove(0);
					_body = null;
				}
				return false;
			}
		}

		private bool TrySetupMountWithAgentVisuals(Scene scene, Dictionary<int, string> slotItems, uint color1, uint color2)
		{
			try
			{
				ItemObject mountItem = slotItems.TryGetValue((int)EquipmentIndex.ArmorItemEndSlot, out string mountItemId)
					? MBObjectManager.Instance?.GetObject<ItemObject>(mountItemId)
					: null;
				if (mountItem?.HorseComponent == null) return false;
				ItemObject harnessItem = slotItems.TryGetValue((int)EquipmentIndex.HorseHarness, out string harnessItemId)
					? MBObjectManager.Instance?.GetObject<ItemObject>(harnessItemId)
					: null;
				Monster mountMonster = mountItem.HorseComponent.Monster;
				if (mountMonster == null) return false;

				_mountEntity = GameEntity.CreateEmpty(scene, false);
				_mountEntity.Name = "MountEntity";
				GetOrCreateRoot(scene).AddChild(_mountEntity);
				MatrixFrame mountSpawnFrame = _mountFrame ?? _frame;
				_mountLocalOffset = mountSpawnFrame.origin - _frame.origin;
				_mountEntity.SetGlobalFrame(mountSpawnFrame);

				Equipment mountEquipment = new Equipment();
				mountEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(mountItem);
				if (harnessItem != null) mountEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(harnessItem);

				AgentVisualsData data = new AgentVisualsData()
					.Equipment(mountEquipment)
					.Frame(mountSpawnFrame)
					.Scale(mountItem.ScaleFactor)
					.Entity(_mountEntity)
					.ActionSet(MBGlobals.GetActionSet(mountMonster.ActionSetCode))
					.ActionCode(ActionIndexCache.act_none)
					.Scene(scene)
					.Monster(mountMonster)
					.ClothColor1(color1)
					.ClothColor2(color2)
					.MountCreationKey(TaleWorlds.Core.MountCreationKey.GetRandomMountKeyString(mountItem, TaleWorlds.Library.Common.GetDJB2(Name)))
					.PrepareImmediately(false);
				_mountVisuals = TaleWorlds.MountAndBlade.View.AgentVisuals.Create(data, Name + "_mount", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
				if (_mountVisuals?.GetVisuals().IsValid() != true)
				{
					Log($"{this}: mount AgentVisuals creation failed - mount skipped.", LogLevel.Error);
					ReleaseMountVisuals();
					return false;
				}
				_usesMountVisuals = true;
				_agentVisualsCount++;
				_mountEntity.CheckResources(addToQueue: true, checkFaceResources: false);

				// Pose the mount before measuring its sit bone (a fresh skeleton sits in its meaningless bind pose) -
				// the measured seat is cached per mount skeleton.
				AllianceData.MonsterDefinition mountDefinition = AllianceData.Instance.GetMonster(mountMonster.StringId);
				string mountSkeletonName = AllianceData.Instance.GetSkeletonForActionSet(mountMonster.ActionSetCode);
				_mountedClips = ResolveMountedClips(mountMonster.ActionSetCode, _bodyActionSetId, mountDefinition?.MonsterUsage ?? mountMonster.MonsterUsage);
				PlayMountClip(_mountedClips.Value.Mount.Idle, loop: true);
				if (_mountFrame.HasValue)
				{
					// Preview convention: the mount stands at its own slot, the rider stays on foot.
					return true;
				}
				if (mountDefinition == null || !SeatRiderOnMount(mountSkeletonName, mountDefinition.RiderSitBone))
				{
					// Seating failed: keep the mount but leave the rider standing beside it.
					return false;
				}
				_seatedOnMount = true;
				Log($"{this}: mounted through AgentVisuals.", LogLevel.Debug);
				return true;
			}
			catch (Exception ex)
			{
				Log($"{this}: mount AgentVisuals setup failed ({ex.Message}) - mount skipped.", LogLevel.Error);
				return false;
			}
		}

		private void ReleaseAgentVisuals()
		{
			if (_usesAgentVisuals) _agentVisualsCount--;
			_usesAgentVisuals = false;
			try { _agentVisuals?.Reset(); } catch { }
			_agentVisuals = null;
			ReleaseMountVisuals();
		}

		private void ReleaseMountVisuals()
		{
			if (_usesMountVisuals) _agentVisualsCount--;
			_usesMountVisuals = false;
			try { _mountVisuals?.Reset(); } catch { }
			_mountVisuals = null;
		}
#endif
				
		private void CreateWithSimpleSkeleton(Scene scene, Dictionary<int, AllianceData.ItemDefinition> roster, AllianceData.SkinDefinition skin, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool weaponsDrawn)
		{
			_body = GameEntity.CreateEmpty(scene, true, false, false);
			GetOrCreateRoot(scene).AddChild(_body);
			_body.SetGlobalFrame(ApplyBodyScale(_frame));
			try
			{
				_body.CreateSimpleSkeleton(skin.Skeleton);
			}
			catch (Exception ex)
			{
				Log($"{this}: skeleton '{skin.Skeleton}' creation failed ({ex.Message}) - skipped.", LogLevel.Error);
				_body.Remove(0);
				_body = null;
				return;
			}

			Dress(roster, skin, monster, color1, color2, weaponsDrawn, _cultureId);
		}

		private void Dress(Dictionary<int, AllianceData.ItemDefinition> roster, AllianceData.SkinDefinition skin, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool weaponsDrawn, string cultureId)
		{
			// Add body meshes (head, body, hands, legs, underwear) according to the visible skin mask.
			AddBodyMeshes(skin, VisibleSkinMask(roster));

			AllianceData.ItemDefinition mountItem = null;
			AllianceData.ItemDefinition harnessItem = null;
			AllianceData.ItemDefinition mainHand = null;
			AllianceData.ItemDefinition offHand = null;
			HashSet<string> usedHolsters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			// Find the drawn weapon and shield (if any) to place them in hand, the rest will be sheathed.
			if (weaponsDrawn) (mainHand, offHand) = ElectDrawnWeapons(roster);

			// Items with has_lower_holster_priority claim their holster after the others.
			List<AllianceData.ItemDefinition> lowerHolsterPriority = new List<AllianceData.ItemDefinition>();

			// Perf cap: at most 2 sheathed weapon/ammo meshes per fake (invisible beyond that).
			int sheathedCount = 0;
			int maxSheathed = 2;

			// Iterate the equipment roster and attach meshes correspondingly.
			foreach (AllianceData.ItemDefinition item in roster.Values)
			{
				switch (item.Kind)
				{
					case AllianceData.ItemKind.Mount: mountItem = item; continue;
					case AllianceData.ItemKind.MountHarness: harnessItem = item; continue;
					// Banner: hidden while sheathed, carried in the off hand (with the culture's sigils/colors) when drawn.
					case AllianceData.ItemKind.Banner:
						if (weaponsDrawn) AttachBanner(item, skin.Skeleton, monster, color1, color2, cultureId);
						continue;
				}
				bool drawn = ReferenceEquals(item, mainHand) || ReferenceEquals(item, offHand);
				bool sheathedWeapon = !drawn && (item.Kind == AllianceData.ItemKind.Weapon || item.Kind == AllianceData.ItemKind.Ammo);
				if (sheathedWeapon && sheathedCount >= maxSheathed) continue;
				if (item.HasLowerHolsterPriority && !drawn)
				{
					lowerHolsterPriority.Add(item);
					continue;
				}
				AttachEquippedItem(item, skin, monster, color1, color2, drawn, usedHolsters, cultureId);
				if (sheathedWeapon) sheathedCount++;
			}
			// Attach the lower-priority items last, until the sheathed limit is reached.
			foreach (AllianceData.ItemDefinition item in lowerHolsterPriority)
			{
				if (sheathedCount >= maxSheathed) break;
				AttachEquippedItem(item, skin, monster, color1, color2, false, usedHolsters, cultureId);
				sheathedCount++;
			}
			// The mount (+harness) becomes the fake's root, with the rider seated on it.
			SetupMount(mountItem, harnessItem, color1, color2);
		}

		private void AttachEquippedItem(AllianceData.ItemDefinition item, AllianceData.SkinDefinition skin, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters, string cultureId)
		{
			// Crafted weapons are composed from their design data, then attached to hand/holster bones.
			if (item.IsCrafted) AttachCraftedWeapon(item, skin.Skeleton, monster, color1, color2, drawn, usedHolsters);
			// Rigid weapons and ammo are attached to hand/holster bones.
			else if (item.Kind == AllianceData.ItemKind.Weapon || item.Kind == AllianceData.ItemKind.Ammo) AttachWeapon(item, skin.Skeleton, monster, color1, color2, drawn, usedHolsters, cultureId);
			// Clothing and armor are simply added to the skeleton (skinned).
			else AddMeshToSkeleton(_body, item.MeshName, color1, color2, 1, item, cultureId);
		}

		private static SkinMask VisibleSkinMask(Dictionary<int, AllianceData.ItemDefinition> roster)
		{
			SkinMask mask = SkinMask.AllVisible;
			foreach (AllianceData.ItemDefinition item in roster.Values)
			{
				if (item.HasArmor) mask &= item.MeshesMask;
			}
			return mask;
		}

		private void AddBodyMeshes(AllianceData.SkinDefinition skin, SkinMask mask)
		{
			uint noTint = uint.MaxValue;
			if (mask.HasAnyFlag(SkinMask.HeadVisible)) AddMeshToSkeleton(_body, skin.Head, noTint, noTint);
			if (mask.HasAnyFlag(SkinMask.BodyVisible))
			{
				AddMeshToSkeleton(_body, skin.Body, noTint, noTint, 3);
				AddMeshToSkeleton(_body, skin.BodyShoulders, noTint, noTint, 3);
			}
			if (mask.HasAnyFlag(SkinMask.UnderwearVisible)) AddMeshToSkeleton(_body, skin.UnderwearBottom, noTint, noTint, 5);
			if (mask.HasAnyFlag(SkinMask.HandsVisible)) AddMeshToSkeleton(_body, skin.Hands, noTint, noTint, 1);
			if (mask.HasAnyFlag(SkinMask.LegsVisible)) AddMeshToSkeleton(_body, skin.Legs, noTint, noTint, 3);
		}

		// One weapon per hand when drawn: first weapon to the main hand, shield to the off-hand.
		// MP rosters never pair shields with two-handed setups, so a present shield is always wielded.
		private static (AllianceData.ItemDefinition MainHand, AllianceData.ItemDefinition OffHand) ElectDrawnWeapons(Dictionary<int, AllianceData.ItemDefinition> roster)
		{
			AllianceData.ItemDefinition mainHand = null;
			AllianceData.ItemDefinition offHand = null;
			for (int slot = (int)EquipmentIndex.Weapon0; slot <= (int)EquipmentIndex.ExtraWeaponSlot; slot++)
			{
				if (!roster.TryGetValue(slot, out AllianceData.ItemDefinition item)) continue;
				if (item.Kind == AllianceData.ItemKind.Ammo || item.Kind == AllianceData.ItemKind.Banner) continue;
				if (item.IsOffHand)
				{
					offHand ??= item;
				}
				else mainHand ??= item;
			}
			return (mainHand, offHand);
		}

		private void AttachWeapon(AllianceData.ItemDefinition item, string skeletonName, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters, string cultureId)
		{
			AllianceData.HolsterDefinition holster = ResolveHolster(item, usedHolsters);
			if (holster != null) usedHolsters.Add(holster.Id);
			bool isAmmo = item.Kind == AllianceData.ItemKind.Ammo;

			if (!drawn && holster != null)
			{
				string sheathMesh = SheathedMeshOf(item);
				string mesh = isAmmo ? sheathMesh : FirstNonEmpty(sheathMesh, item.MeshName);
				AttachSheathed(mesh, holster, item, monster, skeletonName, color1, color2, cultureId);
				return;
			}
			if (isAmmo) return;
			if (drawn && holster != null && holster.ShowHolsterWhenDrawn && !string.IsNullOrEmpty(item.HolsterMesh))
			{
				AttachSheathed(item.HolsterMesh, holster, item, monster, skeletonName, color1, color2, cultureId);
			}
			AttachHandHeldWeapon(item, skeletonName, monster, color1, color2, cultureId);
		}

		// Get the first free compatible holster from the list
		private static AllianceData.HolsterDefinition ResolveHolster(AllianceData.ItemDefinition item, HashSet<string> usedHolsters)
		{
			foreach (string holsterId in item.Holsters)
			{
				AllianceData.HolsterDefinition holster = AllianceData.Instance.GetHolster(holsterId);
				if (holster != null && !usedHolsters.Contains(holster.Id)) return holster;
			}
			return null;
		}

		private static string SheathedMeshOf(AllianceData.ItemDefinition item)
		{
			return FirstNonEmpty(item.HolsterWithWeaponMesh, item.HolsterMesh);
		}

		private static string FirstNonEmpty(string a, string b)
		{
			return !string.IsNullOrEmpty(a) ? a : b;
		}

		private void AttachSheathed(string meshName, AllianceData.HolsterDefinition holster, AllianceData.ItemDefinition item, AllianceData.MonsterDefinition monster, string skeletonName, uint color1, uint color2, string cultureId)
		{
			MetaMesh mesh = GetMesh(item, meshName, color1, color2, cultureId);
			if (mesh == null) return;
			AttachOnRootFrame(mesh, holster, monster, skeletonName, item.HolsterPositionShift);
		}

		private static readonly Dictionary<string, MatrixFrame> _holsterFrameCache = new Dictionary<string, MatrixFrame>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> _holsterLookupWarned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private void AttachOnRootFrame(MetaMesh mesh, AllianceData.HolsterDefinition holster, AllianceData.MonsterDefinition monster, string skeletonName, Vec3 shift)
		{
			try
			{
				if (!_holsterFrameCache.TryGetValue(holster.Id, out MatrixFrame holsterFrame))
				{
					int index = MBItem.GetItemHolsterIndex(holster.Id);
					if (index >= 0)
					{
						holsterFrame = MBItem.GetHolsterFrameByIndex(index);
						_holsterFrameCache[holster.Id] = holsterFrame;
						Log($"[FakeAgent] holster '{holster.Id}' (index {index}) resolved: origin {holsterFrame.origin}, rotation f {holsterFrame.rotation.f}, s {holsterFrame.rotation.s}, u {holsterFrame.rotation.u}.", LogLevel.Debug);
					}
					else
					{
						if (_holsterLookupWarned.Add(holster.Id))
							Log($"[FakeAgent] native holster registry has no entry for '{holster.Id}' (index -1) - falling back to identity placement.", LogLevel.Warning);
						holsterFrame = MatrixFrame.Identity;
					}
				}
				MatrixFrame finalFrame = holsterFrame;
				finalFrame.origin += holsterFrame.rotation.TransformToParent(shift);
				finalFrame = UnscaleWeaponFrame(finalFrame);
				string holsterBone = AllianceData.Instance.GetHolsterBone(holster.Id);
				string monsterBone = HolsterBoneOnMonster(monster, holsterBone);
				sbyte bone = 0;
				if (!string.IsNullOrEmpty(monsterBone))
				{
					sbyte resolved = Skeleton.GetBoneIndexFromName(skeletonName, monsterBone);
					if (resolved >= 0) bone = resolved;
				}
				mesh.Frame = finalFrame;
				_body.Skeleton.AddComponentToBone(bone, mesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: holster '{holster.Id}' placement failed ({ex.Message}).", LogLevel.Error);
			}
		}

		/// <summary> Get the skeleton bone corresponding to the item_holsters's bone.</summary>
		private static string HolsterBoneOnMonster(AllianceData.MonsterDefinition monster, string holsterBone)
		{
			switch (holsterBone)
			{
				case "biped_thorax": return monster?.ThoraxBone;
				case "biped_abdomen": return monster?.SpineLowerBone;
				case "biped_item_r": return monster?.MainHandItemBone;
				case "biped_item_l": return monster?.OffHandItemBone;
				case "biped_forearm1_l": return monster?.OffHandItemSecondaryBone;
				case "biped_forearm1_r": return monster?.OffHandItemSecondaryBone?.Replace("l_", "r_");
				default: return holsterBone;
			}
		}

		private const float DegToRad = 0.017453292f;

		// The local frame of an item, based on its defined position/rotation in the XML.
		private static MatrixFrame WeaponFrameOf(AllianceData.ItemDefinition item)
		{
			Vec3 position = item.WeaponPosition;
			Vec3 rotation = item.WeaponRotation;
			if (position.LengthSquared < 0.0001f && rotation.LengthSquared < 0.0001f) return MatrixFrame.Identity;
			Mat3 rot = Mat3.Identity;
			// Native euler convention (Mat3.ApplyEulerAngles): x -> side, y -> forward, z -> up.
			rot.ApplyEulerAngles(rotation * DegToRad);
			return new MatrixFrame(rot, position);
		}

		// Drawn weapons follow the hand item bone; shields the off-hand secondary one.
		private void AttachHandHeldWeapon(AllianceData.ItemDefinition item, string skeletonName, AllianceData.MonsterDefinition monster, uint color1, uint color2, string cultureId)
		{
			bool offHand = item.IsOffHand;
			sbyte bone = offHand
				? ResolveBone(skeletonName,
					item.NativeFlags.HasFlag(ItemFlags.ForceAttachOffHandSecondaryItemBone) ? monster?.OffHandItemSecondaryBone : null,
					monster?.OffHandItemBone)
				: ResolveBone(skeletonName, monster?.MainHandItemBone);
			if (bone < 0)
			{
				Log($"{this}: no item bone for weapon '{item.MeshName}' - skipped.", LogLevel.Error);
				return;
			}
			MetaMesh mesh = GetMesh(item, item.MeshName, color1, color2, cultureId);
			if (mesh == null) return;
			// The authored Weapon frame orients the grip in the hand.
			mesh.Frame = UnscaleWeaponFrame(WeaponFrameOf(item));
			_body.Skeleton.AddComponentToBone(bone, mesh);
		}

#if !SERVER
		// Composed crafted-weapon meshes, cached per design.
		private static readonly Dictionary<WeaponDesign, TaleWorlds.MountAndBlade.View.CraftedDataView> _craftedViews = new Dictionary<WeaponDesign, TaleWorlds.MountAndBlade.View.CraftedDataView>();

		// Generate and attach crafted weapon meshes.
		private void AttachCraftedWeapon(AllianceData.ItemDefinition item, string skeletonName, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters)
		{
			try
			{
				ItemObject itemObject = MBObjectManager.Instance?.GetObject<ItemObject>(item.Id);				
				WeaponDesign design = itemObject?.WeaponDesign;
				if (design == null)
				{
					Log($"{this}: crafted weapon '{item.Id}' has no design data - skipped.", LogLevel.Debug);
					return;
				}
				if (!_craftedViews.TryGetValue(design, out TaleWorlds.MountAndBlade.View.CraftedDataView view))
				{
					_craftedViews[design] = view = new TaleWorlds.MountAndBlade.View.CraftedDataView(design);
				}

				AllianceData.HolsterDefinition holster = null;
				foreach (string holsterId in itemObject.ItemHolsters ?? new string[0])
				{
					AllianceData.HolsterDefinition candidate = AllianceData.Instance.GetHolster(holsterId);
					if (candidate != null && !usedHolsters.Contains(candidate.Id))
					{
						holster = candidate;
						break;
					}
				}
				if (holster != null) usedHolsters.Add(holster.Id);

				// Native only colors items flagged use_team_color (else tableau) - a crafted weapon
				// keeps its authored materials otherwise (AgentVisuals.AddSkinArmorWeaponMultiMeshesToEntity).
				bool teamColored = itemObject.IsUsingTeamColor;
				MetaMesh weaponMesh = view.WeaponMesh?.CreateCopy();
				weaponMesh?.SetLodBias(1);
				MetaMesh sheathMesh = (view.HolsterMeshWithWeapon ?? view.HolsterMesh)?.CreateCopy();
				sheathMesh?.SetLodBias(1);
				if (teamColored)
				{
					ApplyTeamColor(weaponMesh, color1, color2);
					ApplyTeamColor(sheathMesh, color1, color2);
				}

				if (!drawn && holster != null)
				{
					MetaMesh sheath = sheathMesh ?? weaponMesh;
					if (sheath != null)
					{
						AttachOnRootFrame(sheath, holster, monster, skeletonName, itemObject.HolsterPositionShift);
						return;
					}
				}
				if (drawn && holster != null && holster.ShowHolsterWhenDrawn && view.HolsterMesh != null)
				{
					MetaMesh holsterMesh = view.HolsterMesh.CreateCopy();
					if (teamColored) ApplyTeamColor(holsterMesh, color1, color2);
					AttachOnRootFrame(holsterMesh, holster, monster, skeletonName, itemObject.HolsterPositionShift);
				}
				sbyte bone = ResolveBone(skeletonName, monster?.MainHandItemBone);
				if (weaponMesh == null || bone < 0)
				{
					Log($"{this}: crafted weapon '{item.Id}' cannot be wielded - skipped.", LogLevel.Warning);
					return;
				}
				// Crafted meshes are authored at the race's final size (troll hammer...) - they must not inherit the body's entity scale.
				weaponMesh.Frame = UnscaleWeaponFrame(weaponMesh.Frame);
				_body.Skeleton.AddComponentToBone(bone, weaponMesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: crafted weapon '{item.Id}' composition failed ({ex.Message}).", LogLevel.Error);
			}
			}

#else
		private void AttachCraftedWeapon(AllianceData.ItemDefinition item, string skeletonName, AllianceData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters)
		{
		}
#endif

		/// <summary>Native team coloring (AgentVisuals.AddTeamColorToMesh): per-sub-mesh colors +
		/// the double-colormap shader flag on a material copy, skipping meshes tagged no_team_color.</summary>
		private static void ApplyTeamColor(MetaMesh mesh, uint color1, uint color2)
		{
			if (mesh == null) return;
			for (int i = 0; i < mesh.MeshCount; i++)
			{
				Mesh subMesh = mesh.GetMeshAtIndex(i);
				if (subMesh == null || subMesh.HasTag("no_team_color")) continue;
				subMesh.Color = color1;
				subMesh.Color2 = color2;
				Material material = subMesh.GetMaterial().CreateCopy();
				material.AddMaterialShaderFlag("use_double_colormap_with_mask_texture", showErrors: false);
				subMesh.SetMaterial(material);
			}
		}

		/// <summary>Creates the mount from the roster's Horse (+HorseHarness) items.
		/// The rider is re-parented onto it with its frame pinned to the mount's rider-sit bone
		/// and the mount becomes the "root".</summary>
		private void SetupMount(AllianceData.ItemDefinition mountItem, AllianceData.ItemDefinition harnessItem, uint color1, uint color2)
		{
			if (mountItem == null) return;

			AllianceData.MonsterDefinition mountMonster = AllianceData.Instance.GetMonster(mountItem.MountMonsterId);
			if (mountMonster == null)
			{
				Log($"{this}: mount monster '{mountItem.MountMonsterId}' not found in monsters.xml - mount skipped.", LogLevel.Error);
				return;
			}
			string mountSkeletonName = AllianceData.Instance.GetSkeletonForActionSet(mountMonster.ActionSetId);
			if (string.IsNullOrEmpty(mountSkeletonName))
			{
				Log($"{this}: no skeleton for mount action set '{mountMonster.ActionSetId}' (action_sets.xml) - mount skipped.", LogLevel.Error);
				return;
			}

			_mountEntity = GameEntity.CreateEmpty(_body.Scene, false);
			_mountEntity.Name = "MountEntity";
			try
			{
				_mountEntity.CreateSimpleSkeleton(mountSkeletonName);
			}
			catch (Exception ex)
			{
				Log($"{this}: mount skeleton '{mountSkeletonName}' creation failed ({ex.Message}) - mount skipped.", LogLevel.Error);
				_mountEntity.Remove(0);
				_mountEntity = null;
				return;
			}

			AddMeshToSkeleton(_mountEntity, mountItem.MeshName, uint.MaxValue, uint.MaxValue);
			if (harnessItem != null) AddMeshToSkeleton(_mountEntity, harnessItem.MeshName, uint.MaxValue, uint.MaxValue);
			if (mountItem.AdditionalMeshes != null)
			{
				bool harnessCovers = harnessItem != null
					&& !string.IsNullOrEmpty(harnessItem.ManeCoverType)
					&& !harnessItem.ManeCoverType.Equals("none", StringComparison.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, bool> additional in mountItem.AdditionalMeshes)
				{
					if (string.IsNullOrEmpty(additional.Key)) continue;
					if (harnessCovers && additional.Value) continue;
					AddMeshToSkeleton(_mountEntity, additional.Key, uint.MaxValue, uint.MaxValue);
				}
			}

			GetOrCreateRoot(_body.Scene).AddChild(_mountEntity);
			MatrixFrame mountSpawnFrame = _mountFrame ?? _frame;
			_mountLocalOffset = mountSpawnFrame.origin - _frame.origin;
			_mountEntity.SetGlobalFrame(mountSpawnFrame);
			_mountedClips = ResolveMountedClips(mountMonster.ActionSetId, _bodyActionSetId, mountMonster.MonsterUsage);
			PlayMountClip(_mountedClips.Value.Mount.Idle, loop: true);
			if (_mountFrame.HasValue)
			{
				// Preview convention: the mount stands at its own slot, the rider stays on foot.
				return;
			}
			_seatedOnMount = SeatRiderOnMount(mountSkeletonName, mountMonster.RiderSitBone);
		}

		/// <summary>Pins the rider entity onto the mount's rider-sit bone (mounted animations are made for that anchor).
		/// The measured seat is cached per mount skeleton, so mounted crowds pay the pose evaluation only once.</summary>
		private bool SeatRiderOnMount(string mountSkeletonName, string riderSitBone)
		{
			if (string.IsNullOrEmpty(mountSkeletonName) || string.IsNullOrEmpty(riderSitBone))
			{
				Log($"{this}: mount has no rider_sit_bone - rider not seated.", LogLevel.Warning);
				return false;
			}
			MatrixFrame riderLocal = MatrixFrame.Identity;
			if (!_seatByMountSkeleton.TryGetValue(mountSkeletonName, out MatrixFrame sitFrame))
			{
				sbyte sitBone = Skeleton.GetBoneIndexFromName(mountSkeletonName, riderSitBone);
				if (sitBone < 0)
				{
					Log($"{this}: sit bone '{riderSitBone}' not found on skeleton '{mountSkeletonName}' - rider not seated.", LogLevel.Warning);
					return false;
				}
				// Force an animation tick to ensure the skeleton is in a meaningful pose to get proper bone position.
				_mountEntity.Skeleton?.TickAnimations(0f, _frame, false);
				sitFrame = _mountEntity.GetBoneEntitialFrameWithIndex(sitBone);
				_seatByMountSkeleton[mountSkeletonName] = sitFrame;
			}
			riderLocal.origin = sitFrame.origin;
			riderLocal = ApplyBodyScale(riderLocal);
			try
			{
				// Re-parent the rider onto the mount (the mount becomes the new root).
				GetOrCreateRoot(_body.Scene).RemoveChild(_body, false, false, true, 0);
				_mountEntity.AddChild(_body);
				_body.SetLocalFrame(ref riderLocal, true);
				return true;
			}
			catch (Exception ex)
			{
				Log($"{this}: rider re-parenting onto the mount failed ({ex.Message}).", LogLevel.Error);
				return false;
			}
		}

		/// <summary>Drawn banner: carried in the off hand (main hand stays free for the weapon),
		/// cloth textured with the culture's sigils/colors - like any native held weapon.</summary>
		private void AttachBanner(AllianceData.ItemDefinition item, string skeletonName, AllianceData.MonsterDefinition monster, uint color1, uint color2, string cultureId)
		{
			sbyte bone = ResolveBone(skeletonName, monster?.OffHandItemBone, monster?.MainHandItemBone);
			if (bone < 0)
			{
				Log($"{this}: no hand bone for banner '{item.MeshName}' - skipped.", LogLevel.Warning);
				return;
			}
			MetaMesh mesh = GetMesh(item, item.MeshName, color1, color2, cultureId);
			if (mesh == null) return;
			mesh.Frame = UnscaleWeaponFrame(WeaponFrameOf(item));
			_body.Skeleton.AddComponentToBone(bone, mesh);
#if !SERVER
			ApplyCultureBannerTexture(mesh, cultureId, color1, color2);
#endif
		}

		/// <summary>Applies the culture's banner design to the banner_replacement_mesh tagged
		/// sub-meshes (tableau texture + blending), like the native agent visuals do.</summary>
#if !SERVER
		private void ApplyCultureBannerTexture(MetaMesh mesh, string cultureId, uint color1, uint color2)
		{
			Banner banner = BannerToCultureHelper.GetBannerFromCulture(cultureId, color1, color2);
			if (banner == null)
			{
				Log($"{this}: no banner design for culture '{cultureId}' - banner stays untextured.", LogLevel.Warning);
				return;
			}
			TaleWorlds.MountAndBlade.View.BannerVisual visual = new TaleWorlds.MountAndBlade.View.BannerVisual(banner);
			BannerDebugInfo debugInfo = TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails.BannerDebugInfo.CreateManual("FakeAgent");
			visual.GetTableauTextureLarge(in debugInfo, delegate (Texture texture)
			{
				// No banner_replacement_mesh tag filter here: shield/armor meshes don't carry the
				// banner pipeline tags, the tableau texture applies to the whole mesh and the
				// materials' own shaders decide whether it shows.
				for (int i = 0; i < mesh.MeshCount; i++)
				{
					Mesh subMesh = mesh.GetMeshAtIndex(i);
					if (subMesh == null) continue;
					Material material = subMesh.GetMaterial().CreateCopy();
					material.SetTexture(Material.MBTextureType.DiffuseMap2, texture);
					ulong blendingFlag = (ulong)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
					material.SetShaderFlags(material.GetShaderFlags() | blendingFlag);
					subMesh.SetMaterial(material);
				}
			});
		}
#endif

		/// <summary>First bone index resolved from the candidate names (-1 when none).</summary>
		private static sbyte ResolveBone(string skeletonName, string boneName1, string boneName2 = null)
		{
			foreach (string name in new[] { boneName1, boneName2 })
			{
				if (string.IsNullOrEmpty(name)) continue;
				try
				{
					sbyte bone = Skeleton.GetBoneIndexFromName(skeletonName, name);
					if (bone >= 0) return bone;
				}
				catch { }
			}
			return -1;
		}

		private void AddMeshToSkeleton(GameEntity entity, string meshName, uint color1, uint color2, int lodBias = 0, AllianceData.ItemDefinition item = null, string cultureId = null)
		{
			if (string.IsNullOrEmpty(meshName)) return;
			MetaMesh mesh = GetMesh(item, meshName, color1, color2, cultureId);
			if (mesh == null) return;
			try
			{
				int bias = _lodBiasOverride >= 0 ? _lodBiasOverride : lodBias;
				if (bias != 0) mesh.SetLodBias(bias);
				entity.AddMultiMeshToSkeleton(mesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: mesh '{meshName}' binding failed ({ex.Message}).", LogLevel.Error);
			}
		}
		/// <summary>Applies the culture's banner design (tableau) or the native team coloring to a
		/// mesh: tableau items (using_tableau="true") get the banner motif, items flagged
		/// use_team_color get the culture colors (like native agent visuals), everything else
		/// (mounts, gear) keeps its authored materials.</summary>
		private MetaMesh PaintMesh(AllianceData.ItemDefinition item, MetaMesh mesh, uint color1, uint color2, string cultureId)
		{
#if !SERVER
			if (item != null && item.UsingTableau)
			{
				ApplyCultureBannerTexture(mesh, cultureId, color1, color2);
				return mesh;
			}

#endif
			if (item != null && item.NativeFlags.HasFlag(ItemFlags.UseTeamColor))
			{
				ApplyTeamColor(mesh, color1, color2);
			}
			return mesh;
		}

		private MetaMesh GetMesh(AllianceData.ItemDefinition item, string meshName, uint color1, uint color2, string cultureId)
		{
			try
			{
				MetaMesh mesh = MetaMesh.GetCopy(meshName, true, false);
				if (mesh == null) return null;
				return PaintMesh(item, mesh, color1, color2, cultureId);
			}
			catch (Exception ex)
			{
				Log($"{this}: mesh '{meshName}' unavailable ({ex.Message}).", LogLevel.Error);
				return null;
			}
		}

		/// <summary>Entity carrying the fake's world frame: the mount when mounted, the body otherwise.</summary>
		/// <summary>Entity carrying the fake's world frame: the mount when the fake is actually
		/// seated on it, the body otherwise (unseated preview mounts stand beside the rider).</summary>
		private GameEntity MoveRoot => _seatedOnMount && _mountEntity != null ? _mountEntity : _body;

		public void PlayClip(string clipName, float speed = 1f, bool loop = false, float maxStartOffset = MaxClipOffset)
		{
			PlayClipOn(_body?.Skeleton, ref _riderLoop, clipName, speed, loop, "", maxStartOffset);
		}

		public void PlayMountClip(string clipName, float speed = 1f, bool loop = false, float maxStartOffset = MaxClipOffset)
		{
			PlayClipOn(_mountEntity?.Skeleton, ref _mountLoop, clipName, speed, loop, "mount ", maxStartOffset);
		}

		private void PlayClipOn(Skeleton skeleton, ref ClipLooper looper, string clipName, float speed, bool loop, string logPrefix, float maxStartOffset)
		{
			if (skeleton == null || string.IsNullOrEmpty(clipName)) return;
			float randomizedStart = MBRandom.RandomFloat * (speed != 1f ? MaxFastClipOffset : maxStartOffset);
			try
			{
				skeleton.SetAnimationAtChannel(clipName, 0, speed > 0f ? speed : 1f, -1, randomizedStart);
			}
			catch (Exception ex)
			{
				Log($"{this}: {logPrefix}clip '{clipName}' failed ({ex.Message}).", LogLevel.Error);
				return;
			}
			looper.Set(clipName, speed, loop, randomizedStart);
		}

		/// <summary>Bakes the race scale into a frame (unmounted bodies only - mounted riders are
		/// scaled through their local frame under the mount).</summary>
		private MatrixFrame ApplyBodyScale(MatrixFrame frame)
		{
			if (_bodyScale == 1f) return frame;
			frame.rotation = frame.rotation * _bodyScale;
			return frame;
		}

		/// <summary>Weapon meshes must not scale with the race's scale (e.g. the troll hammer):
		/// Since we inevitably apply the race's scale to the weapon when attaching it to the skeleton,
		/// we must counter-scale it to get the correct size.</summary>
		private MatrixFrame UnscaleWeaponFrame(MatrixFrame frame)
		{
			if (_bodyScale == 1f) return frame;
			frame.rotation = frame.rotation * (1f / _bodyScale);
			return frame;
		}

		private void PlayIdle()
		{
			string clip = _seatedOnMount ? (_mountedClips?.Rider.Idle) : (_bodyClips?.Idle);
			if (!string.IsNullOrEmpty(clip)) PlayClip(clip, loop: true);
		}

		private void PlayMountIdle()
		{
			if (!string.IsNullOrEmpty(_mountedClips?.Mount.Idle)) PlayMountClip(_mountedClips.Value.Mount.Idle, loop: true);
		}

		public void MoveTo(MatrixFrame destination, bool run = false, string moveClip = null, float? speedOverride = null, string mountMoveClip = null, bool followGround = false)
		{
			_moveDestination = destination.origin;
			_moveSpeed = speedOverride ?? (run ? _runSpeed : _walkSpeed);
			_followGround = followGround;
			bool hasMove = false;
			bool fast = _moveSpeed > _walkSpeed * 1.25f;

			if (_seatedOnMount && _mountedClips.HasValue)
			{
				// Mounted: mount and rider play their own idle/walk/run, paired by action type
				// (act_warg_forward_walk -> warg clip on the creature, rider clip on the rider).
				DefaultClips mount = _mountedClips.Value.Mount;
				DefaultClips rider = _mountedClips.Value.Rider;
				string riderGait = rider.Gait(fast);
				if (!string.IsNullOrEmpty(riderGait)) { PlayClip(riderGait, loop: true, maxStartOffset: MaxMoveClipOffset); hasMove = true; }
				else if (!string.IsNullOrEmpty(moveClip) && AllianceData.Instance.ClipExists(moveClip)) { PlayClip(moveClip, loop: true, maxStartOffset: MaxMoveClipOffset); hasMove = true; }
				else { string gait = (_bodyClips ?? ResolveBodyClips(HumanActionSet)).Gait(fast); if (!string.IsNullOrEmpty(gait)) { PlayClip(gait, loop: true, maxStartOffset: MaxMoveClipOffset); hasMove = true; } }

				string mountGait = !string.IsNullOrEmpty(mountMoveClip) ? mountMoveClip : mount.Gait(fast);
				if (!string.IsNullOrEmpty(mountGait)) { PlayMountClip(mountGait, loop: true, maxStartOffset: MaxMoveClipOffset); hasMove = true; }
			}
			else
			{
				DefaultClips clips = _bodyClips ?? ResolveBodyClips(HumanActionSet);
				string gait = !string.IsNullOrEmpty(moveClip) && AllianceData.Instance.ClipExists(moveClip) ? moveClip : clips.Gait(fast);
				if (!string.IsNullOrEmpty(gait)) { PlayClip(gait, loop: true, maxStartOffset: MaxMoveClipOffset); hasMove = true; }
			}

			_movingWithClip = hasMove;
		}

		public void Teleport(MatrixFrame frame)
		{
			_moveDestination = null;
			_frame = frame;
			MoveRoot?.SetGlobalFrame(_seatedOnMount ? frame : ApplyBodyScale(frame));
			// Unseated preview mounts are separate siblings - move them with their rider
			// (they spawn at a fixed offset beside the character frame).
			if (!_seatedOnMount && _mountEntity != null)
			{
				MatrixFrame mountFrame = frame;
				mountFrame.origin += _mountLocalOffset;
				_mountEntity.SetGlobalFrame(mountFrame);
			}
		}

		// Requires the full-visuals path; simple-skeleton fakes cannot play facial
		public void SetFacialAnimation(string animationName, bool loop)
		{
			if (string.IsNullOrEmpty(animationName))
			{
				Log($"{this}: facial animation skipped - no animation name.", LogLevel.Warning);
				return;
			}
#if !SERVER
			if (_usesAgentVisuals)
			{
				try
				{
					_body?.Skeleton?.SetFacialAnimation(Agent.FacialAnimChannel.Mid, animationName, false, loop);
				}
				catch (Exception ex)
				{
					Log($"{this}: facial animation '{animationName}' failed ({ex.Message}).", LogLevel.Error);
				}
				return;
			}
#endif
			Log($"{this}: facial animation '{animationName}' skipped - facial animations require the full-visuals path (AgentVisuals).", LogLevel.Warning);
		}

		public void SetVisible(bool visible)
		{
			_body?.SetVisibilityExcludeParents(visible);
			_mountEntity?.SetVisibilityExcludeParents(visible);
		}

		private void Tick(float dt, Vec3? cameraOrigin = null)
		{
			bool nearCamera = !cameraOrigin.HasValue || (cameraOrigin.Value - _frame.origin).Length < FarCameraDistance;
			_tickCounter++;
			if (nearCamera || (_tickCounter >= 3))
			{
				_riderLoop.Tick(_body?.Skeleton, dt);
				_mountLoop.Tick(_mountEntity?.Skeleton, dt);
			}

			bool moved = false;
			if (_moveDestination.HasValue)
			{
				Vec3 delta = _moveDestination.Value - _frame.origin;
				delta.z = 0f;
				float distance = delta.Length;
				float step = _moveSpeed * dt;
				bool arrived = distance <= step || distance < 0.05f;
				if (arrived)
				{
					_frame.origin = _moveDestination.Value;
					_moveDestination = null;
					if (_movingWithClip)
					{
						_movingWithClip = false;
						PlayIdle();
					}
					if (_mountEntity != null) PlayMountIdle();
				}
				else
				{
					Vec3 direction = delta / distance;
					_frame.origin += direction * step;
					if (_followGround && _body?.Scene != null)
					{
						_body.Scene.GetTerrainHeightAndNormal(_frame.origin.AsVec2, out float height, out _);
						_frame.origin.z = height;
					}
					_frame.rotation = Mat3.Identity;
					_frame.rotation.ApplyEulerAngles(new Vec3(0f, 0f, direction.AsVec2.RotationInRadians));
				}
				moved = true;
			}

			// Idle fakes keep a constant frame - only touching the transform when it changes
			// removes a native call per idle fake per frame.
			if (moved) MoveRoot?.SetGlobalFrame(_seatedOnMount ? _frame : ApplyBodyScale(_frame), false);
		}

		private int _tickCounter;

		/// <summary>Ticks every active fake agent (the only simulation entry point, to avoid double-ticking).
		/// <paramref name="cameraOrigin"/> lets far-away fakes tick their clip loopers at quarter rate.</summary>
		public static void TickAll(float dt, Vec3? cameraOrigin = null)
		{
			_tickWatch.Restart();
			int fakes = _activeAgents.Count;
			for (int i = _activeAgents.Count - 1; i >= 0; i--)
			{
				_activeAgents[i]?.Tick(dt, cameraOrigin);
			}
			// Rolling perf report (helps spot simulation cost regressions with big crowds)
			_tickAccumulatedMs += _tickWatch.Elapsed.TotalMilliseconds;
			_tickFrames++;
			_tickFakeSamples += fakes;
			if (_tickFrames >= 300)
			{
				Log($"[FakeAgent] tick perf: {_tickFakeSamples / (float)_tickFrames:0} fakes/frame, {_tickAccumulatedMs / _tickFrames:0.00} ms/frame average over the last {_tickFrames} frames.", LogLevel.Debug);
				_tickAccumulatedMs = 0;
				_tickFrames = 0;
				_tickFakeSamples = 0;
			}
		}

		private static readonly System.Diagnostics.Stopwatch _tickWatch = new System.Diagnostics.Stopwatch();
		private static double _tickAccumulatedMs;
		private static int _tickFrames;
		private static int _tickFakeSamples;

		public void Despawn()
		{
			// When seated, the mount is the hierarchy root - removing it takes the rider with it.
			// Unseated (preview mounts standing beside) the two entities are separate siblings.
			if (!_seatedOnMount) _body?.Remove(0);
			if (_mountEntity != null) _mountEntity.Remove(0);
			else if (_seatedOnMount) _body?.Remove(0);
#if !SERVER
			ReleaseAgentVisuals();
#endif
			_body = null;
			_mountEntity = null;
			if (_registered) _activeAgents.Remove(this);
		}

		/// <summary>Removes every fake of the given scene (all scenes when null). The registered
		/// fakes are swept too: their visuals are released, their entity references cleared and
		/// they are unregistered - otherwise TickAll would keep ticking destroyed entities.</summary>
		public static void DespawnAll(Scene scene = null)
		{
			for (int i = _activeAgents.Count - 1; i >= 0; i--)
			{
				FakeAgent fake = _activeAgents[i];
				if (fake == null || (scene != null && fake._scene != scene)) continue;
#if !SERVER
				fake.ReleaseAgentVisuals();
#endif
				fake._body = null;
				fake._mountEntity = null;
				fake._moveDestination = null;
				_activeAgents.RemoveAt(i);
			}
			if (scene != null)
			{
				if (_roots.TryGetValue(scene, out GameEntity root))
				{
					root.Remove(0);
					_roots.Remove(scene);
				}
			}
			else
			{
				foreach (GameEntity root in _roots.Values)
				{
					root.Remove(0);
				}
				_roots.Clear();
			}
		}

		private struct ClipLooper
		{
			private string _clipName;
			private float _speed;
			private float _period;
			private float _elapsed;

			public void Set(string clipName, float speed, bool loop, float startProgress = 0f)
			{
				_clipName = loop ? clipName : null;
				_speed = speed > 0f ? speed : 1f;
				_period = AllianceData.Instance.GetClipDuration(clipName) / _speed;
				_elapsed = _period * startProgress;
			}

			public void Tick(Skeleton skeleton, float dt)
			{
				if (_clipName == null || _period <= 0f || skeleton == null) return;
				_elapsed += dt;
				if (_elapsed < _period) return;
				skeleton.SetAnimationAtChannel(_clipName, 0, _speed, -1);
				_elapsed %= _period;
			}
		}
	}

	/// <summary>
	/// Tracks fake agents owned by persistent staged-extras groups, keyed by group.
	/// Cleared on scenario (re)start / mission end.
	/// </summary>
	public static class FakeAgentStore
	{
		private static readonly Dictionary<string, List<FakeAgent>> _byKey = new Dictionary<string, List<FakeAgent>>();

		public static bool Has(string key) => !string.IsNullOrEmpty(key) && _byKey.ContainsKey(key);

		public static void Track(string key, List<FakeAgent> fakes)
		{
			if (string.IsNullOrEmpty(key)) return;
			_byKey[key] = fakes;
		}

		public static void Clear() => _byKey.Clear();
	}
}
