using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for spawning agents.
	/// </summary>
	[OverrideAction(typeof(SpawnAgentAction))]
	public class Server_SpawnAgentAction : SpawnAgentAction
	{
		public Server_SpawnAgentAction() : base() { }

		public override ActionTask Execute()
		{
			Spawn();
			return ActionTask.CompletedTask;
		}

		private async void Spawn()
		{
			Team team = Side == BattleSideEnum.Defender ? Mission.Current.DefenderTeam : Mission.Current.AttackerTeam;
			string cultureId = Side == BattleSideEnum.Defender ? MultiplayerOptions.OptionType.CultureTeam2.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureId);
			BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(CharacterId);
			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Difficulty);
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;
			int spawnCount = SpawnCount?.Resolve(ctx, globals) ?? 0;
			int numbertoSpawn = IsPercentage ? 
				SpawnHelper.GetTroopCountFromPercentage(spawnCount) : 
				spawnCount;

			Zone spawnZone = SpawnZone?.Resolve(ctx, globals);
			Zone dirZone = Direction?.Resolve(ctx, globals);
			Vec3 spawnCenter = spawnZone != null ? spawnZone.ResolveCenter(ctx, globals) : Vec3.Zero;
			Vec3 dirCenter = dirZone != null ? dirZone.ResolveCenter(ctx, globals) : Vec3.Zero;
			float spawnRadius = spawnZone?.Radius ?? 0f;
			float dirRadius = dirZone?.Radius ?? 0f;

			for (int i = 0; i < numbertoSpawn; i++)
			{
				// Calculate random position in the SpawnZone
				Vec3 randomSpawnPosition = CoreUtils.GetRandomPositionWithinRadius(spawnCenter, spawnRadius);
				MatrixFrame position = new MatrixFrame(Mat3.Identity, randomSpawnPosition);

				// Calculate random position in the target zone
				Vec3 randomTargetPosition = CoreUtils.GetRandomPositionWithinRadius(dirCenter, dirRadius);
				WorldPosition target = randomTargetPosition.ToWorldPosition(Mission.Current.Scene);

				Agent agent = await SpawnHelper.SpawnBotAsync(team, culture, character, position, selectedFormation: (int)Formation, botDifficulty: difficulty);

				// Move the agent to the target position
				agent.SetScriptedPosition(ref target, false);
			}
		}
	}
}
