using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Conditional action.
	/// </summary>
	[Serializable]
	[XmlType("ConditionalAction")]
	public class IfElseAction : ActionBase
	{
		[ConfigProperty(label: "Conditions", tooltip: "If there are multiple conditions, they must all be met.")]
		public List<Condition> Condition;
		[ConfigProperty(label: "Actions if true", tooltip: "Actions to execute if all conditions are met.")]
		public List<ActionBase> ActionIfTrue;
		[ConfigProperty(label: "Actions if false", tooltip: "Actions to execute if any condition is not met.")]
		public List<ActionBase> ActionIfFalse;

		public IfElseAction(Condition condition, ActionBase actionIfTrue, ActionBase actionIfFalse)
		{
			Condition = new List<Condition> { condition };
			ActionIfTrue = new List<ActionBase> { actionIfTrue };
			ActionIfFalse = new List<ActionBase> { actionIfFalse };
		}

		public IfElseAction()
		{
			Condition = new List<Condition>();
			ActionIfTrue = new List<ActionBase>();
			ActionIfFalse = new List<ActionBase>();
		}

		public override void Register(WeakGameEntity entity)
		{
			base.Register(entity);
			Condition.ForEach(c => c.Register(entity));
			ActionIfTrue.ForEach(a => a.Register(entity));
			ActionIfFalse.ForEach(a => a.Register(entity));
		}

		public override ActionTask Execute(VariableStore context)
		{
			bool result = true;
			Condition.ForEach(c => result &= c.Evaluate(context));

			List<ActionBase> branch = result ? ActionIfTrue : ActionIfFalse;
			if (branch == null || branch.Count == 0)
			{
				return ActionTask.CompletedTask;
			}
			if (branch.Count == 1)
			{
				return branch[0].Execute(context);
			}
			return new SequenceTask(branch);
		}
	}
}