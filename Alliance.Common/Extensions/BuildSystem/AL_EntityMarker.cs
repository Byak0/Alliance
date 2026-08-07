using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace Alliance.Common.Extensions.BuildSystem
{
	/// <summary>
	/// Marker script carrying a stable GUID (<see cref="RefId"/>) so a scene entity can be referenced
	/// regardless of rename/duplication. Attached automatically by the editor's "Select entity on map"
	/// flow, or placeable manually by the level designer. Resolved at runtime through
	/// <see cref="EntityMarkerIndex"/>.
	/// </summary>
	public class AL_EntityMarker : ScriptComponentBehavior
	{
		[EditableScriptComponentVariable(true)]
		public string RefId = "";

		[EditableScriptComponentVariable(true)]
		public string DisplayName = "";
	}
}
