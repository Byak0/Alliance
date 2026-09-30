using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// A "fake" agent is a GameEntity dressed as a character, which can move and play raw animation clips.
	/// Used in cinematics, modding kit previews, revive system, as fake soldier, etc.
	/// Animation are raw clips driven by <see cref="TickAll"/>.
	/// Most data used comes from <see cref="NativeMpData"/> (no fallbacks).
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
		// Fakes farther than this from the camera skip engine transform updates
		private const float FarCameraDistance = 100f;
		// Neutral adult body type used by TaleWorlds' CharacterSpawner
		private const string DefaultBodyPropertiesString = "<BodyProperties version=\"4\" age=\"23.16\" weight=\"0.3333\" build=\"0\" key=\"00000C07000000010011111211151111000701000010000000111011000101000000500202111110000000000000000000000000000000000000000000A00000\" />";

		// Rigid item types: their mesh is not skinned and must be worn (holster) or hand-bound.
		private static readonly HashSet<string> RigidWeaponTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"OneHandedWeapon", "TwoHandedWeapon", "Polearm", "Bow", "Crossbow", "Thrown", "Shield", "Pistol", "Musket", "Banner"
		};

		// Ammo item types: their mesh is the projectile - only the quiver is worn.
		private static readonly HashSet<string> AmmoItemTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Arrows", "Bolts", "SlingStones", "Bullets"
		};

		private static readonly List<FakeAgent> _activeAgents = new List<FakeAgent>();

		// Cached measured rider seat per mount skeleton.
		private static readonly Dictionary<string, MatrixFrame> _seatByMountSkeleton = new Dictionary<string, MatrixFrame>();

		// Shared parent: all fakes can be removed in one engine call (individual removals are slower).
		private static GameEntity _root;
		private static Scene _rootScene;

		private static GameEntity GetOrCreateRoot(Scene scene)
		{
			if (_root != null && _rootScene == scene) return _root;
			_root?.Remove(0);
			_root = GameEntity.CreateEmpty(scene, true, false, false);
			_root.EntityFlags |= EntityFlags.DontSaveToScene;
			_root.Name = "FakeAgents";
			_rootScene = scene;
			return _root;
		}

		private GameEntity _body;
		private GameEntity _mountEntity;
#if !SERVER
		private const int MaxAgentVisualsFakes = 64;
		private static int _agentVisualsCount;
		private TaleWorlds.MountAndBlade.View.AgentVisuals _agentVisuals;
		private bool _usesAgentVisuals;
#endif
		private readonly float _walkSpeed;
		private readonly float _runSpeed;
		private MatrixFrame _frame;
		private Vec3? _moveDestination;
		private float _moveSpeed;
		private bool _movingWithClip;
		private ClipLooper _riderLoop;
		private ClipLooper _mountLoop;

		public bool IsValid => _body != null;
		public string Name { get; }

		public override string ToString() => $"[FakeAgent] '{Name}'";

		/// <summary>Creates a fake agent from a character definition.</summary>
		/// <param name="cultureId">Pass a culture to use its colors.</param>
		/// <param name="useFullAgentVisuals">Use the full native visuals (facial animations...). Costly so only use when necessary.</param>
		/// <param name="weaponsDrawn">Wield weapons in hand instead of carrying them sheathed.</param>
		/// <param name="equipmentSeed">Deterministic pick among the character's equipment rosters.</param>
		public FakeAgent(Scene scene, MatrixFrame frame, NativeMpData.CharacterDefinition character, string cultureId = null, string name = null, bool useFullAgentVisuals = true, bool weaponsDrawn = false, int equipmentSeed = 0)
		{
			Name = name ?? $"Fake {character?.Id ?? "human"}";
			_frame = frame;

			if (character == null)
			{
				Log($"{this}: no character definition - skipped.", LogLevel.Error);
				return;
			}

			string raceId = character.RaceId;
			NativeMpData.SkinDefinition skin = NativeMpData.Instance.GetSkin(raceId, character.IsFemale);
			if (string.IsNullOrEmpty(skin?.Skeleton))
			{
				Log($"{this}: no skins.xml entry with a skeleton for race '{raceId}' - skipped.", LogLevel.Error);
				return;
			}

			NativeMpData.MonsterDefinition monster = NativeMpData.Instance.GetMonster(raceId);
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

			(uint color1, uint color2) = NativeMpData.Instance.GetCultureColors(string.IsNullOrEmpty(cultureId) ? character.CultureId : cultureId);
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
				else if (TryCreateWithAgentVisuals(scene, character, slotItems, equipmentSeed, color1, color2))
				{
					_agentVisualsCount++;
					PlayIdle();
					_activeAgents.Add(this);
					return;
				}
			}
#endif

			// Simple-skeleton path: a raw skeleton wearing the character's meshes. Relatively cheap, but no facial animations possible.
			CreateWithSimpleSkeleton(scene, character, slotItems, skin, monster, color1, color2, weaponsDrawn);
			PlayIdle();
			_activeAgents.Add(this);
		}

#if !SERVER
		// Full-visuals path: builds the fake through the native AgentVisuals pipeline.
		private bool TryCreateWithAgentVisuals(Scene scene, NativeMpData.CharacterDefinition character, Dictionary<int, string> slotItems, int equipmentSeed, uint color1, uint color2)
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

				BodyProperties.FromString(DefaultBodyPropertiesString, out BodyProperties bodyProperties);
				int race = TaleWorlds.Core.FaceGen.GetRaceOrDefault(raceId);

				_body = GameEntity.CreateEmpty(scene, false);
				GetOrCreateRoot(scene).AddChild(_body);
				_body.SetGlobalFrame(_frame);

				AgentVisualsData data = new AgentVisualsData()
					.Equipment(equipment)
					.BodyProperties(bodyProperties)
					.Race(race)
					.Frame(_frame)
					.Scale(1f)
					.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
					.Entity(_body)
					.ActionSet(actionSet)
					.ActionCode(ActionIndexCache.act_inventory_idle_start)
					.Scene(scene)
					.Monster(monster)
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
				Log($"{this}: created as AgentVisuals.", LogLevel.Debug);
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

		private void ReleaseAgentVisuals()
		{
			if (_usesAgentVisuals) _agentVisualsCount--;
			_usesAgentVisuals = false;
			try { _agentVisuals?.Reset(); } catch { }
			_agentVisuals = null;
		}
#endif

		private void CreateWithSimpleSkeleton(Scene scene, NativeMpData.CharacterDefinition character, Dictionary<int, string> slotItems, NativeMpData.SkinDefinition skin, NativeMpData.MonsterDefinition monster, uint color1, uint color2, bool weaponsDrawn)
			{
			List<NativeMpData.ItemDefinition> equipment = new List<NativeMpData.ItemDefinition>();
			foreach (KeyValuePair<int, string> entry in slotItems)
			{
				NativeMpData.ItemDefinition item = NativeMpData.Instance.GetItem(entry.Value);
				if (item != null && (item.IsCrafted || !string.IsNullOrEmpty(item.MeshName))) equipment.Add(item);
			}

			_body = GameEntity.CreateEmpty(scene, true, false, false);
			GetOrCreateRoot(scene).AddChild(_body);
			_body.SetGlobalFrame(_frame);
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

			Dress(equipment, slotItems, skin, monster, color1, color2, weaponsDrawn);
			SetupMount(equipment, color1, color2);
			}

		private void Dress(List<NativeMpData.ItemDefinition> equipment, Dictionary<int, string> slotItems, NativeMpData.SkinDefinition skin, NativeMpData.MonsterDefinition monster, uint color1, uint color2, bool weaponsDrawn)
		{
			// Add body meshes (head, body, hands, legs, underwear) according to the visible skin mask.
			AddBodyMeshes(skin, VisibleSkinMask(equipment), color1, color2);
			// Find the drawn weapon and shield (if any) to place them in hand, the rest will be sheathed
			(string weaponId, string shieldId) = ElectDrawnWeapons(slotItems, weaponsDrawn);
			HashSet<string> usedHolsters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NativeMpData.ItemDefinition item in equipment)
			{
				// Mounts and harnesses are handled separately in SetupMount().
				if (IsMountItem(item) || IsMountHarnessItem(item)) continue;
				bool drawn = item.Id == weaponId || item.Id == shieldId;
				// Crafted weapons are composed from their design data, then attached to hand/holster bones.
				if (item.IsCrafted) AttachCraftedWeapon(item, skin.Skeleton, monster, color1, color2, drawn, usedHolsters);
				// Rigid weapons and ammo are attached to hand/holster bones.
				else if (IsWeaponOrAmmo(item)) AttachWeapon(item, skin.Skeleton, monster, color1, color2, drawn, usedHolsters);
				// Gloves share the hands' lod bias: their lod1 fist pose reads better on idles.
				else if (string.Equals(item.ItemType, "HandArmor", StringComparison.OrdinalIgnoreCase)) AddMeshToSkeleton(_body, item.MeshName, color1, color2, 1);
				// Clothing and armor are simply added to the skeleton (skinned).
				else AddMeshToSkeleton(_body, item.MeshName, color1, color2);
			}
			}

		private static SkinMask VisibleSkinMask(List<NativeMpData.ItemDefinition> equipment)
		{
			SkinMask mask = SkinMask.AllVisible;
			foreach (NativeMpData.ItemDefinition item in equipment)
			{
				if (item.HasArmor) mask &= item.MeshesMask;
			}
			return mask;
		}

		private void AddBodyMeshes(NativeMpData.SkinDefinition skin, SkinMask mask, uint color1, uint color2)
		{
			if (mask.HasAnyFlag(SkinMask.HeadVisible)) AddMeshToSkeleton(_body, skin.Head, color1, color2);
			if (mask.HasAnyFlag(SkinMask.BodyVisible))
			{
				AddMeshToSkeleton(_body, skin.Body, color1, color2, 5);
				AddMeshToSkeleton(_body, skin.BodyShoulders, color1, color2, 5);
			}
			if (mask.HasAnyFlag(SkinMask.UnderwearVisible)) AddMeshToSkeleton(_body, skin.UnderwearBottom, color1, color2, 5);
			if (mask.HasAnyFlag(SkinMask.HandsVisible)) AddMeshToSkeleton(_body, skin.Hands, color1, color2, 1);
			if (mask.HasAnyFlag(SkinMask.LegsVisible)) AddMeshToSkeleton(_body, skin.Legs, color1, color2, 3);
			}

		// One weapon per hand when drawn: first weapon to the main hand, shield to the off-hand.
		// MP rosters never pair shields with two-handed setups, so a present shield is always wielded.
		private static (string WeaponId, string ShieldId) ElectDrawnWeapons(Dictionary<int, string> slotItems, bool weaponsDrawn)
		{
			string weaponId = null, shieldId = null;
			if (weaponsDrawn)
			{
				for (int slot = (int)EquipmentIndex.Weapon0; slot <= (int)EquipmentIndex.ExtraWeaponSlot; slot++)
				{
					if (!slotItems.TryGetValue(slot, out string itemId)) continue;
					NativeMpData.ItemDefinition item = NativeMpData.Instance.GetItem(itemId);
					if (item == null || AmmoItemTypes.Contains(item.ItemType)) continue;
					if (IsOffHandItem(item))
					{
						if (shieldId == null) shieldId = itemId;
					}
					else if (weaponId == null)
					{
						weaponId = itemId;
					}
				}
			}
			return (weaponId, shieldId);
		}

		private static bool IsWeaponOrAmmo(NativeMpData.ItemDefinition item)
		{
			return RigidWeaponTypes.Contains(item.ItemType) || AmmoItemTypes.Contains(item.ItemType);
		}

		private static bool IsOffHandItem(NativeMpData.ItemDefinition item)
		{
			return string.Equals(item.ItemType, "Shield", StringComparison.OrdinalIgnoreCase) || item.HasFlag("HeldInOffHand");
			}

		private void AttachWeapon(NativeMpData.ItemDefinition item, string skeletonName, NativeMpData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters)
			{
			NativeMpData.HolsterDefinition holster = ResolveHolster(item, usedHolsters);
			if (holster != null) usedHolsters.Add(holster.Id);
			bool isAmmo = AmmoItemTypes.Contains(item.ItemType);

			if (!drawn && holster != null)
			{
				string sheathMesh = SheathedMeshOf(item);
				string mesh = isAmmo ? sheathMesh : FirstNonEmpty(sheathMesh, item.MeshName);
				// A bare strapped weapon pivots at its grip: its authored frame composes in.
				// Authored holster meshes already contain the weapon oriented.
				bool composeWeaponFrame = !isAmmo && string.IsNullOrEmpty(sheathMesh);
				AttachSheathed(mesh, holster, item, monster, skeletonName, color1, color2, composeWeaponFrame);
				return;
			}
			if (isAmmo) return;
			if (drawn && holster != null && holster.ShowHolsterWhenDrawn && !string.IsNullOrEmpty(item.HolsterMesh))
			{
				AttachSheathed(item.HolsterMesh, holster, item, monster, skeletonName, color1, color2, false);
			}
			AttachHandHeldWeapon(item, skeletonName, monster, color1, color2);
			}

		// Get the first free compatible holster from the list
		private static NativeMpData.HolsterDefinition ResolveHolster(NativeMpData.ItemDefinition item, HashSet<string> usedHolsters)
			{
			foreach (string holsterId in item.Holsters)
			{
				NativeMpData.HolsterDefinition holster = NativeMpData.Instance.GetHolster(holsterId);
				if (holster != null && !usedHolsters.Contains(holster.Id)) return holster;
			}
			return null;
			}

		private static string SheathedMeshOf(NativeMpData.ItemDefinition item)
			{
			return FirstNonEmpty(item.HolsterWithWeaponMesh, item.HolsterMesh);
			}

		private static string FirstNonEmpty(string a, string b)
			{
			return !string.IsNullOrEmpty(a) ? a : b;
			}

		// Standard holsters place their meshes through the engine root frame; back-carried
		// shields instead match native on the holster bone with the authored frame.
		private void AttachSheathed(string meshName, NativeMpData.HolsterDefinition holster, NativeMpData.ItemDefinition item, NativeMpData.MonsterDefinition monster, string skeletonName, uint color1, uint color2, bool composeWeaponFrame)
		{
			MetaMesh mesh = GetMesh(meshName, color1, color2);
			if (mesh == null) return;
			if (item.HasFlag("ForceAttachOffHandSecondaryItemBone"))
			{
				AttachOnHolsterBone(mesh, holster, item, monster, skeletonName);
			}
			else
			{
				AttachOnRootFrame(mesh, holster, item.HolsterPositionShift, composeWeaponFrame ? WeaponFrameOf(item) : MatrixFrame.Identity);
			}
		}

		private void AttachOnRootFrame(MetaMesh mesh, NativeMpData.HolsterDefinition holster, Vec3 shift, MatrixFrame weaponFrame)
		{
			try
			{
				int index = MBItem.GetItemHolsterIndex(holster.Id);
				MatrixFrame frame = index >= 0 ? MBItem.GetHolsterFrameByIndex(index) : MatrixFrame.Identity;
				frame.origin += frame.rotation.TransformToParent(shift);
				mesh.Frame = frame.TransformToParent(weaponFrame);
				_body.Skeleton.AddComponentToBone(0, mesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: holster '{holster.Id}' placement failed ({ex.Message}).", LogLevel.Error);
			}
			}

		private void AttachOnHolsterBone(MetaMesh mesh, NativeMpData.HolsterDefinition holster, NativeMpData.ItemDefinition item, NativeMpData.MonsterDefinition monster, string skeletonName)
		{
			sbyte bone = -1;
			try
			{
				string boneName = HolsterBoneOnMonster(monster, holster.Bone);
				if (!string.IsNullOrEmpty(boneName)) bone = Skeleton.GetBoneIndexFromName(skeletonName, boneName);
			}
			catch { }
			if (bone < 0)
			{
				AttachOnRootFrame(mesh, holster, item.HolsterPositionShift, WeaponFrameOf(item));
				return;
			}
			Mat3 rotation = Mat3.Identity;
			rotation.ApplyEulerAngles(holster.RotationYawPitchRoll * DegToRad);
			MatrixFrame frame = new MatrixFrame(rotation, holster.Position + item.HolsterPositionShift);
			mesh.Frame = frame.TransformToParent(WeaponFrameOf(item));
			_body.Skeleton.AddComponentToBone(bone, mesh);
			}

		// Return correct bone for given monster
		private static string HolsterBoneOnMonster(NativeMpData.MonsterDefinition monster, string holsterBone)
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

		// The item's authored Weapon component frame (identity when the item carries none).
		private static MatrixFrame WeaponFrameOf(NativeMpData.ItemDefinition item)
		{
			Vec3 position = item.WeaponPosition;
			Vec3 rotation = item.WeaponRotation;
			if (position.LengthSquared < 0.0001f && rotation.LengthSquared < 0.0001f) return MatrixFrame.Identity;
			Mat3 rot = Mat3.Identity;
			rot.RotateAboutUp(rotation.z * DegToRad);
			rot.RotateAboutSide(rotation.y * DegToRad);
			rot.RotateAboutForward(rotation.x * DegToRad);
			return new MatrixFrame(rot, position);
			}

		// Drawn weapons follow the hand item bone; shields the off-hand secondary one.
		private void AttachHandHeldWeapon(NativeMpData.ItemDefinition item, string skeletonName, NativeMpData.MonsterDefinition monster, uint color1, uint color2)
			{
			bool offHand = IsOffHandItem(item);
			sbyte bone = offHand
				? ResolveBone(skeletonName,
					item.HasFlag("ForceAttachOffHandSecondaryItemBone") ? monster?.OffHandItemSecondaryBone : null,
					monster?.OffHandItemBone)
				: ResolveBone(skeletonName, monster?.MainHandItemBone);
			if (bone < 0)
			{
				Log($"{this}: no item bone for weapon '{item.MeshName}' - skipped.", LogLevel.Error);
				return;
			}
			MetaMesh mesh = GetMesh(item.MeshName, color1, color2);
			if (mesh == null) return;
			// The authored Weapon frame orients the grip in the hand.
			mesh.Frame = WeaponFrameOf(item);
			_body.Skeleton.AddComponentToBone(bone, mesh);
			}

#if !SERVER
		// Composed crafted-weapon meshes, cached per design.
		private static readonly Dictionary<WeaponDesign, TaleWorlds.MountAndBlade.View.CraftedDataView> _craftedViews = new Dictionary<WeaponDesign, TaleWorlds.MountAndBlade.View.CraftedDataView>();

		// Generate and attach crafted weapon meshes.
		private void AttachCraftedWeapon(NativeMpData.ItemDefinition item, string skeletonName, NativeMpData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters)
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

				NativeMpData.HolsterDefinition holster = null;
				foreach (string holsterId in itemObject.ItemHolsters ?? new string[0])
				{
					NativeMpData.HolsterDefinition candidate = NativeMpData.Instance.GetHolster(holsterId);
					if (candidate != null && !usedHolsters.Contains(candidate.Id))
					{
						holster = candidate;
						break;
					}
				}
				if (holster != null) usedHolsters.Add(holster.Id);

				MetaMesh weaponMesh = ColoredCopy(view.WeaponMesh, color1, color2);
				MetaMesh sheathMesh = ColoredCopy(FirstMesh(view.HolsterMeshWithWeapon, view.HolsterMesh), color1, color2);

				if (!drawn && holster != null)
				{
					MetaMesh sheath = FirstMesh(sheathMesh, weaponMesh);
					if (sheath != null)
					{
						// Bare composed weapons pivot at the grip: the design's weapon frame
						// composes in (authored scabbards already contain the weapon oriented).
						MatrixFrame weaponFrame = ReferenceEquals(sheath, weaponMesh)
							? itemObject.PrimaryWeapon?.Frame ?? MatrixFrame.Identity
							: MatrixFrame.Identity;
						AttachOnRootFrame(sheath, holster, itemObject.HolsterPositionShift, weaponFrame);
						return;
					}
				}
				if (drawn && holster != null && holster.ShowHolsterWhenDrawn && view.HolsterMesh != null)
				{
					AttachOnRootFrame(ColoredCopy(view.HolsterMesh, color1, color2), holster, itemObject.HolsterPositionShift, MatrixFrame.Identity);
				}
				sbyte bone = ResolveBone(skeletonName, monster?.MainHandItemBone);
				if (weaponMesh == null || bone < 0)
				{
					Log($"{this}: crafted weapon '{item.Id}' cannot be wielded - skipped.", LogLevel.Warning);
					return;
				}
				_body.Skeleton.AddComponentToBone(bone, weaponMesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: crafted weapon '{item.Id}' composition failed ({ex.Message}).", LogLevel.Error);
			}
			}

		private static MetaMesh FirstMesh(MetaMesh a, MetaMesh b) => a != null ? a : b;

		private static MetaMesh ColoredCopy(MetaMesh source, uint color1, uint color2)
			{
			MetaMesh copy = source?.CreateCopy();
			if (copy != null)
			{
				copy.SetFactor1(color1);
				copy.SetFactor2(color2);
			}
			return copy;
			}
#else
		private void AttachCraftedWeapon(NativeMpData.ItemDefinition item, string skeletonName, NativeMpData.MonsterDefinition monster, uint color1, uint color2, bool drawn, HashSet<string> usedHolsters)
			{
			}
#endif

		/// <summary>Creates the mount from the equipment's Horse (+HorseHarness) item.
		/// The rider is re-parented onto it with its frame pinned to the mount's rider-sit bone
		/// and the mount becomes the "root".</summary>
		private void SetupMount(List<NativeMpData.ItemDefinition> equipment, uint color1, uint color2)
		{
			NativeMpData.ItemDefinition mountItem = equipment.FirstOrDefault(IsMountItem);
			if (mountItem == null) return;
			NativeMpData.ItemDefinition harnessItem = equipment.FirstOrDefault(IsMountHarnessItem);

			NativeMpData.MonsterDefinition mountMonster = NativeMpData.Instance.GetMonster(mountItem.MountMonsterId);
			if (mountMonster == null)
			{
				Log($"{this}: mount monster '{mountItem.MountMonsterId}' not found in monsters.xml - mount skipped.", LogLevel.Error);
				return;
			}
			string mountSkeletonName = NativeMpData.Instance.GetSkeletonForActionSet(mountMonster.ActionSetId);
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

			AddMeshToSkeleton(_mountEntity, mountItem.MeshName, color1, color2);
			if (harnessItem != null) AddMeshToSkeleton(_mountEntity, harnessItem.MeshName, color1, color2);
			if (mountItem.AdditionalMeshes != null)
			{
				foreach (KeyValuePair<string, bool> additional in mountItem.AdditionalMeshes)
				{
					if (string.IsNullOrEmpty(additional.Key)) continue;
					// The second value flags meshes a harness hides (e.g. the mane).
					if (harnessItem != null && additional.Value) continue;
					AddMeshToSkeleton(_mountEntity, additional.Key, color1, color2);
				}
			}

			GetOrCreateRoot(_body.Scene).AddChild(_mountEntity);
			_mountEntity.SetGlobalFrame(_frame);
			// The mount must be IN its idle pose before its bones are read: skeletons freshly
			// created sit in their spread bind pose, whose bone positions are meaningless (the
			// sit bone reads meters away until the first animation tick). The measured seat is
			// cached per mount skeleton - identical for every fake of that mount, so spawning a
			// mounted crowd pays the pose evaluation only once.
			PlayMountClip(DefaultMountIdleClip, loop: true);
			if (string.IsNullOrEmpty(mountMonster.RiderSitBone))
			{
				Log($"{this}: mount monster '{mountMonster.Id}' has no rider_sit_bone - rider not seated.", LogLevel.Warning);
				return;
			}
			sbyte sitBone = Skeleton.GetBoneIndexFromName(mountSkeletonName, mountMonster.RiderSitBone);
			if (sitBone < 0)
			{
				Log($"{this}: sit bone '{mountMonster.RiderSitBone}' not found on skeleton '{mountSkeletonName}' - rider not seated.", LogLevel.Warning);
				return;
			}
			MatrixFrame riderLocal = MatrixFrame.Identity;
			if (!_seatByMountSkeleton.TryGetValue(mountSkeletonName, out MatrixFrame sitFrame))
			{
				_mountEntity.Skeleton?.TickAnimations(0f, _frame, false);
				sitFrame = _mountEntity.GetBoneEntitialFrameWithIndex(sitBone);
				_seatByMountSkeleton[mountSkeletonName] = sitFrame;
				Log($"Sit bone {mountMonster.RiderSitBone} of '{mountSkeletonName}' measured at {sitFrame.origin}.", LogLevel.Debug);
			}
			// Rider seat, native convention: the rider's frame is pinned to the mount's rider-sit
			// bone (upright - agent-style), and mounted animations are authored for that anchor.
			// No constants: any mount/rider build measures itself.
			riderLocal.origin = sitFrame.origin;
			try
			{
				GetOrCreateRoot(_body.Scene).RemoveChild(_body, false, false, true, 0);
				_mountEntity.AddChild(_body);
				_body.SetLocalFrame(ref riderLocal, true);
			}
			catch (Exception ex)
			{
				Log($"{this}: rider re-parenting onto the mount failed ({ex.Message}).", LogLevel.Error);
			}
		}

		private static bool IsMountItem(NativeMpData.ItemDefinition item) => string.Equals(item.ItemType, "Horse", StringComparison.OrdinalIgnoreCase);

		private static bool IsMountHarnessItem(NativeMpData.ItemDefinition item) => string.Equals(item.ItemType, "HorseHarness", StringComparison.OrdinalIgnoreCase);

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

		private void AddMeshToSkeleton(GameEntity entity, string meshName, uint color1, uint color2, int lodBias = 0)
		{
			if (string.IsNullOrEmpty(meshName)) return;
			MetaMesh mesh = GetMesh(meshName, color1, color2);
			if (mesh == null) return;
			try
			{
				if(lodBias != 0) mesh.SetLodBias(lodBias);
				entity.AddMultiMeshToSkeleton(mesh);
			}
			catch (Exception ex)
			{
				Log($"{this}: mesh '{meshName}' binding failed ({ex.Message}).", LogLevel.Error);
			}
		}

		private MetaMesh GetMesh(string meshName, uint color1, uint color2)
		{
			try
			{
				MetaMesh mesh = MetaMesh.GetCopy(meshName, true, false);
				if (mesh == null) return null;
				mesh.SetFactor1(color1);
				mesh.SetFactor2(color2);
				return mesh;
			}
			catch (Exception ex)
			{
				Log($"{this}: mesh '{meshName}' unavailable ({ex.Message}).", LogLevel.Error);
				return null;
			}
		}

		/// <summary>Entity carrying the fake's world frame: the mount when mounted, the body otherwise.</summary>
		private GameEntity MoveRoot => _mountEntity ?? _body;

		public void PlayClip(string clipName, float speed = 1f, bool loop = false)
		{
			PlayClipOn(_body?.Skeleton, ref _riderLoop, clipName, speed, loop, "");
		}

		public void PlayMountClip(string clipName, float speed = 1f, bool loop = false)
		{
			PlayClipOn(_mountEntity?.Skeleton, ref _mountLoop, clipName, speed, loop, "mount ");
		}

		private void PlayClipOn(Skeleton skeleton, ref ClipLooper looper, string clipName, float speed, bool loop, string logPrefix)
		{
			if (skeleton == null || string.IsNullOrEmpty(clipName)) return;
			try
			{
				skeleton.SetAnimationAtChannel(clipName, 0, speed > 0f ? speed : 1f);
			}
			catch (Exception ex)
			{
				Log($"{this}: {logPrefix}clip '{clipName}' failed ({ex.Message}).", LogLevel.Error);
				return;
			}
			looper.Set(clipName, speed, loop);
		}

		private void PlayIdle()
		{
			// Mounted riders idle in the saddle (mounted pose convention); standing idle otherwise.
			PlayClip(_mountEntity != null ? DefaultMountedIdleClip : DefaultIdleClip, loop: true);
		}

		private void PlayMountIdle()
		{
			PlayMountClip(DefaultMountIdleClip, loop: true);
		}

		public void MoveTo(MatrixFrame destination, bool run = false, string moveClip = null, float? speedOverride = null, string mountMoveClip = null)
		{
			_moveDestination = destination.origin;
			_moveSpeed = speedOverride ?? (run ? _runSpeed : _walkSpeed);
			_movingWithClip = !string.IsNullOrEmpty(moveClip);
			if (_movingWithClip) PlayClip(moveClip, loop: true);
			// The mount strides too: authored clip, or the registered horse walk/trot by pace.
			if (_mountEntity != null)
			{
				PlayMountClip(!string.IsNullOrEmpty(mountMoveClip) ? mountMoveClip : (_moveSpeed > _walkSpeed * 1.25f ? DefaultMountTrotClip : DefaultMountWalkClip), loop: true);
			}
		}

		public void Teleport(MatrixFrame frame)
		{
			_moveDestination = null;
			_frame = frame;
			MoveRoot?.SetGlobalFrame(frame);
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
			_riderLoop.Tick(_body?.Skeleton, dt);
			_mountLoop.Tick(_mountEntity?.Skeleton, dt);

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
					_frame.rotation = Mat3.Identity;
					_frame.rotation.ApplyEulerAngles(new Vec3(0f, 0f, direction.AsVec2.RotationInRadians));
				}
				moved = true;
			}

			// Only update the frame when close to camera
			bool nearCamera = !cameraOrigin.HasValue || (cameraOrigin.Value - _frame.origin).Length < FarCameraDistance;
			if (moved || nearCamera) MoveRoot?.SetGlobalFrame(_frame, false);
		}

		/// <summary>Ticks every active fake agent (the only simulation entry point, to avoid double-ticking).
		/// <paramref name="cameraOrigin"/> lets far-away fakes skip engine transform updates.</summary>
		public static void TickAll(float dt, Vec3? cameraOrigin = null)
		{
			for (int i = _activeAgents.Count - 1; i >= 0; i--)
				_activeAgents[i]?.Tick(dt, cameraOrigin);
		}

		public void Despawn()
		{
			// When mounted, the mount is the hierarchy root - removing it takes the rider with it.
			if (_mountEntity != null) _mountEntity.Remove(0);
			else _body?.Remove(0);
#if !SERVER
			ReleaseAgentVisuals();
#endif
			_body = null;
			_mountEntity = null;
			_activeAgents.Remove(this);
		}

		public static void DespawnAll()
		{
			if (_root != null)
			{
				_root.Remove(0);
				_root = null;
				_rootScene = null;
			}
			foreach (FakeAgent fake in _activeAgents)
			{
#if !SERVER
				fake.ReleaseAgentVisuals();
#endif
				fake._body = null;
				fake._mountEntity = null;
				fake._moveDestination = null;
			}
			_activeAgents.Clear();
		}

		/// <summary>Loops a non-cyclic clip: the skeleton channel parameter is the normalized
		/// progress of the current clip (0..1, wraps on cyclic animations) - the clip is re-fired
		/// when it runs out, and cyclic clips (parameter wraps) are left looping natively.</summary>
		private struct ClipLooper
		{
			private string _clipName;
			private float _speed;
			private float _lastParameter;
			private bool _cyclic;

			public void Set(string clipName, float speed, bool loop)
			{
				_clipName = loop ? clipName : null;
				_speed = speed > 0f ? speed : 1f;
				_lastParameter = 0f;
				_cyclic = false;
			}

			public void Tick(Skeleton skeleton, float dt)
			{
				if (_clipName == null || skeleton == null) return;
				float parameter = skeleton.GetAnimationParameterAtChannel(0);
				if (parameter < _lastParameter - 0.5f)
				{
					// Wrapped around: cyclic animation, loops by itself - stop managing it.
					_cyclic = true;
				}
				_lastParameter = parameter;
				if (_cyclic || parameter < 0.99f) return;
				// Re-fire at the very end with a short blend-in so the restart crossfades
				// instead of snapping.
				skeleton.SetAnimationAtChannel(_clipName, 0, _speed, 0.25f);
				_lastParameter = 0f;
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
