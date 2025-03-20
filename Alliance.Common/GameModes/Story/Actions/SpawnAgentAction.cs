using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Spawn agent(s) with specific parameters.
	/// </summary>
	[Serializable]
	public class SpawnAgentAction : ActionBase
	{
		[ScenarioEditor(label: "Character", tooltip: "ID of the character to spawn.")]
		public string CharacterId = "mp_heavy_infantry_vlandia_troop";
		[ScenarioEditor(label: "Number", tooltip: "Number of characters to spawn.")]
		public int SpawnCount = 1;
		[ScenarioEditor(label: "IsPercentage", tooltip: "If true, Number will be treated as percentage of the current number of players.")]
		public bool IsPercentage = false;
		[ScenarioEditor(label: "Side", tooltip: "Which side the characters belongs to.")]
		public BattleSideEnum Side = BattleSideEnum.Defender;
		[ScenarioEditor(label: "Formation", tooltip: "Formation to spawn the characters.")]
		public FormationClass Formation = FormationClass.Infantry;
		[ScenarioEditor(label: "Difficulty", tooltip: "Difficulty of the characters.")]
		public SpawnHelper.Difficulty Difficulty = SpawnHelper.Difficulty.Normal;
		[ScenarioEditor(label: "Position", tooltip: "Position to spawn the characters.")]
		public SerializableZone SpawnZone;
		[ScenarioEditor(label: "Direction", tooltip: "Direction the characters will move to.")]
		public SerializableZone Direction;

		public SpawnAgentAction() { }
	}
}