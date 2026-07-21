using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>
	/// Returns the live agent nearest to a reference agent, optionally filtered by side. The reference
	/// agent is read from a variable (typical use: the trigger's captured agent).
	/// </summary>
	[Serializable]
	[PhrasePreview("nearest {Side} agent to {Source}")]
	[PhraseTemplate("nearest {Side} agent to {Source}")]
	public class NearestAgentToFunction : Function
	{
		public override Type ReturnType => typeof(Agent);

		[ConfigProperty(label: "Source agent", tooltip: "Agent to measure distance from.")]
		public ValueSource<Agent> Source = new VariableValue<Agent>();

		[ConfigProperty(label: "Side", tooltip: "Restrict candidates to this side.")]
		public SideType Side = SideType.All;

		public NearestAgentToFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			if (Mission.Current == null || Source == null) return null;
			Agent source = Source.Resolve(ctx, globals);
			if (source == null) return null;

			Agent best = null;
			float bestDist = float.MaxValue;
			foreach (Agent candidate in Mission.Current.Agents)
			{
				if (candidate == source || candidate?.Team == null) continue;
				if (!MatchesSide(candidate, source)) continue;
				float d = candidate.Position.DistanceSquared(source.Position);
				if (d < bestDist) { bestDist = d; best = candidate; }
			}
			return best;
		}

		private bool MatchesSide(Agent candidate, Agent source)
		{
			if (Side == SideType.All) return true;
			if (Side == SideType.Defender) return candidate.Team.Side == BattleSideEnum.Defender;
			if (Side == SideType.Attacker) return candidate.Team.Side == BattleSideEnum.Attacker;
			return true;
		}
	}
}
