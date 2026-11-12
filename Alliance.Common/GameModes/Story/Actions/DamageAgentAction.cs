using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Damage agents in a zone.
	/// </summary>
	[Serializable]
	[PhrasePreview("Deal {Damage} damage to {Who}")]
	[PhraseTemplate("Deal {Damage} damage to {Who}")]
	public class DamageAgentAction : ActionBase
	{
		public ValueSource<List<Agent>> Who = new VariableValue<List<Agent>>();
		public ValueSource<int> Damage = new LiteralValue<int>(500);

		public DamageAgentAction() { }
	}
}
