using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	/// <summary>
	/// Represents a team of players in the player spawn menu.
	/// </summary>
	[Serializable]
	public class PlayerTeam
	{
		/// XML-serializable properties
		public int Index { get; set; }
		public BattleSideEnum TeamSide { get; set; }
		public string Name { get; set; }
		public List<PlayerFormation> Formations { get; set; } = new();

		public PlayerFormation AddFormation(string name, FormationSettings settings = null, List<AvailableCharacter> chars = null)
		{
			PlayerFormation formation = new PlayerFormation()
			{
				Index = PlayerSpawnMenu.GetNextFormationIndex(this),
				Name = name,
				Settings = settings ?? new FormationSettings(),
				AvailableCharacters = chars ?? new List<AvailableCharacter>()
			};
			Formations.Add(formation);
			return formation;
		}

		public void RemoveFormation(PlayerFormation formation)
		{
			if (formation == null || !Formations.Contains(formation)) return;

			Formations.Remove(formation);
		}
	}
}
