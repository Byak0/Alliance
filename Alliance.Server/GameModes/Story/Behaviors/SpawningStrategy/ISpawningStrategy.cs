using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy
{
	/// <summary>
	/// This interface defines the common strategy necessary for the spawn mechanic.
	/// Based on it, custom strategies can be implemented to catter everyone's needs.
	/// </summary>
	public interface ISpawningStrategy
	{
		float SpawningTimer { get; }

		// Methods for lifecycle management
		void Initialize(SpawnComponent spawnComponent, ScenarioSpawningBehavior spawnBehavior, SpawnFrameBehaviorBase defaultSpawnFrameBehavior);
		void OnTick(float dt);

		// Methods for spawning control
		bool AllowExternalSpawn();
		void StartSpawnSession();
		void EndSpawnSession();
		void PauseSpawnSession();
		void ResumeSpawnSession();

		// Methods for managing scene
		void OnClearScene();
		void OnLoadScene();

		// Methods for managing individual spawns
		bool CanPlayerSpawn(NetworkCommunicator player, MissionPeer peer);
		bool CanPlayerSelectCharacter(NetworkCommunicator player, BasicCharacterObject character);
		bool CanPlayerSelectLocation(NetworkCommunicator player, SpawnLocation location);
		void OnSpawn(Agent agent);
		void OnDespawn(Agent agent);
	}
}
