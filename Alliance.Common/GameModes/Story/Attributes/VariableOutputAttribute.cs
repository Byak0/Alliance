using System;

namespace Alliance.Common.GameModes.Story.Attributes
{
	/// <summary>
	/// Marks a string field on a Condition as the NAME of a trigger variable that the condition captures
	/// (of <see cref="VariableType"/>). The field's current value is the variable name; the user can rename
	/// it per-instance, so multiple capturing conditions produce distinct variables. The editor lists these
	/// as available targets for [VariableRef] fields, and the condition writes the value at runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class VariableOutputAttribute : Attribute
	{
		public Type VariableType { get; }

		public VariableOutputAttribute(Type variableType)
		{
			VariableType = variableType;
		}
	}
}
