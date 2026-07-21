using System;

namespace Alliance.Common.GameModes.Story.Attributes
{
	/// <summary>
	/// Defines a read-only text preview rendered from an object's fields, used wherever a compact
	/// summary of an object is needed (e.g. list-item buttons). Uses the same slot syntax as
	/// <see cref="PhraseTemplateAttribute"/> but produces plain text instead of editable widgets:
	/// <c>{Field}</c> → the field's value, <c>{Field|Label0|Label1}</c> → the selected label,
	/// <c>{?Condition: content}</c> → conditional text.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class PhrasePreviewAttribute : Attribute
	{
		public string Template { get; }

		public PhrasePreviewAttribute(string template)
		{
			Template = template ?? string.Empty;
		}
	}
}
