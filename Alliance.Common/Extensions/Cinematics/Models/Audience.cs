using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Targeting model describing which clients play a cinematic.
	/// </summary>
	[Serializable]
	public class Audience
	{
		[ConfigProperty(label: "Scope", tooltip: "Who sees this cinematic.")]
		public AudienceScope Scope = AudienceScope.All;

		[ConfigProperty(label: "Team", tooltip: "Used when Scope = Team.", dependency: "?Scope=Team")]
		public BattleSideEnum Team = BattleSideEnum.Attacker;

		[ConfigProperty(label: "Player variables", tooltip: "Value sources resolving to Agents - the players controlling those agents receive the cinematic. Used when Scope = Players.", dependency: "?Scope=Players")]
		public List<ValueSource<Agent>> PlayerVariables = new List<ValueSource<Agent>>();

		public Audience() { }

		public Audience(AudienceScope scope) => Scope = scope;
	}
}
