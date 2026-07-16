using Alliance.Common.GameModes.Story.Models;
using System.Reflection;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public static class ScenarioEditorHelper
	{
		/// <summary>
		/// Returns a "readable" name for any object. Prefers an explicit [PhrasePreview] template,
		/// then a "Name" field or property, and finally the type name.
		/// </summary>
		public static string GetItemDisplayName(object item, string language = "English")
		{
			if (item == null)
			{
				return "null";
			}

			// Prefer an explicit read-only preview template.
			PhrasePreviewAttribute previewAttr = item.GetType().GetCustomAttribute<PhrasePreviewAttribute>();
			if (previewAttr != null && !string.IsNullOrWhiteSpace(previewAttr.Template))
			{
				return PhraseTextRenderer.RenderText(previewAttr.Template, item);
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
