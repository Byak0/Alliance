using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.Extensions.BuildSystem.Configuration;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromClient;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.BuildSystem.Views
{
	/// <summary>
	/// View responsible for the build system.
	/// Handles ghost preview, prefab selection, placement and removal.
	/// Only accessible by admins.
	/// </summary>
	[DefaultView]
	public class BuildSystemView : MissionView, IUseKeyBinder
	{
		private static string KeyCategoryId = "build";
		BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
		{
			CategoryId = KeyCategoryId,
			Category = "Alliance - Build",
			Keys = new List<BindedKey>()
			{
				new BindedKey()
				{
					Id = "key_build_place",
					Description = "Toggle build mode",
					Name = "Toggle build mode",
					DefaultInputKey = InputKey.K,
				},
				new BindedKey()
				{
					Id = "key_build_confirm",
					Description = "Confirm placement",
					Name = "Confirm placement",
					DefaultInputKey = InputKey.LeftMouseButton,
				},
				new BindedKey()
				{
					Id = "key_build_cancel",
					Description = "Cancel",
					Name = "Cancel",
					DefaultInputKey = InputKey.RightMouseButton,
				},
				new BindedKey()
				{
					Id = "key_build_rotate",
					Description = "Rotate building",
					Name = "Rotate building",
					DefaultInputKey = InputKey.MiddleMouseButton,
				},
				new BindedKey()
				{
					Id = "key_build_next",
					Description = "Next prefab",
					Name = "Next prefab",
					DefaultInputKey = InputKey.MouseScrollUp,
				},
				new BindedKey()
				{
					Id = "key_build_prev",
					Description = "Previous prefab",
					Name = "Previous prefab",
					DefaultInputKey = InputKey.MouseScrollDown,
				},
				new BindedKey()
				{
					Id = "key_build_delete",
					Description = "Delete nearest building",
					Name = "Delete nearest building",
					DefaultInputKey = InputKey.Delete,
				}
			}
		};

		private List<string> AvailablePrefabs = new List<string>();

		private GameKey _buildKey;
		private GameKey _confirmKey;
		private GameKey _cancelKey;
		private GameKey _rotateKey;
		private GameKey _nextKey;
		private GameKey _prevKey;
		private GameKey _deleteKey;

		private bool _isBuildMode;
		private int _selectedPrefabIndex;
		private float _rotationAngle;
		private GameEntity _ghostEntity;

		private const float ROTATION_STEP = 15f; // degrees per click
		private const float MAX_BUILD_DISTANCE = 50f;

		public override void EarlyStart()
		{
			var keys = HotKeyManager.GetCategory(KeyCategoryId).RegisteredGameKeys;
			_buildKey = keys.Find(gk => gk != null && gk.StringId == "key_build_place");
			_confirmKey = keys.Find(gk => gk != null && gk.StringId == "key_build_confirm");
			_cancelKey = keys.Find(gk => gk != null && gk.StringId == "key_build_cancel");
			_rotateKey = keys.Find(gk => gk != null && gk.StringId == "key_build_rotate");
			_nextKey = keys.Find(gk => gk != null && gk.StringId == "key_build_next");
			_prevKey = keys.Find(gk => gk != null && gk.StringId == "key_build_prev");
			_deleteKey = keys.Find(gk => gk != null && gk.StringId == "key_build_delete");

			SetAvailablePrefabs(BuildPrefabCatalogManager.GetActivePrefabIds());
		}

		public override void OnMissionScreenTick(float dt)
		{
			if (MBNetwork.MyPeer == null || !MBNetwork.MyPeer.IsAdmin()) return;

			if (IsKeyPressed(_buildKey)) ToggleBuildMode();

			if (!_isBuildMode) return;

			// Prefab selection
			if (IsKeyPressed(_nextKey)) CyclePrefab(1);
			if (IsKeyPressed(_prevKey)) CyclePrefab(-1);

			if (IsKeyPressed(_rotateKey)) _rotationAngle += ROTATION_STEP;

			UpdateGhostPreview();

			if (IsKeyPressed(_confirmKey)) ConfirmPlacement();
			if (IsKeyPressed(_cancelKey)) ExitBuildMode();
			if (IsKeyPressed(_deleteKey)) DeleteNearestBuilding();
		}

		private bool IsKeyPressed(GameKey key)
		{
			return Input.IsKeyPressed(key.KeyboardKey.InputKey) || Input.IsKeyPressed(key.ControllerKey.InputKey);
		}

		private void ToggleBuildMode()
		{
			if (_isBuildMode)
			{
				ExitBuildMode();
			}
			else
			{
				EnterBuildMode();
			}
		}

		private void EnterBuildMode()
		{
			SetAvailablePrefabs(BuildPrefabCatalogManager.GetActivePrefabIds());

			if (AvailablePrefabs.Count == 0)
			{
				Log("[BuildSystem] Cannot enter build mode: no prefab available.", LogLevel.Warning);
				return;
			}

			_isBuildMode = true;
			_selectedPrefabIndex = 0;
			_rotationAngle = 0f;
			CreateGhostEntity();
			Log("Build mode ON - Use scroll to select, click to place, right-click to cancel.", LogLevel.Information);
		}

		private void ExitBuildMode()
		{
			_isBuildMode = false;
			DestroyGhostEntity();
			Log("Build mode OFF.", LogLevel.Information);
		}

		private void CyclePrefab(int direction)
		{
			if (AvailablePrefabs.Count == 0) return;

			_selectedPrefabIndex = (_selectedPrefabIndex + direction + AvailablePrefabs.Count) % AvailablePrefabs.Count;
			DestroyGhostEntity();
			CreateGhostEntity();
			Log($"Selected: {GetSelectedPrefabName()}", LogLevel.Debug);
		}

		private void CreateGhostEntity()
		{
			if (Mission.Current?.Scene == null || AvailablePrefabs.Count == 0) return;

			string prefabName = GetSelectedPrefabName();
			_ghostEntity = GameEntity.Instantiate(Mission.Current.Scene, prefabName, true, false);

			if (_ghostEntity == null)
			{
				Log($"[BuildSystem] Failed to instantiate ghost prefab '{prefabName}'.", LogLevel.Warning);
				return;
			}

			_ghostEntity.SetVisibilityExcludeParents(true);
			_ghostEntity.SetMobility(GameEntity.Mobility.Dynamic);
		}

		private void DestroyGhostEntity()
		{
			if (_ghostEntity != null)
			{
				_ghostEntity.SetVisibilityExcludeParents(false);
				_ghostEntity.RemoveAllChildren();
				_ghostEntity.Remove(0);
				_ghostEntity = null;
			}
		}

		private void UpdateGhostPreview()
		{
			if (_ghostEntity == null || MissionScreen == null) return;

			bool validPosition = MissionScreen.GetProjectedMousePositionOnGround(
				out Vec3 groundPosition,
				out Vec3 groundNormal,
				BodyFlags.CommonFocusRayCastExcludeFlags,
				false);

			if (!validPosition) return;

			MatrixFrame frame = MatrixFrame.Identity;
			float radians = _rotationAngle * MathF.PI / 180f;
			frame.rotation.RotateAboutUp(radians);
			frame.origin = groundPosition;

			_ghostEntity.SetGlobalFrame(frame);
		}

		private void ConfirmPlacement()
		{
			if (_ghostEntity == null || AvailablePrefabs.Count == 0) return;

			string prefabName = GetSelectedPrefabName();
			MatrixFrame frame = _ghostEntity.GetGlobalFrame();

			SendBuildRequest(prefabName, frame);

			Log($"Build request sent: '{prefabName}'.", LogLevel.Debug);
		}

		private string GetSelectedPrefabName()
		{
			return AvailablePrefabs[_selectedPrefabIndex];
		}

		private static void SendBuildRequest(string prefabName, MatrixFrame frame)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestPrefabCreation(prefabName, frame));
			GameNetwork.EndModuleEventAsClient();
		}

		private void DeleteNearestBuilding()
		{
			BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			if (buildBehavior == null) return;

			Vec3 playerPos = Agent.Main?.Position ?? Mission.GetCameraFrame().origin;
			float closestDist = float.MaxValue;
			int closestIndex = -1;

			foreach (var kvp in buildBehavior.BuiltEntities)
			{
				if (kvp.Value.Entity == null) continue;
				float dist = kvp.Value.Entity.GetGlobalFrame().origin.Distance(playerPos);
				if (dist < closestDist)
				{
					closestDist = dist;
					closestIndex = kvp.Key;
				}
			}

			if (closestIndex < 0 || closestDist > MAX_BUILD_DISTANCE)
			{
				Log("No building nearby to delete.", LogLevel.Warning);
				return;
			}

			SendRemovalRequest(closestIndex);

			Log($"Removal request sent for building #{closestIndex}.", LogLevel.Debug);
		}

		private static void SendRemovalRequest(int buildIndex)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestPrefabRemoval(buildIndex));
			GameNetwork.EndModuleEventAsClient();
		}

		public override void OnMissionScreenFinalize()
		{
			DestroyGhostEntity();
			_isBuildMode = false;
		}

		public void SetAvailablePrefabs(List<string> prefabNames)
		{
			AvailablePrefabs = prefabNames?
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(System.StringComparer.OrdinalIgnoreCase)
				.ToList()
				?? new List<string>();

			if (AvailablePrefabs.Count == 0)
			{
				_selectedPrefabIndex = 0;
				DestroyGhostEntity();
				return;
			}

			if (_selectedPrefabIndex >= AvailablePrefabs.Count)
			{
				_selectedPrefabIndex = 0;
			}

			if (_isBuildMode)
			{
				DestroyGhostEntity();
				CreateGhostEntity();
			}
		}
	}
}
