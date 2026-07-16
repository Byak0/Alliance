using System;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Defines one or more human-readable phrases that the scenario editor renders inline for this type,
	/// instead of a flat list of fields. Each string becomes its own line in the editor, so complex types
	/// can spread their fields across several readable sentences.
	/// <para>
	/// Placeholders in the form <c>{FieldName}</c> are replaced live with a compact editor widget for the
	/// matching public field (matched by field name). Fields referenced in any phrase are shown inline;
	/// the remaining fields appear below under an "Advanced" group. If a placeholder cannot be matched
	/// to a field (or the field is currently hidden by a dependency), it is rendered as literal text.
	/// Types without this attribute keep the standard property-grid layout.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class PhraseTemplateAttribute : Attribute
	{
		public string[] Templates { get; }

		public PhraseTemplateAttribute(params string[] templates)
		{
			Templates = templates ?? Array.Empty<string>();
		}
	}
}
