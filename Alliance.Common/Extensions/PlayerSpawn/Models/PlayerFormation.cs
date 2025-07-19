using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

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
		[XmlIgnore]
		public HashSet<CandidateInfo> MyVotes = new();
		[XmlIgnore]
		private string _mainLanguage = LocalizationHelper.GetCurrentLanguage();
		[XmlIgnore]
		public string MainLanguage
		{
			get => _mainLanguage;
			set
			{
				if (value != _mainLanguage)
				{
					_mainLanguage = value;
					OnLanguageChanged?.Invoke(this, _mainLanguage);
				}
			}
		}

		// Events for UI updates
		public event Action<PlayerFormation, string> OnLanguageChanged;
		public event Action<PlayerFormation> OnOfficerCandidaciesUpdated;
		public event Action<PlayerFormation> OnOfficerUpdated;

		public AvailableCharacter AddCharacter(string characterId, bool officer)
		{
			AvailableCharacter character = new AvailableCharacter
			{
				CharacterId = characterId,
				Officer = officer
			};
			return AddCharacter(character);
		}

		public AvailableCharacter AddCharacter(AvailableCharacter character)
		{
			character.Index = PlayerSpawnMenu.GetNextCharacterIndex(this);
			AvailableCharacters.Add(character);
			return character;
		}

		public void AddMember(NetworkCommunicator member)
		{
			if (member != null && !Members.Contains(member))
			{
				Members.Add(member);
			}
		}

		public void RemoveMember(NetworkCommunicator member)
		{
			if (member != null && Members.Contains(member)) Members.Remove(member);
			// Remove member from other relevant collections
			if (Officer == member) Officer = null;
			if (CandidateInfo.ContainsKey(member)) RemoveCandidate(CandidateInfo[member]);
			if (PlayerVotes.ContainsKey(member))
			{
				foreach (var candidate in PlayerVotes[member])
				{
					candidate.Votes--;
				}
				PlayerVotes.Remove(member);
			}
		}

		public void AddCandidate(NetworkCommunicator candidate, string pitch)
		{
			CandidateInfo info = new CandidateInfo(candidate, pitch);
			if (!Members.Contains(info.Candidate))
			{
				Log("Cannot add a candidate who is not a member of the formation.", LogLevel.Error);
				return;
			}
			if (info != null && !Candidates.Contains(info))
			{
				Candidates.Add(info);
				CandidateInfo[info.Candidate] = info;
				OnOfficerCandidaciesUpdated?.Invoke(this);
			}
		}

		public void RemoveCandidate(CandidateInfo info)
		{
			if (info != null && Candidates.Contains(info))
			{
				Candidates.Remove(info);
				CandidateInfo.Remove(info.Candidate);
				OnOfficerCandidaciesUpdated?.Invoke(this);
			}
		}

		public void AddVote(NetworkCommunicator player, CandidateInfo candidate)
		{
			if (player == null || candidate == null || !Members.Contains(player) || !Candidates.Contains(candidate))
			{
				Log("Invalid vote attempt: Player or candidate is null, or player is not a member of the formation.", LogLevel.Error);
				return;
			}
			if (!PlayerVotes.ContainsKey(player))
			{
				PlayerVotes[player] = new HashSet<CandidateInfo>();
			}
			if (!PlayerVotes[player].Contains(candidate))
			{
				PlayerVotes[player].Add(candidate);
				candidate.Votes++;
			}
		}

		public void RemoveVote(NetworkCommunicator player, CandidateInfo candidate)
		{
			if (player == null || candidate == null || !PlayerVotes.ContainsKey(player) || !PlayerVotes[player].Contains(candidate))
			{
				Log("Invalid vote removal attempt: Player or candidate is null, or player has not voted for this candidate.", LogLevel.Error);
				return;
			}
			PlayerVotes[player].Remove(candidate);
			candidate.Votes--;
		}

		public void RefreshMainLanguage()
		{
			// Calculate the main language based on members' preferences
			Dictionary<int, int> languagePreferences = new Dictionary<int, int>();
			foreach (NetworkCommunicator member in Members)
			{
				int preferredLanguage = UserConfigs.Instance.GetConfig(member).PreferredLanguageIndex;
				if (!languagePreferences.ContainsKey(preferredLanguage)) languagePreferences[preferredLanguage] = 0;
				languagePreferences[preferredLanguage]++;
			}
			if (languagePreferences.Count > 0)
			{
				MainLanguage = LocalizationHelper.GetLanguage(languagePreferences.OrderByDescending(kvp => kvp.Value).First().Key);
			}
		}

		public void SetOfficer(NetworkCommunicator officer)
		{
			if (officer != null && !Members.Contains(officer))
			{
				Log("Cannot set an officer who is not a member of the formation.", LogLevel.Error);
				return;
			}
			Officer = officer;
			OnOfficerUpdated?.Invoke(this);
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
