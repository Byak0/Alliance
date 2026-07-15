using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Marks a class as the runtime override of a scenario action.
	/// <para>
	/// Apply this to a Client_/Server_ subclass in the project where both the base action type and the
	/// override type are visible. At startup, <see cref="ActionOverrideRegistry"/> scans every loaded assembly
	/// for this attribute and builds a map from the Common base action type to the override available in the
	/// current process. 
	/// </para>
	/// Actions without a registered override run their Common implementation as-is.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class OverrideActionAttribute : Attribute
	{
		public Type BaseType { get; }

		public OverrideActionAttribute(Type baseType)
		{
			BaseType = baseType;
		}
	}
}
