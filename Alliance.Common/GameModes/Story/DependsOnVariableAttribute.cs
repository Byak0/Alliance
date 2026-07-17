using System;

namespace Alliance.Common.GameModes.Story
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class DependsOnVariableAttribute : Attribute
	{
		public string SourceField { get; }

		public DependsOnVariableAttribute(string sourceField)
		{
			SourceField = sourceField;
		}
	}
}
