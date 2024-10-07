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
		public string Character;
		[ScenarioEditor(label: "Number", tooltip: "Number of characters to spawn.")]
		public int Number;
		[ScenarioEditor(label: "Side", tooltip: "Which side the characters belongs to.")]
		public BattleSideEnum Side;
		[ScenarioEditor(label: "Formation", tooltip: "Formation to spawn the characters.")]
		public FormationClass Formation;
		[ScenarioEditor(label: "Difficulty", tooltip: "Difficulty of the characters.")]
		public SpawnHelper.Difficulty Difficulty;
		[ScenarioEditor(label: "Position", tooltip: "Position to spawn the characters.")]
		public SerializableZone SpawnZone;
		[ScenarioEditor(label: "Direction", tooltip: "Direction the characters will move to.")]
		public SerializableZone Direction;

		public SpawnAgentAction() { }
	}
}