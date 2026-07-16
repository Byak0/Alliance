using System.Collections.ObjectModel;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// One piece of a rendered phrase: either literal text or an inline field editor.
	/// </summary>
	public abstract class PhraseSegment { }

	/// <summary>
	/// A literal chunk of text between field placeholders.
	/// </summary>
	public class PhraseTextSegment : PhraseSegment
	{
		public string Text { get; set; }
	}

	/// <summary>
	/// An inline editor widget for a single field.
	/// </summary>
	public class PhraseFieldSegment : PhraseSegment
	{
		public FieldViewModel Field { get; set; }
	}

	/// <summary>
	/// A single line of a phrase (one template string), holding its ordered text/field segments.
	/// A type may declare several lines via [PhraseTemplate(...)].
	/// </summary>
	public class PhraseLineViewModel
	{
		public ObservableCollection<PhraseSegment> Segments { get; } = new ObservableCollection<PhraseSegment>();
	}
}
