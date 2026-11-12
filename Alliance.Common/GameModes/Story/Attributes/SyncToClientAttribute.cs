using System;

namespace Alliance.Common.GameModes.Story.Attributes
{
	/// <summary>
	/// Marks a ValueSource field (or list of them) as sync-to-client: the server resolves the
	/// expression once and ships the bare value; the receiver rewrites the slot with a literal.
	/// Used by ActionBase.ExecuteOnClient and the cinematic dynamic-data walk. Unmarked slots are never shipped.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class SyncToClientAttribute : Attribute
	{
		public SyncToClientAttribute() { }
	}
}
