using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Actions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using MathF = System.MathF;

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
			Team team = Side == BattleSideEnum.Defender ? Mission.Current.DefenderTeam : Mission.Current.AttackerTeam;
			string cultureId = Side == BattleSideEnum.Defender ? MultiplayerOptions.OptionType.CultureTeam2.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureId);
			BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(Character);
			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Difficulty);

			for (int i = 0; i < Number; i++)
			{
				// Calculate random position in the SpawnZone
				Vec3 randomSpawnPosition = GetRandomPositionWithinRadius(SpawnZone.Position, SpawnZone.Radius);
				MatrixFrame position = new MatrixFrame(Mat3.Identity, randomSpawnPosition);

				// Calculate random position in the target zone
				Vec3 randomTargetPosition = GetRandomPositionWithinRadius(Direction.Position, Direction.Radius);
				WorldPosition target = randomTargetPosition.ToWorldPosition(Mission.Current.Scene);

				SpawnHelper.SpawnBot(out Agent agent, team, culture, character, position, selectedFormation: (int)Formation, botDifficulty: difficulty);

				// Move the agent to the target position
				agent.SetScriptedPosition(ref target, false);
			}
		}

		/// <summary>
		/// Returns a random position within the given center and radius.
		/// </summary>
		private Vec3 GetRandomPositionWithinRadius(Vec3 center, float radius)
		{
			// Generate random angle and distance within the radius
			float angle = MBRandom.RandomFloat * MathF.PI * 2; // Random angle between 0 and 360 degrees
			float distance = MBRandom.RandomFloat * radius; // Random distance within the radius

			// Calculate X and Y coordinates
			float x = center.x + MathF.Cos(angle) * distance;
			float y = center.y + MathF.Sin(angle) * distance;

			// Return the new position (Z remains unchanged)
			return new Vec3(x, y, center.z);
		}
	}
}