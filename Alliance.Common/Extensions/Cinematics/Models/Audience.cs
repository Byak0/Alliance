using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Targeting model describing which clients play a cinematic and how roles resolve.
	/// Carried by a <see cref="Cinematic"/> and overridable from <c>PlayCinematicAction</c>.
	/// </summary>
	[Serializable]
	public class Audience
	{
		[ConfigProperty(label: "Scope", tooltip: "Who sees this cinematic.")]
		public AudienceScope Scope = AudienceScope.All;

		[ConfigProperty(label: "Team", tooltip: "Used when Scope = Team.", dependency: "?Scope=Team")]
		public BattleSideEnum Team = BattleSideEnum.Attacker;

		[ConfigProperty(label: "Player names", tooltip: "MissionPeer display names, comma-separated. Used when Scope = Players.", dependency: "?Scope=Players")]
		public string PlayerNames = "";

		[ConfigProperty(label: "Relative to viewer", tooltip: "Resolve the \"Viewer\" role to each receiver's own agent. Used when Scope = RelativeToViewer.", dependency: "?Scope=RelativeToViewer")]
		public bool UseViewerOrigin = true;

		public Audience() { }

		public Audience(AudienceScope scope) => Scope = scope;

		// TODO : rework this scope to get NetworkPeer ref or at least Agents. Player names is very ineficient and error-prone.
		/// <summary>Splits <see cref="PlayerNames"/> into a trimmed, non-empty list.</summary>
		public List<string> GetPlayerNameList()
		{
			var result = new List<string>();
			if (string.IsNullOrEmpty(PlayerNames)) return result;
			foreach (string part in PlayerNames.Split(','))
			{
				string trimmed = part.Trim();
				if (trimmed.Length > 0) result.Add(trimmed);
			}
			return result;
		}
	}
}
