using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Client.Extensions.SAE.Models;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.SAE.Models;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.SAE.Behaviors
{
	[DefaultView]
	public class SaeBehavior : MissionView, IUseKeyBinder
	{
		private static string KeyCategoryId = "sae_spawn_cat";
		BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory
		{
			CategoryId = KeyCategoryId,
			Category = "Scatter Around Expanded",
			Keys = new List<BindedKey>
			{
				new BindedKey
				{
					Id = "key_delete_all_markers",
					Description = "Delete all placed markers.",
					Name = "Delete all markers",
					DefaultInputKey = InputKey.Numpad5
				},
				new BindedKey
				{
					Id = "key_create_marker",
					Description = "Create one marker.",
					Name = "Create marker",
					DefaultInputKey = InputKey.X
				},
				new BindedKey
				{
					Id = "key_delete_marker",
					Description = "Delete nearest marker from mouse position.",
					Name = "Delete marker",
					DefaultInputKey = InputKey.X
				},
				new BindedKey
				{
					Id = "key_fast_create_marker",
					Description = "Create multiple markers around cursor.",
					Name = "Create multiple",
					DefaultInputKey = InputKey.X
				},
				new BindedKey
				{
					Id = "key_fast_delete_marker",
					Description = "Delete multiple markers around cursor.",
					Name = "Delete multiple",
					DefaultInputKey = InputKey.X
				},
				new BindedKey
				{
					Id = "key_increase_sphere",
					Description = "Increase sphere radius.",
					Name = "Increase SAE sphere",
					DefaultInputKey = InputKey.PageUp
				},
				new BindedKey
				{
					Id = "key_decrease_sphere",
					Description = "Decrease sphere radius.",
					Name = "Decrease SAE sphere",
					DefaultInputKey = InputKey.PageDown
				},
				new BindedKey
				{
					Id = "key_crouch",
					Description = "Make selected formation crouch.",
					Name = "Crouch troops",
					DefaultInputKey = InputKey.M
				},
			}
		};

		private GameKey createMarkerIK;
		private GameKey deleteMarkerIK;
		private GameKey deleteAllMarkerIK;
		private GameKey fastCreateMarkerIK;
		private GameKey fastDeleteMarkerIK;
		private GameKey increaseSphereRadius;
		private GameKey decreaseSphereRadius;
		private GameKey crouchIk;


		/// <summary>
		/// This list contain all markers from player's team
		/// So, all his makers plus all friendly markers
		/// </summary>
		private List<SaeMarkerClientEntity> visualMarkersList;

		private List<GameEntity> fakeDynamicMarkers;

		private bool crouchedMode = true;
		private SaeVirtualCursorManager saeVirtualCursorManager;
		private SaeVirtualSphereManager saeVirtualSphereManager;

		private DateTime lastTimeMakerPosed;

		private bool _isModActivated = false;
		public bool IsModActivated
		{
			get
			{
				return _isModActivated;
			}

			set
			{
				if (!IsModActivated && value)
				{

					//Init mod
					MissionPeer.OnTeamChanged += OnPlayerChangeTeam;
					MultiplayerRoundComponent roundController = Mission.GetMissionBehavior<MultiplayerRoundComponent>();
					if (roundController != null) roundController.OnRoundEnding += ResetSAE;
					InitDynamicMarkers();

					_isModActivated = value;
				}
			}
		}

		private float saeSphereDiameter = 10f;
		public float SaeSphereDiameter
		{
			get { return saeSphereDiameter; }
			set
			{
				if (value < 3f)
				{
					saeSphereDiameter = 3f;
					return;
				}
				if (value > 60f)
				{
					saeSphereDiameter = 60f;
					return;
				}

				saeSphereDiameter = value;
			}
		}

		public List<SaeMarkerClientEntity> VisualMarkersList { get { return visualMarkersList; } }

		public SaeBehavior()
		{
			visualMarkersList = new List<SaeMarkerClientEntity>();
			lastTimeMakerPosed = DateTime.Now;
		}

		public override void EarlyStart()
		{
			createMarkerIK = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_create_marker");
			deleteMarkerIK = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_delete_marker");
			deleteAllMarkerIK = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_delete_all_markers");
			fastCreateMarkerIK = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_fast_create_marker");
			fastDeleteMarkerIK = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_fast_delete_marker");
			increaseSphereRadius = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_increase_sphere");
			decreaseSphereRadius = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_decrease_sphere");
			crouchIk = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_crouch");
		}

		private void InitDynamicMarkers()
		{
			fakeDynamicMarkers = new List<GameEntity>() { };
			List<GameEntity> gameEntities = new();
			Mission.Current.Scene.GetEntities(ref gameEntities);
			gameEntities.Where(entity => entity.HasTag(SaeCommonConstants.FDC_QUICK_PLACEMENT_POS_TAG_NAME)).ToList()
				.ForEach(entity =>
				{
					fakeDynamicMarkers.Add(entity);
				});
		}

		public override void OnMissionScreenFinalize()
		{
			if (IsModActivated)
			{
				MissionPeer.OnTeamChanged -= OnPlayerChangeTeam;
				MultiplayerRoundComponent roundController = Mission.GetMissionBehavior<MultiplayerRoundComponent>();
				if (roundController != null) roundController.OnRoundEnding -= ResetSAE;
				base.OnMissionScreenFinalize();
			}
		}

		public override void OnMissionTick(float dt)
		{
			IsModActivated = Config.Instance.ActivateSAE;

			if (IsModActivated)
			{
				base.OnMissionTick(dt);

				if (saeVirtualCursorManager == null)
				{
					saeVirtualCursorManager = new SaeVirtualCursorManager(MissionScreen);
				}
				else
				{
					saeVirtualCursorManager.HandleCursorVisibilityForOneTick();
				}

				if (saeVirtualSphereManager == null)
				{
					saeVirtualSphereManager = new SaeVirtualSphereManager(MissionScreen, fakeDynamicMarkers.Select(e => e.GetGlobalFrame()).ToList(), this);
				}
				else
				{
					saeVirtualSphereManager.HandleSphereVisibilityForOneTick();
				}

				//ActionTrigger
				if (Mission.Current.IsOrderMenuOpen)
				{
					OnSphereIncreasingOrDecreasingButtonPressed();
					WhenCreateMarkerActionTriggered();
					WhenDeleteMarkerActionTriggered();
					WhenDebugActionTriggered();
					WhenDynamicCreateMarkerActionTriggered();
				}

				WhenCrouchActionTriggered();
			}
		}

		private void OnSphereIncreasingOrDecreasingButtonPressed()
		{
			if (DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 700000 && Input.IsKeyDown(increaseSphereRadius.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				SaeSphereDiameter += 5f;
			}

			if (DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 700000 && Input.IsKeyDown(decreaseSphereRadius.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				SaeSphereDiameter -= 5f;
			}
		}

		private void OnPlayerChangeTeam(NetworkCommunicator peer, Team previousTeam, Team newTeam)
		{
			if (peer.IsMine && newTeam != null && (newTeam.Side == TaleWorlds.Core.BattleSideEnum.Defender || newTeam.Side == TaleWorlds.Core.BattleSideEnum.Attacker))
			{
				//Remove all visual markers
				RemoveMarkersFromList(visualMarkersList.Select(e => e.Id).ToList());

				//Ask server to set me new markers from the new team
				SendToServerToGetMarkers((int)newTeam.Side);
			}
		}

		private void ResetSAE()
		{
			List<int> ints = visualMarkersList.Select(e => e.Id).ToList();

			//Deactivate all markers
			RemoveMarkersFromList(ints);
		}

		private void WhenCrouchActionTriggered()
		{
			//Crouch troops
			if (Input.IsKeyPressed(crouchIk.KeyboardKey.InputKey))
			{
				Log("Crouch !", LogLevel.Information);

				MBReadOnlyList<Formation> formations = Mission.Current.PlayerTeam?.PlayerOrderController?.SelectedFormations;
				if (formations != null && formations.Count > 0)
				{
					foreach (Formation formation in formations)
					{
						GameNetwork.BeginModuleEventAsClient();
						GameNetwork.WriteMessage(new SaeCrouchNetworkClientMessage(formation, crouchedMode));
						GameNetwork.EndModuleEventAsClient();
					}
				}

				crouchedMode = !crouchedMode;
			}
		}

		private void WhenDynamicCreateMarkerActionTriggered()
		{
			if (Input.IsKeyDown(InputKey.LeftAlt))
			{
				//Show sphere area + hightlight markers in sphere

				if (!Input.IsKeyDown(InputKey.LeftControl) && saeVirtualCursorManager.CursorMarker != null && DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 1000000 && Input.IsKeyDown(fastCreateMarkerIK.KeyboardKey.InputKey))
				{
					lastTimeMakerPosed = DateTime.Now;
					SendToServerEligibleMarkersToSpawnInZone();
				}
				else if (Input.IsKeyDown(InputKey.LeftControl) && saeVirtualCursorManager.CursorMarker != null && DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 1000000 && Input.IsKeyDown(fastDeleteMarkerIK.KeyboardKey.InputKey))
				{
					lastTimeMakerPosed = DateTime.Now;
					SendToServerToDeleteEligibleMarkers();
				}
			}
		}

		private void SendToServerToDeleteEligibleMarkers()
		{
			//Merge fakeDynamicMarkers with VisualArrowEntity of visualMarkersList
			List<GameEntity> eligibleMakerPositionList = fakeDynamicMarkers.Concat(visualMarkersList.Select(x => x.VisualArrowEntity).ToList()).ToList();
			List<GameEntity> markersPositionsInRange = SphereSelection.SelectObjectsInSphere(eligibleMakerPositionList, saeVirtualCursorManager.CursorMarker.GlobalPosition, SaeSphereDiameter);

			SendToServerToDeleteMarker(
				visualMarkersList.Where(
					e => markersPositionsInRange.Exists(x => e.VisualArrowEntity.GlobalPosition.NearlyEquals(x.GlobalPosition, 0.5f))
				)
				.Select(e => e.Id)
				.ToList()
			);
		}

		private void SendToServerEligibleMarkersToSpawnInZone()
		{
			List<GameEntity> markersPositionsInRange = SphereSelection.SelectObjectsInSphere(fakeDynamicMarkers, saeVirtualCursorManager.CursorMarker.GlobalPosition, SaeSphereDiameter);

			markersPositionsInRange.RemoveAll(e => IsSaeMarkerAlreadyCreatedNearThisPosition(e.GlobalPosition, 0.2f));
			SendToServerToCreateDynamicMarker(markersPositionsInRange.Select(e => e.GetGlobalFrame()).ToList());
		}

		/// <summary>
		/// This method will return true if near the desired location, an SAE visual marker exist.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="maxDistanceToBeConsideredAsNear"></param>
		/// <returns></returns>
		private bool IsSaeMarkerAlreadyCreatedNearThisPosition(Vec3 position, float maxDistanceToBeConsideredAsNear)
		{
			return visualMarkersList.Exists(e => SphereSelection.CalculateDistance(e.VisualArrowEntity.GlobalPosition, position) <= maxDistanceToBeConsideredAsNear);
		}

		private void WhenCreateMarkerActionTriggered()
		{
			if (!Input.IsKeyDown(InputKey.LeftAlt) && !Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(createMarkerIK.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				SendToServerToCreateMarker();
			}

			if (!Input.IsKeyDown(InputKey.LeftAlt) && !Input.IsKeyDown(InputKey.LeftControl) && DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 1500000 && Input.IsKeyDown(createMarkerIK.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				SendToServerToCreateMarker();
			}
		}

		private void WhenDeleteMarkerActionTriggered()
		{
			if (Input.IsKeyDown(InputKey.LeftControl) && !Input.IsKeyDown(InputKey.LeftAlt) && Input.IsKeyPressed(deleteMarkerIK.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				GetNearestMarkerAndAskToDeleteIfNeeded();
			}

			if (Input.IsKeyDown(InputKey.LeftControl) && !Input.IsKeyDown(InputKey.LeftAlt) && DateTime.Now.Ticks - lastTimeMakerPosed.Ticks >= 1500000 && Input.IsKeyDown(deleteMarkerIK.KeyboardKey.InputKey))
			{
				lastTimeMakerPosed = DateTime.Now;
				GetNearestMarkerAndAskToDeleteIfNeeded();
			}

			if (Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(deleteAllMarkerIK.KeyboardKey.InputKey))
			{
				SendServerToDeleteAllMarkers();
			}
		}

		private void SendServerToDeleteAllMarkers()
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SaeDeleteAllMarkersForAllNetworkClientMessage());
			GameNetwork.EndModuleEventAsClient();
		}

		private void GetNearestMarkerAndAskToDeleteIfNeeded()
		{
			int markerIdToDelete = GetNearestMarkerIdFromCursor();
			if (markerIdToDelete != -1)
			{
				List<int> markers = new() { markerIdToDelete };
				SendToServerToDeleteMarker(markers);
			}
		}

		private void WhenDebugActionTriggered()
		{
			if (Input.IsKeyPressed(InputKey.G))
			{
				//SendToServerToPrintDebug();
			}
		}

		private void SendToServerToPrintDebug()
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SaeDebugNetworkClientMessage());
			GameNetwork.EndModuleEventAsClient();
		}

		private void SendToServerToGetMarkers(int side)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SaeGetMarkersNetworkClientMessage(side));
			GameNetwork.EndModuleEventAsClient();
		}

		private void SendToServerToCreateMarker()
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SaeCreateMarkerNetworkClientMessage(new List<MatrixFrame>() { saeVirtualCursorManager.CursorMarker.GetGlobalFrame() }));
			GameNetwork.EndModuleEventAsClient();
		}

		private void SendToServerToCreateDynamicMarker(List<MatrixFrame> positionToCreate)
		{
			if (positionToCreate.Count > 0)
			{
				int groupSize = 10;
				int totalElements = positionToCreate.Count;

				for (int i = 0; i < totalElements; i += groupSize)
				{
					int startIndex = i;
					int endIndex = Math.Min(i + groupSize, totalElements);

					List<MatrixFrame> group = positionToCreate.GetRange(startIndex, endIndex - startIndex);

					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new SaeCreateDynamicMarkerNetworkClientMessage(group));
					GameNetwork.EndModuleEventAsClient();
				}
			}
		}

		private static void SendToServerToDeleteMarker(List<int> markersIdToDelete)
		{
			if (markersIdToDelete.Count > 0)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new SaeDeleteMarkerForAllNetworkClientMessage(markersIdToDelete));
				GameNetwork.EndModuleEventAsClient();
			}
		}

		private int GetNearestMarkerIdFromCursor()
		{
			int markerId = -1;
			SaeMarkerClientEntity possibleBestMarker = null;

			visualMarkersList.ForEach(marker =>
			{
				if (possibleBestMarker != null)
				{
					if (marker.VisualArrowEntity.GlobalPosition.Distance(MissionScreen.GetOrderFlagPosition()) < possibleBestMarker.VisualArrowEntity.GlobalPosition.Distance(MissionScreen.GetOrderFlagPosition()))
					{
						possibleBestMarker = marker;
					}
				}
				else
				{
					possibleBestMarker = marker;
				}

				markerId = possibleBestMarker.Id;
			});

			return markerId;
		}

		public void AddMarkerFromList(List<SaeMarkerWithIdAndPos> markersToAdd)
		{
			markersToAdd.ForEach(marker =>
			{
				visualMarkersList.Add(new SaeMarkerClientEntity(marker.Id, marker.Pos, Mission.Current.IsOrderMenuOpen));
			});
		}

		public void RemoveMarkersFromList(List<int> markersToDelete)
		{
			//Remove all markers from the list of IDs
			Log("Markers before suppression = " + visualMarkersList.Count, LogLevel.Debug);

			visualMarkersList.FindAll(e => markersToDelete.Contains(e.Id)).ForEach(markerToDelete =>
			{
				//Remove it from game engine
				markerToDelete.VisualArrowEntity.RemoveAllChildren();
				markerToDelete.VisualArrowEntity.Remove(1);

				//Remove it from the list
				visualMarkersList.Remove(markerToDelete);
			});

			Log("Markers after suppression = " + visualMarkersList.Count, LogLevel.Debug);
		}

		public MatrixFrame GetMousePosition()
		{
			MissionScreen.GetProjectedMousePositionOnGround(out var groundPos, out _, BodyFlags.BodyOwnerFlora, true);
			return new(Mat3.Identity, groundPos);
		}
	}

	internal class SaeVirtualSphereManager
	{
		private GameEntity sphereEntity;
		MissionScreen missionScreen;

		/// <summary>
		/// List of all ghost that are at the same position than dynamic markers
		/// </summary>
		List<GameEntity> hightLightGhostList = new List<GameEntity>();
		SaeBehavior saeBehavior;

		public GameEntity SphereEntity { get { return sphereEntity; } }

		//Indicate if custom marker have been updated visually to be shown or not
		private bool isSupposedToBeInvisibleBecauseOfNonAltPressed = true;

		public SaeVirtualSphereManager(MissionScreen missionScreen, List<MatrixFrame> positionOfGhostList, SaeBehavior sae)
		{
			this.missionScreen = missionScreen;
			saeBehavior = sae;
			positionOfGhostList.ForEach(p => hightLightGhostList.Add(CreateInvisibleGhost(p)));
		}

		public void HandleSphereVisibilityForOneTick()
		{
			if (Mission.Current.MainAgent != null && missionScreen != null)
			{
				if (Input.IsKeyDown(InputKey.LeftAlt) && Mission.Current.IsOrderMenuOpen && Mission.Current.IsOrderMenuOpen)
				{
					isSupposedToBeInvisibleBecauseOfNonAltPressed = true;
					InitSphereIfNotExisting();
					UpdateSpherePosition();
					HandleGhostVisibility();
				}
				//When exit order menu arrow disappear
				else if (isSupposedToBeInvisibleBecauseOfNonAltPressed)
				{
					isSupposedToBeInvisibleBecauseOfNonAltPressed = false;
					//Make everything invisible
					hightLightGhostList.ForEach(e => e.SetVisibilityExcludeParents(false));
					sphereEntity?.SetVisibilityExcludeParents(false);
				}
			}
		}

		private GameEntity CreateInvisibleGhost(MatrixFrame position)
		{
			GameEntity ghost = GameEntity.Instantiate(Mission.Current.Scene, SaeCommonConstants.FDC_QUICK_PLACEMENT_POS_INDICATOR_PREFAB_NAME, position);

			ghost.SetVisibilityExcludeParents(false);
			ghost.GetGlobalScale().Normalize();
			ghost.SetMobility(GameEntity.Mobility.stationary);

			return ghost;
		}


		/// <summary>
		/// Toggle marker visibility
		/// </summary>
		private void HandleGhostVisibility()
		{
			//Apply glow for markers in my sphere
			List<GameEntity> allMarkersInSphere = hightLightGhostList.Where(x =>
				SphereSelection.SelectObjectsInSphere(x, missionScreen.GetOrderFlagPosition(), saeBehavior.SaeSphereDiameter) &&
				!saeBehavior.VisualMarkersList.Exists(v => x.GlobalPosition.NearlyEquals(v.VisualArrowEntity.GlobalPosition, 0.5f))
				)
				.ToList();

			allMarkersInSphere.ForEach(e =>
			{
				if (!e.IsVisibleIncludeParents())
				{
					e.SetVisibilityExcludeParents(true);
					e.SetContourColor(new Color(0, 1, 0, 1).ToUnsignedInteger(), true);
				}
			});

			//Disable glow for others
			hightLightGhostList.Where(x => !allMarkersInSphere.Contains(x))
				.ToList()
				.ForEach(e =>
				{
					if (e.IsVisibleIncludeParents())
					{
						e.SetContourColor(null, false);
						e.SetVisibilityExcludeParents(false);
					}
				});
		}

		/// <summary>
		/// Arrow follow cursor
		/// </summary>
		public void InitSphereIfNotExisting()
		{
			if (sphereEntity == null)
			{
				//Try to init cursorMarker
				if (missionScreen != null && Mission.Current != null && Mission.Current.MainAgent != null)
				{
					sphereEntity = CreateSphere(SaeConstants.VISUAL_SPHERE_PREFAB_NAME);
				}
				else
				{
					Log("Can't init sphereEntity due to missing properties", LogLevel.Error);
				}
			}
		}

		private GameEntity CreateSphere(string spherePrefabName)
		{
			Mat3 rotation = Mat3.Identity;
			Vec3 flagOrientation = missionScreen.GetOrderFlagPosition();
			MatrixFrame markworldFrame = new(rotation, flagOrientation);
			markworldFrame.Scale(new Vec3(saeBehavior.SaeSphereDiameter, saeBehavior.SaeSphereDiameter, saeBehavior.SaeSphereDiameter));

			sphereEntity = GameEntity.Instantiate(Mission.Current.Scene, spherePrefabName, markworldFrame);

			sphereEntity.SetVisibilityExcludeParents(true);
			sphereEntity.GetGlobalScale().Normalize();
			sphereEntity.SetMobility(GameEntity.Mobility.dynamic);

			return sphereEntity;
		}

		private void UpdateSpherePosition()
		{
			Vec3 flagOrientation = missionScreen.GetOrderFlagPosition();
			MatrixFrame markworldFrame = new(Mat3.Identity, flagOrientation);
			markworldFrame.Scale(new Vec3(saeBehavior.SaeSphereDiameter, saeBehavior.SaeSphereDiameter, saeBehavior.SaeSphereDiameter));
			sphereEntity.SetGlobalFrame(markworldFrame);
			if (!sphereEntity.IsVisibleIncludeParents())
			{
				sphereEntity.SetVisibilityExcludeParents(true);
			}
		}
	}

	internal class SphereSelection
	{
		public static List<GameEntity> SelectObjectsInSphere(List<GameEntity> objects, Vec3 center, float diameter)
		{
			float radius = diameter / 2;
			List<GameEntity> selectedObjects = new();

			foreach (GameEntity obj in objects)
			{
				float distance = CalculateDistance(obj.GlobalPosition, center);
				if (distance <= radius)
				{
					selectedObjects.Add(obj);
				}
			}

			return selectedObjects;
		}
		public static bool SelectObjectsInSphere(GameEntity obj, Vec3 center, float diameter)
		{
			float radius = diameter / 2;

			float distance = CalculateDistance(obj.GlobalPosition, center);
			if (distance <= radius)
			{
				return true;
			}

			return false;
		}

		public static float CalculateDistance(Vec3 obj, Vec3 center)
		{
			float deltaX = obj.X - center.X;
			float deltaY = obj.Y - center.Y;
			float deltaZ = obj.Z - center.Z;

			return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
		}
	}

	internal class SaeVirtualCursorManager
	{
		private GameEntity cursorMarker;
		private Mat3 lastKnowRotationForVisualMarker = Mat3.Identity;
		MissionScreen missionScreen;

		public GameEntity CursorMarker { get { return cursorMarker; } }

		//Indicate if custom marker have been updated visually to be shown or not
		private bool isCustomMarkersShown = false;

		public SaeVirtualCursorManager(MissionScreen missionScreen)
		{
			this.missionScreen = missionScreen;
		}

		public void HandleCursorVisibilityForOneTick()
		{
			if (Mission.Current.MainAgent != null && missionScreen != null)
			{

				if (Mission.Current.IsOrderMenuOpen)
				{
					InitMarkerIfNotExisting();
					ManageLogicWhenOrderMenuIsOpen();
					ShowBaseStratArea(true);
				}
				//When exit order menu arrow disappear
				else
				{
					ShowBaseStratArea(false);
				}
			}
		}

		private void ManageLogicWhenOrderMenuIsOpen()
		{
			//Update marker position
			UpdateCursorMarkerPositionAndRotation();

			//Rotate arrow cursor and placed strategic area
			if (Input.IsKeyDown(InputKey.Q))
			{
				RotateCursorMarker(3E-2f);
			}
			if (Input.IsKeyDown(InputKey.E))
			{
				RotateCursorMarker(-3E-2f);
			}

		}

		/// <summary>
		/// Rotate the main cursor
		/// </summary>
		public void RotateCursorMarker(float rotation)
		{
			cursorMarker.SetGlobalFrame(MarkerRotation(cursorMarker.GetGlobalFrame(), rotation));
			lastKnowRotationForVisualMarker = cursorMarker.GetFrame().rotation;
		}

		/// <summary>
		/// Is rotating about z axis MatrixFrame
		/// </summary>
		public static MatrixFrame MarkerRotation(MatrixFrame m, float angleDegree)
		{
			m.rotation.f.RotateAboutZ(angleDegree);
			m.rotation.u.RotateAboutZ(angleDegree);
			m.rotation.s.RotateAboutZ(angleDegree);
			return m;
		}


		/// <summary>
		/// Toggle marker visibility
		/// </summary>
		private void ShowBaseStratArea(bool ToF)
		{
			if (isCustomMarkersShown != ToF)
			{
				isCustomMarkersShown = ToF;

				//Toggle cursor marker visibility
				cursorMarker.SetVisibilityExcludeParents(ToF);

				var gameEntities = new List<GameEntity>();
				Mission.Current.Scene.GetEntities(ref gameEntities);
				foreach (GameEntity g in gameEntities)
				{
					if (g.HasTag(SaeConstants.MAIN_CURSOR) || g.HasTag(SaeConstants.VISUAL_STR_POS_TAG))
					{
						g.SetVisibilityExcludeParents(ToF);
					}
				}
			}
		}

		/// <summary>
		/// Arrow follow cursor
		/// </summary>
		public void InitMarkerIfNotExisting()
		{
			if (cursorMarker == null)
			{
				//Try to init cursorMarker
				if (missionScreen != null && Mission.Current != null && Mission.Current.MainAgent != null)
				{
					cursorMarker = CreateCursorMarker(SaeConstants.VISUAL_STR_POS_MOUSE_OBJ);
				}
				else
				{
					Log("Can't init Marker due to missing properties", LogLevel.Error);
				}
			}
		}

		private GameEntity CreateCursorMarker(string visualMarkerType)
		{
			Mat3 rotation = Mat3.Identity;
			Vec3 flagOrientation = missionScreen.GetOrderFlagPosition();
			MatrixFrame markworldFrame = new(rotation, flagOrientation);
			lastKnowRotationForVisualMarker = rotation;
			cursorMarker = GameEntity.Instantiate(Mission.Current.Scene, visualMarkerType, markworldFrame);

			cursorMarker.SetVisibilityExcludeParents(true);
			cursorMarker.GetGlobalScale().Normalize();
			cursorMarker.SetMobility(GameEntity.Mobility.dynamic);
			cursorMarker.AddTag(SaeConstants.MAIN_CURSOR);

			return cursorMarker;
		}

		private void UpdateCursorMarkerPositionAndRotation()
		{
			Vec3 flagOrientation = missionScreen.GetOrderFlagPosition();
			MatrixFrame markworldFrame = new(lastKnowRotationForVisualMarker, flagOrientation);
			cursorMarker.SetGlobalFrame(markworldFrame);
		}
	}

}
