using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Spawn agent(s) with specific parameters.
	/// </summary>
	[Serializable]
	public class SpawnFormationAction : ActionBase
	{
		[ScenarioEditor(label: "Characters", tooltip: "Characters to spawn.")]
		public List<CharacterToSpawn> Characters;
		[ScenarioEditor(label: "Side", tooltip: "Which side the characters belongs to.")]
		public BattleSideEnum Side = BattleSideEnum.Defender;
		[ScenarioEditor(label: "Formation", tooltip: "Formation to spawn the characters.")]
		public FormationClass Formation = FormationClass.Infantry;
		[ScenarioEditor(label: "Position", tooltip: "Position to spawn the characters.")]
		public SerializableZone SpawnZone;
		[ScenarioEditor(label: "Direction", tooltip: "Direction the characters will move to (only when using an order to move).")]
		public SerializableZone Direction;
		[ScenarioEditor(label: "Order", tooltip: "Formation will be given this order.")]
		public MoveOrderType MoveOrder = MoveOrderType.Charge;
		[ScenarioEditor(label: "Disposition", tooltip: "Formation arrangement like Line, Loose, Shieldwall, etc.")]
		public ArrangementOrderEnum Disposition = ArrangementOrderEnum.Line;

		public SpawnFormationAction() { }
	}

	[Serializable]
	public class CharacterToSpawn
	{
		[ScenarioEditor(label: "Character", tooltip: "ID of the character to spawn.")]
		public string Character = "mp_heavy_infantry_vlandia_troop";
		[ScenarioEditor(label: "Number", tooltip: "Number of characters to spawn.")]
		public int Number = 1;
		[ScenarioEditor(label: "Difficulty", tooltip: "Difficulty of the characters.")]
		public SpawnHelper.Difficulty Difficulty = SpawnHelper.Difficulty.Normal;
		[ScenarioEditor(label: "Health multiplier", tooltip: "Multiply health by this value.")]
		public float HealthMultiplier = 1f;

		public CharacterToSpawn() { }
	}
}