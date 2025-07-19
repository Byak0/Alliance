using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using static Alliance.Common.Core.Configuration.Models.AllianceData;
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
		[ConfigProperty(label: "Characters", tooltip: "Characters to spawn.")]
		public List<CharacterToSpawn> Characters;
		[ConfigProperty(label: "Side", tooltip: "Which side the characters belongs to.")]
		public BattleSideEnum Side = BattleSideEnum.Defender;
		[ConfigProperty(label: "Formation", tooltip: "Formation to spawn the characters.")]
		public FormationClass Formation = FormationClass.Infantry;
		[ConfigProperty(label: "Position", tooltip: "Position to spawn the characters.")]
		public SerializableZone SpawnZone;
		[ConfigProperty(label: "Direction", tooltip: "Direction the characters will move to (only when using an order to move).")]
		public SerializableZone Direction;
		[ConfigProperty(label: "Order", tooltip: "Formation will be given this order.")]
		public MoveOrderType MoveOrder = MoveOrderType.Charge;
		[ConfigProperty(label: "Disposition", tooltip: "Formation arrangement like Line, Loose, Shieldwall, etc.")]
		public ArrangementOrderEnum Disposition = ArrangementOrderEnum.Line;

		public SpawnFormationAction() { }
	}

	[Serializable]
	public class CharacterToSpawn
	{
		[ConfigProperty(label: "Character", tooltip: "ID of the character to spawn.")]
		public string CharacterId = "mp_heavy_infantry_vlandia_troop";
		[ConfigProperty(label: "Number", tooltip: "Number of characters to spawn.")]
		public int SpawnCount = 1;
		[ConfigProperty(label: "IsPercentage", tooltip: "If true, Number will be treated as percentage of the current number of players.")]
		public bool IsPercentage = false;
		[ConfigProperty(label: "Difficulty", tooltip: "Difficulty of the characters.")]
		public Difficulty Difficulty = Difficulty.Normal;
		[ConfigProperty(label: "Health multiplier", tooltip: "Multiply health by this value.")]
		public float HealthMultiplier = 1f;

		public CharacterToSpawn() { }
	}
}