using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Teleport a set of agents to a destination zone. The agents come from a generic <see cref="AgentSource"/>
	/// </summary>
	[Serializable]
	[PhraseTemplate("Teleport {Who} to {Destination}")]
	public class TeleportAgentAction : ActionBase
	{
		[ConfigProperty(label: "Who", tooltip: "Which agent(s) to teleport, based on various sources.")]
		public AgentSource Who;

		[ConfigProperty(label: "Destination", tooltip: "Zone the agents will be teleported to.")]
		public SerializableZone Destination;

		public TeleportAgentAction() { }

		public override void Execute()
		{
			if (!GameNetwork.IsServer) return;
			if (Who == null || Destination == null) return;

			TriggerContext context = ScenarioManager.Instance.CurrentTriggerContext;
			foreach (Agent agent in Who.Resolve(context))
			{
				var position = CoreUtils.GetRandomPositionWithinRadius(Destination.GlobalPosition, Destination.Radius);
				agent.TeleportToPosition(position);
			}
		}
	}
}
