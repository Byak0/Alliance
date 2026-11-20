using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhrasePreview("Wait {Duration} second(s)")]
	[PhraseTemplate("Wait {Duration} second(s)")]
	public class WaitAction : ActionBase
	{
		public ValueSource<float> Duration = new LiteralValue<float>(1f);

		public WaitAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			float duration = Duration.Resolve(context);
			return new WaitTask(duration);
		}
	}
}
