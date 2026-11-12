using System;

namespace Alliance.Common.GameModes.Story.Attributes
{
	/// <summary>
	/// Marks a string field as the name of a Cinematic defined on the scenario. The editor renders it
	/// as a dropdown of the available cinematics (like VariableRefAttribute does for variables).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class CinematicRefAttribute : Attribute
	{
	}
}
