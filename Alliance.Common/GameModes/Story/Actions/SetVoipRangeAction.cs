using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change the VOIP range for targets.
	/// </summary>
	[Serializable]
	public class SetVoipRangeAction : ActionBase
	{
		[ConfigProperty(label: "Targets", tooltip: "The agents to change the VOIP range for.")]
		public ValueSource<List<Agent>> Who = new VariableValue<List<Agent>>();
		[ConfigProperty(label: "VOIP range", tooltip: "The new VOIP range for the targets. In meters.")]
		public ValueSource<int> VOIP_Range = new LiteralValue<int>(AgentsInfoModel.DEFAULT_SPEAKING_RANGE);

		public SetVoipRangeAction() { }
	}
}