using Alliance.Common.Core.Utils;
using System;
using System.Xml.Serialization;
using TaleWorlds.Core;
using static Alliance.Common.Core.Configuration.Models.AllianceData;
using static Alliance.Common.Core.Utils.Characters;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	/// <summary>
	/// Represents a character than can be selected and reserved by players in the player spawn menu.
	/// </summary>
	[Serializable]
	public class AvailableCharacter
	{
		/// XML-serializable properties
		public int Index { get; set; }
		public string CharacterId { get; set; }
		public bool Officer { get; set; } = false;
		public int SpawnCount { get; set; } = 100;
		public bool IsPercentage { get; set; } = true;
		public Difficulty Difficulty { get; set; } = Difficulty.Normal;
		public float HealthMultiplier { get; set; } = 1f;

		// Runtime properties
		[XmlIgnore]
		public BasicCharacterObject Character => Instance.GetCharacterObject(CharacterId);
		[XmlIgnore]
		public BasicCharacterStub CharacterStub => Instance.GetCharacterStub(CharacterId);
		[XmlIgnore]
		public string Name => Character?.Name.ToString() ?? CharacterStub?.Name.ToString() ?? "";
		[XmlIgnore]
		public BasicCultureObject Culture => Character?.Culture ?? CharacterStub?.Culture;
		[XmlIgnore]
		public int MaxSlots => SpawnCount * (IsPercentage ? CoreUtils.CurrentPlayerCount / 100 : 1);
		[XmlIgnore]
		public int UsedSlots { get; set; }
		[XmlIgnore]
		public int AvailableSlots => MaxSlots - UsedSlots;
	}
}
