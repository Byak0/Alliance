using Alliance.Common.GameModes.Story.Models;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public static class ScenarioEditorHelper
	{
		/// <summary>
		/// Returns a "readable" name for any object, looking for a "Name" field or property.
		/// Will return the type name otherwise.
		/// </summary>
		public static string GetItemDisplayName(object item, string language = "English")
		{
			if (item == null)
			{
				return "null";
			}
			var nameProperty = item.GetType().GetProperty("Name");
			if (nameProperty != null && nameProperty.PropertyType == typeof(string))
			{
				return (string)nameProperty.GetValue(item) ?? item.GetType().Name;
			}
			if (nameProperty != null && nameProperty.PropertyType == typeof(LocalizedString))
			{
				return ((LocalizedString)nameProperty.GetValue(item))?.GetText(language) ?? item.GetType().Name;
			}
			var nameField = item.GetType().GetField("Name");
			if (nameField != null && nameField.FieldType == typeof(string))
			{
				return (string)nameField.GetValue(item) ?? item.GetType().Name;
			}
			if (nameField != null && nameField.FieldType == typeof(LocalizedString))
			{
				return ((LocalizedString)nameField.GetValue(item))?.GetText(language) ?? item.GetType().Name;
			}
			return item.GetType().Name;
		}
	}
}
