using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Localization;

namespace Alliance.Common.GameModes.Story.Models
{
	public class LocalizedString : ISerializationCallback
	{
		[XmlIgnore]
		public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

		[XmlElement("Entry")]
		public List<LocalizedEntry> Entries { get; set; }

		public LocalizedString(string defaultText)
		{
			Translations["English"] = defaultText;
		}

		public LocalizedString() { }

		public string LocalizedText => GetText(MBTextManager.ActiveTextLanguage);

		public string GetText(string languageCode)
		{
			if (Translations.ContainsKey(languageCode))
			{
				return Translations[languageCode];
			}
			return Translations.ContainsKey("English") ? Translations["English"] : string.Empty;
		}

		public void SetText(string languageCode, string text)
		{
			Translations[languageCode] = text;
		}

		public void OnBeforeSerialize()
		{
			// Prepare Entries for serialization
			Entries = Translations.Select(kv => new LocalizedEntry { Language = kv.Key, Text = kv.Value }).ToList();
		}

		public void OnAfterDeserialize()
		{
			// Rebuild the Translations dictionary after deserialization
			Translations = Entries?.ToDictionary(e => e.Language, e => e.Text) ?? new Dictionary<string, string>();
		}
	}

	public class LocalizedEntry
	{
		[XmlAttribute("language")]
		public string Language { get; set; }

		[XmlText]
		public string Text { get; set; }
	}
}
