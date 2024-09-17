using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Editor.GameModes.Story.Views;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	public class EditorTools : IEditorTools
	{
		ObjectEditorWindow _objectEditorWindow;

		public object OpenEditor(object obj)
		{
			// Create and show the editor window
			if (_objectEditorWindow == null || !_objectEditorWindow.IsLoaded)
			{
				_objectEditorWindow = new ObjectEditorWindow(obj);
				_objectEditorWindow.ShowDialog();

				// Retrieve the modified object from the view model after the window is closed
				var modifiedObject = (_objectEditorWindow.DataContext as ObjectEditorViewModel)?.Object;

				// Dispose of the window instance
				_objectEditorWindow = null;

				// Return the modified object
				return modifiedObject;
			}
			else
			{
				_objectEditorWindow.Focus();
				return obj;
			}
		}
	}
}
