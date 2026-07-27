using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Teleport agent to a specified zone.
	/// </summary>
	[Serializable]
	[PhrasePreview("Teleport {Who} to {Destination}")]
	[PhraseTemplate("Teleport {Who} to {Destination}")]
	public class TeleportAgentAction : ActionBase
	{
		public ValueSource<List<Agent>> Who = new VariableValue<List<Agent>>();
		public ValueSource<Zone> Destination = new LiteralValue<Zone>(new Zone());

		public TeleportAgentAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			if (!GameNetwork.IsServer) return ActionTask.CompletedTask;
			if (Who == null || Destination == null) return ActionTask.CompletedTask;

			Zone dest = Destination.Resolve(context);
			if (dest == null) return ActionTask.CompletedTask;

			Vec3 center = dest.ResolveCenter(context);
			List<Agent> agents = Who.Resolve(context);
			if (agents == null) return ActionTask.CompletedTask;
			foreach (Agent agent in agents)
			{
				if (agent == null) continue;
				var position = CoreUtils.GetRandomPositionWithinRadius(center, dest.Radius);
				agent.TeleportToPosition(position);
			}
			return ActionTask.CompletedTask;
		}
	}
}
