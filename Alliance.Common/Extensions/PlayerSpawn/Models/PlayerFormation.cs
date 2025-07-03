using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	/// <summary>
	/// Represents a player formation that can be selected in the player spawn menu.
	/// </summary>
	[Serializable]
	public class PlayerFormation
	{
		/// XML-serializable properties
		public int Index { get; set; }
		public string Name { get; set; }
		public string MainCultureId { get; set; }
		public FormationSettings Settings { get; set; } = new();
		public List<AvailableCharacter> AvailableCharacters { get; set; } = new();

		// Runtime properties
		[XmlIgnore]
		public BasicCultureObject MainCulture => MBObjectManager.Instance.GetObject<BasicCultureObject>(MainCultureId ?? string.Empty);
		[XmlIgnore]
		public NetworkCommunicator Officer { get; private set; }
		[XmlIgnore]
		public readonly HashSet<CandidateInfo> Candidates = new();
		[XmlIgnore]
		public readonly HashSet<NetworkCommunicator> Members = new();
		[XmlIgnore]
		public readonly Dictionary<NetworkCommunicator, HashSet<CandidateInfo>> PlayerVotes = new();
		[XmlIgnore]
		public readonly Dictionary<NetworkCommunicator, CandidateInfo> CandidateInfo = new();

		public void SetOfficer(NetworkCommunicator officer)
		{
			Officer = officer;
		}

		public int GetOccupiedSlots()
		{
			int occupiedSlots = 0;
			foreach (var character in AvailableCharacters)
			{
				occupiedSlots += character.UsedSlots;
			}
			return occupiedSlots;
		}

		public int GetTotalSlots()
		{
			int totalSlots = 0;
			foreach (var character in AvailableCharacters)
			{
				totalSlots += character.MaxSlots;
			}
			return totalSlots;
		}

		public int GetAvailableSlots()
		{
			int availableSlots = 0;
			foreach (var character in AvailableCharacters)
			{
				availableSlots += character.AvailableSlots;
			}
			return availableSlots;
		}
	}
}
