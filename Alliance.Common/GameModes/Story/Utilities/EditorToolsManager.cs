using Alliance.Common.GameModes.Story.Interfaces;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Manager for the editor tools. Implemented in Editor project.
	/// </summary>
	public static class EditorToolsManager
	{
		public static IEditorTools EditorTools;

		public static object OpenEditor(object obj)
		{
			return EditorTools?.OpenEditor(obj);
		}
	}
}
