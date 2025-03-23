using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Actions;
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
	public class Server_SpawnAgentAction : SpawnAgentAction
	{
		public Server_SpawnAgentAction() : base() { }

		public override void Execute()
		{
			Spawn();
		}

		private async void Spawn()
		{
			Team team = Side == BattleSideEnum.Defender ? Mission.Current.DefenderTeam : Mission.Current.AttackerTeam;
			string cultureId = Side == BattleSideEnum.Defender ? MultiplayerOptions.OptionType.CultureTeam2.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureId);
			BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(CharacterId);
			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Difficulty);
			int numbertoSpawn = IsPercentage ? (int)((SpawnCount / 100f) * CoreUtils.CurrentPlayerCount) : SpawnCount;

			for (int i = 0; i < numbertoSpawn; i++)
			{
				// Calculate random position in the SpawnZone
				Vec3 randomSpawnPosition = CoreUtils.GetRandomPositionWithinRadius(SpawnZone.GlobalPosition, SpawnZone.Radius);
				MatrixFrame position = new MatrixFrame(Mat3.Identity, randomSpawnPosition);

				// Calculate random position in the target zone
				Vec3 randomTargetPosition = CoreUtils.GetRandomPositionWithinRadius(Direction.GlobalPosition, Direction.Radius);
				WorldPosition target = randomTargetPosition.ToWorldPosition(Mission.Current.Scene);

				Agent agent = await SpawnHelper.SpawnBotAsync(team, culture, character, position, selectedFormation: (int)Formation, botDifficulty: difficulty);

				// Move the agent to the target position
				agent.SetScriptedPosition(ref target, false);
			}
		}
	}
}