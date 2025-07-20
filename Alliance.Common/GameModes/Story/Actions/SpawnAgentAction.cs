using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.Core;
using static Alliance.Common.Core.Configuration.Models.AllianceData;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Spawn agent(s) with specific parameters.
	/// </summary>
	[Serializable]
	public class SpawnAgentAction : ActionBase
	{
		[ConfigProperty(label: "Character", tooltip: "ID of the character to spawn.")]
		public string CharacterId = "mp_heavy_infantry_vlandia_troop";
		[ConfigProperty(label: "Number", tooltip: "Number of characters to spawn.")]
		public int SpawnCount = 1;
		[ConfigProperty(label: "IsPercentage", tooltip: "If true, Number will be treated as percentage of the current number of players.")]
		public bool IsPercentage = false;
		[ConfigProperty(label: "Side", tooltip: "Which side the characters belongs to.")]
		public BattleSideEnum Side = BattleSideEnum.Defender;
		[ConfigProperty(label: "Formation", tooltip: "Formation to spawn the characters.")]
		public FormationClass Formation = FormationClass.Infantry;
		[ConfigProperty(label: "Difficulty", tooltip: "Difficulty of the characters.")]
		public Difficulty Difficulty = Difficulty.Normal;
		[ConfigProperty(label: "Position", tooltip: "Position to spawn the characters.")]
		public SerializableZone SpawnZone;
		[ConfigProperty(label: "Direction", tooltip: "Direction the characters will move to.")]
		public SerializableZone Direction;

		public SpawnAgentAction() { }
	}
}