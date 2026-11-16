using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change mortality status of agents.
	/// </summary>
	[Serializable]
	[PhrasePreview("Make {Who} {State}")]
	[PhraseTemplate("Make {Who} {State}")]
	public class SetAgentMortalityAction : ActionBase
	{
		public ValueSource<List<Agent>> Who = new VariableValue<List<Agent>>();
		public ValueSource<MortalityState> State = new LiteralValue<MortalityState>(MortalityState.Invulnerable);

		public SetAgentMortalityAction() { }
	}
}