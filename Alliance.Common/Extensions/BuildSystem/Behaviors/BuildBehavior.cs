using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.BuildSystem.Behaviors
{
	/// <summary>
	/// Handle build behavior - Creating and placing custom buildings/prefabs, syncing them...
	/// Shared between server and client. The server is authoritative.
	/// </summary>
	public class BuildBehavior : MissionNetwork
	{
		public struct BuildEntry
		{
			public int Index;
			public string PrefabName;
			public MatrixFrame Frame;
			public GameEntity Entity;
		}

		public const int MAX_BUILD_ENTRIES = 10000;

		private readonly Dictionary<int, BuildEntry> _builtEntities = new();
		private readonly HashSet<UIntPtr> _trackedPointers = new();
		private int _nextBuildIndex;

		public IReadOnlyDictionary<int, BuildEntry> BuiltEntities => _builtEntities;

		public bool IsTracked(GameEntity entity) => entity != null && _trackedPointers.Contains(entity.WeakEntity.Pointer);

		/// <summary>Finds the BuildIndex of a tracked entity, if any. Linear scan (moves/deletes are infrequent).</summary>
		public bool TryGetBuildIndex(WeakGameEntity entity, out int buildIndex)
		{
			foreach (var kvp in _builtEntities)
			{
				if (kvp.Value.Entity != null && kvp.Value.Entity.WeakEntity == entity)
				{
					buildIndex = kvp.Key;
					return true;
				}
			}
			buildIndex = -1;
			return false;
		}

		public void BroadcastCreation(int buildIndex, string prefabName, MatrixFrame frame, GameEntity entity)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			if (TryGetRootMissionObjectId(entity, out MissionObjectId moId))
				GameNetwork.WriteMessage(new SyncPrefabCreation(buildIndex, prefabName, frame, moId));
			else
				GameNetwork.WriteMessage(new SyncPrefabCreation(buildIndex, prefabName, frame));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public void BroadcastMove(WeakGameEntity entity, MatrixFrame frame)
		{
			if (TryGetBuildIndex(entity, out int buildIndex))
			{
				SendBroadcast(new SyncEntityMove(buildIndex, frame));
				return;
			}
			AL_EntityMarker marker = entity.GetFirstScriptOfType<AL_EntityMarker>();
			if (marker != null && !string.IsNullOrEmpty(marker.RefId))
			{
				SendBroadcast(new SyncEntityMove(marker.RefId, frame));
				return;
			}
			Log("[BuildBehavior] Cannot sync move: entity is neither build-tracked nor marked.", LogLevel.Warning);
		}

		public void BroadcastDelete(int buildIndex) => SendBroadcast(new SyncEntityDelete(buildIndex));

		public void BroadcastDelete(WeakGameEntity entity)
		{
			if (TryGetBuildIndex(entity, out int buildIndex))
			{
				SendBroadcast(new SyncEntityDelete(buildIndex));
				return;
			}
			AL_EntityMarker marker = entity.GetFirstScriptOfType<AL_EntityMarker>();
			if (marker != null && !string.IsNullOrEmpty(marker.RefId))
			{
				SendBroadcast(new SyncEntityDelete(marker.RefId));
				return;
			}
			Log("[BuildBehavior] Cannot sync delete: entity is neither build-tracked nor marked.", LogLevel.Warning);
		}

		private static void SendBroadcast(GameNetworkMessage message)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(message);
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// Raised after an entity has been fully built/tracked AND its scripts
		/// have been lifecycle-initialized. Subscribers (e.g. VehicleView) can
		/// use this to register callbacks for dynamically spawned prefabs.
		/// </summary>
		public event Action<int, GameEntity> OnPrefabBuilt;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_builtEntities.Clear();
			_trackedPointers.Clear();
			_nextBuildIndex = 0;
			// If mission has rounds, add a listener to disable UsableMachines at the end of each round.
			MultiplayerRoundController roundComponent = Mission.Current?.GetMissionBehavior<MultiplayerRoundController>();
			if (roundComponent != null)
			{
				roundComponent.OnPreRoundEnding += DisableTrackedUsableMachines;
			}
		}

		public override void OnEndMissionInternal()
		{
			MultiplayerRoundController roundComponent = Mission.Current?.GetMissionBehavior<MultiplayerRoundController>();
			if (roundComponent != null)
			{
				roundComponent.OnPreRoundEnding -= DisableTrackedUsableMachines;
			}
			base.OnEndMissionInternal();
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			base.HandleNewClientAfterSynchronized(networkPeer);
			if (!GameNetwork.IsServer) return;

			foreach (var kvp in _builtEntities)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);

				if (TryGetRootMissionObjectId(kvp.Value.Entity, out MissionObjectId moId))
				{
					GameNetwork.WriteMessage(new SyncPrefabCreation(
						kvp.Value.Index, kvp.Value.PrefabName, kvp.Value.Frame, moId));
				}
				else
				{
					GameNetwork.WriteMessage(new SyncPrefabCreation(
						kvp.Value.Index, kvp.Value.PrefabName, kvp.Value.Frame));
				}

				GameNetwork.EndModuleEventAsServer();
			}
		}

		public int AllocateBuildIndex()
		{
			if (_nextBuildIndex >= MAX_BUILD_ENTRIES)
			{
				Log($"AllocateBuildIndex failed: reached maximum build entries ({MAX_BUILD_ENTRIES}).", LogLevel.Error);
				return -1;
			}
			return _nextBuildIndex++;
		}

		/// <summary>
		/// Instantiate a prefab in the scene and track it.
		/// For prefabs with MissionObject scripts, also registers them with the
		/// mission system and broadcasts CreateMissionObject for native client sync.
		/// </summary>
		public GameEntity BuildPrefab(int buildIndex, string prefabName, MatrixFrame matrixFrame)
		{
			if (Mission.Current?.Scene == null)
			{
				Log($"BuildPrefab failed: no active scene.", LogLevel.Error);
				return null;
			}

			GameEntity entity = GameEntity.Instantiate(Mission.Current.Scene, prefabName, true);
			if (entity == null)
			{
				Log($"BuildPrefab failed: could not instantiate prefab '{prefabName}'.", LogLevel.Error);
				return null;
			}

			// Set position BEFORE physics so shapes are preloaded at the correct location
			entity.SetGlobalFrame(matrixFrame);
			entity.SetMobility(GameEntity.Mobility.Dynamic);

			PreloadAndActivatePhysics(entity);

			// Register MissionObject scripts for multiplayer sync (server only)
			if (GameNetwork.IsServer)
			{
				RegisterMissionObjects(entity, prefabName, matrixFrame);

				// Activate physics through the MissionObject layer.
				// UsableMachine.SetPhysicsStateSynched also calls SetAbilityOfFaces
				// and StandingPoint.OnParentMachinePhysicsStateChanged, which are
				// required for the native interaction system (F key) to work on
				// dynamically spawned StandingPoints.
				UsableMachine machine = entity.GetFirstScriptOfType<UsableMachine>();
				if (machine != null)
				{
					machine.SetPhysicsStateSynched(true, true);
				}
			}

			// Complete the script lifecycle that dynamically spawned objects miss
			InvokeAfterMissionStart(entity);

			TrackEntry(buildIndex, prefabName, matrixFrame, entity);
			OnPrefabBuilt?.Invoke(buildIndex, entity);

			Log($"Prefab '{prefabName}' built (#{buildIndex}) at {matrixFrame.origin}.", LogLevel.Debug);
			return entity;
		}

		/// <summary>
		/// Track an existing entity that was already created by native CreateMissionObject.
		/// Called on clients for prefabs that have MissionObject scripts.
		/// </summary>
		public void TrackExistingEntity(int buildIndex, string prefabName, MatrixFrame frame, MissionObjectId missionObjectId)
		{
			MissionObject mo = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);

			if (mo != null)
			{
				GameEntity entity = GameEntity.CreateFromWeakEntity(mo.GameEntity);
				if (entity != null)
				{
					PreloadAndActivatePhysics(entity);

					// Native CreateMissionObject only calls OnInit, not AfterMissionStart.
					InvokeAfterMissionStart(entity);

					TrackEntry(buildIndex, prefabName, frame, entity);
					OnPrefabBuilt?.Invoke(buildIndex, entity);

					Log($"Tracked MissionObject prefab '{prefabName}' (#{buildIndex}, MO={missionObjectId.Id}, runtime={missionObjectId.CreatedAtRuntime}).", LogLevel.Debug);
					return;
				}
			}

			Log($"TrackExistingEntity: MissionObject (Id={missionObjectId.Id}, runtime={missionObjectId.CreatedAtRuntime}) not found for '{prefabName}'. " +
				$"Falling back to instantiation.", LogLevel.Warning);
			BuildPrefab(buildIndex, prefabName, frame);
		}

		private void TrackEntry(int buildIndex, string prefabName, MatrixFrame frame, GameEntity entity)
		{
			_builtEntities[buildIndex] = new BuildEntry
			{
				Index = buildIndex,
				PrefabName = prefabName,
				Frame = frame,
				Entity = entity
			};
			if (entity != null) _trackedPointers.Add(entity.WeakEntity.Pointer);

			if (buildIndex >= _nextBuildIndex)
			{
				_nextBuildIndex = buildIndex + 1;
			}
		}

		/// <summary>
		/// Dynamically spawned scripts only receive OnInit (via callScripts=true
		/// or native CreateMissionObject). The mission lifecycle method
		/// AfterMissionStart has already executed for pre-placed objects,
		/// so we must invoke it manually for late-spawned ones.
		/// This is critical for UsableMachine / StandingPoint to finish their
		/// internal setup (interaction registration, etc.).
		/// </summary>
		private void InvokeAfterMissionStart(GameEntity entity)
		{
			List<GameEntity> allEntities = new List<GameEntity>();
			entity.GetChildrenRecursive(ref allEntities);
			allEntities.Add(entity);

			foreach (GameEntity e in allEntities)
			{
				MissionObject mo = e.GetFirstScriptOfType<MissionObject>();
				if (mo != null)
				{
					mo.AfterMissionStart();
				}
			}
		}

		/// <summary>
		/// Register MissionObject scripts with the engine so they are properly synced
		/// to clients via native CreateMissionObject.
		/// Same pattern as FlagTrackerBehavior.
		/// </summary>
		private void RegisterMissionObjects(GameEntity entity, string prefabName, MatrixFrame frame)
		{
			MissionObject rootMo = entity.GetFirstScriptOfType<MissionObject>();
			if (rootMo == null) return;

			List<MissionObjectId> childIds = new List<MissionObjectId>();
			CollectChildMissionObjectIdsRecursive(entity, childIds);

			Log($"[Server] RegisterMissionObjects '{prefabName}': root={rootMo.GetType().Name} Id={rootMo.Id.Id}, {childIds.Count} recursive children.", LogLevel.Debug);

			// Broadcast native CreateMissionObject for proper client-side instantiation
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CreateMissionObject(rootMo.Id, prefabName, frame, childIds));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);

			// Register with mission for late-joiner sync and recording
			Mission.Current.AddDynamicallySpawnedMissionObjectInfo(
				new Mission.DynamicallyCreatedEntity(prefabName, rootMo.Id, frame, ref childIds));

			Log($"Registered MissionObject '{prefabName}' (Id={rootMo.Id.Id}, runtime={rootMo.Id.CreatedAtRuntime}, {childIds.Count} children).", LogLevel.Debug);
		}

		private void CollectChildMissionObjectIdsRecursive(GameEntity entity, List<MissionObjectId> childIds)
		{
			foreach (GameEntity child in entity.GetChildren())
			{
				MissionObject childMo = child.GetFirstScriptOfType<MissionObject>();
				if (childMo != null)
				{
					childIds.Add(childMo.Id);
					Log($"  [Server] Child MO: {childMo.GetType().Name} on '{child.Name}' => Id={childMo.Id.Id}, runtime={childMo.Id.CreatedAtRuntime}", LogLevel.Debug);
				}

				CollectChildMissionObjectIdsRecursive(child, childIds);
			}
		}

		/// <summary>
		/// Returns the full MissionObjectId of the root MissionObject, or false if none.
		/// </summary>
		public static bool TryGetRootMissionObjectId(GameEntity entity, out MissionObjectId id)
		{
			MissionObject mo = entity?.GetFirstScriptOfType<MissionObject>();
			if (mo != null)
			{
				id = mo.Id;
				return true;
			}
			id = default;
			return false;
		}

		private void PreloadAndActivatePhysics(GameEntity entity)
		{
			List<GameEntity> allEntities = new List<GameEntity>();
			entity.GetChildrenRecursive(ref allEntities);
			allEntities.Add(entity);

			bool hasPhysics = false;
			foreach (GameEntity child in allEntities)
			{
				PhysicsShape bodyShape = child.GetBodyShape();
				if (bodyShape != null)
				{
					PhysicsShape.AddPreloadQueueWithName(bodyShape.GetName(), child.GetGlobalScale());
					hasPhysics = true;
				}
			}

			if (hasPhysics)
			{
				PhysicsShape.ProcessPreloadQueue();
			}

			entity.AddBodyFlags(BodyFlags.Moveable, true);
			entity.SetPhysicsState(true, true);
			entity.SetFrameChanged();
		}

		private static List<GameEntity> GetAllEntitiesRecursive(GameEntity rootEntity)
		{
			List<GameEntity> allEntities = new List<GameEntity>();
			if (rootEntity == null)
			{
				return allEntities;
			}

			rootEntity.GetChildrenRecursive(ref allEntities);
			allEntities.Add(rootEntity);
			return allEntities;
		}

		private static void DisableUsableMachines(GameEntity rootEntity)
		{
			HashSet<UsableMachine> usableMachines = new HashSet<UsableMachine>();
			foreach (GameEntity entity in GetAllEntitiesRecursive(rootEntity))
			{
				UsableMachine machine = entity.GetFirstScriptOfType<UsableMachine>();
				if (machine != null)
				{
					usableMachines.Add(machine);
				}
			}

			foreach (UsableMachine machine in usableMachines)
			{
				machine.Disable();
			}
		}

		private void DisableTrackedUsableMachines()
		{
			foreach (BuildEntry entry in _builtEntities.Values)
			{
				if (entry.Entity != null)
				{
					DisableUsableMachines(entry.Entity);
				}
			}
		}

		public bool RemovePrefab(int buildIndex)
		{
			if (!_builtEntities.TryGetValue(buildIndex, out BuildEntry entry))
			{
				Log($"RemovePrefab failed: build #{buildIndex} not found.", LogLevel.Warning);
				return false;
			}

			if (entry.Entity != null)
			{
				_trackedPointers.Remove(entry.Entity.WeakEntity.Pointer);
				DisableUsableMachines(entry.Entity);
				entry.Entity.SetVisibilityExcludeParents(false);
				entry.Entity.RemoveAllChildren();
				entry.Entity.Remove(0);
			}

			_builtEntities.Remove(buildIndex);
			Log($"Prefab '{entry.PrefabName}' (#{buildIndex}) removed.", LogLevel.Debug);
			return true;
		}
	}
}