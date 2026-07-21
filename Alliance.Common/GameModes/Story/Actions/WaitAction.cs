using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhraseTemplate("Wait {Duration} second(s)")]
	public class WaitAction : ActionBase
	{
		public ValueSource<float> Duration = new LiteralValue<float>(1f);

		public WaitAction() { }

		public override ActionTask Execute()
		{
			float duration = Duration.Resolve(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals);
			return new WaitTask(duration);
		}
	}
}
