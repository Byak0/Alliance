using System;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Marks a string field on an action as a reference to a trigger variable captured by a sibling
	/// condition (see <see cref="ProducesVariableAttribute"/>). The editor renders it as a dropdown of
	/// in-scope variables of <see cref="VariableType"/>. At runtime the action resolves the name through
	/// <see cref="ScenarioManager.CurrentTriggerContext"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class VariableRefAttribute : Attribute
	{
		public Type VariableType { get; }

		public VariableRefAttribute(Type variableType)
		{
			VariableType = variableType;
		}
	}
}
