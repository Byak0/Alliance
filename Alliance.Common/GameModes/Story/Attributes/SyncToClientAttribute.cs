using System;

namespace Alliance.Common.GameModes.Story.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SyncToClientAttribute : Attribute
	{
		public SyncToClientAttribute() { }
	}
}
