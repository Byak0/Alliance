using Alliance.Common.Core.Utils;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Core.Utils.EntityUtils;
using static Alliance.Common.Utilities.Logger;
using Material = TaleWorlds.Engine.Material;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// Display custom text on an object. 
	/// Can optionally be edited by players (locally, or synchronized).
	/// Generates ONE mesh (two triangles per glyph) on the XZ plane.
	/// </summary>
	public class CS_TextPanel : SynchedMissionObject
	{
		/// <summary>
		/// Do not use outside of editor. String format is ISO-8859-1 and escaped.
		/// Use UpdateText/CleanedText instead.
		/// </summary>
		public string Text = "Hello world";
		public bool IsSynchronized = true;
		public bool IsEditable = false;
		public float FontSize = 0.5f; // meters (glyph height)
		public float LetterSpacing = 0f; // in "em" relative to glyph width; e.g., 0.1 = +10%
		public float LineSpacing = 1f; // multiplier for space between lines;
		public float PanelMaxWidth = 4f; // meters; <=0 = no wrap
		public TextHorizontalAlignment TextAlignment = TextHorizontalAlignment.Center;
		public AvailableFonts Font = AvailableFonts.Galahad;
		public string CustomColor = "#ffffffff"; // RGBA - default white

		public SimpleButton RENDER;

		/// <summary>
		/// Text in UTF-8, unescaped. Updated automatically when Text is changed.
		/// </summary>
		public string CleanedText { get; private set; }

		private static readonly Encoding Latin1Encoding = Encoding.GetEncoding("ISO-8859-1");

		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == nameof(RENDER) ||
				variableName == nameof(FontSize) ||
				variableName == nameof(PanelMaxWidth) ||
				variableName == nameof(TextAlignment) ||
				variableName == nameof(Font) ||
				variableName == nameof(LetterSpacing) ||
				variableName == nameof(LineSpacing))
			{
				Render();
			}
			else if (variableName == nameof(Text))
			{
				// Text value from editor is in ISO-8859-1 and escaped, need to clean it
				CleanedText = CleanText(Text);
				Render();
			}
		}

		public void UpdateText(string newText)
		{
			CleanedText = newText;
		}

		private string CleanText(string str)
		{
			byte[] bytes = Latin1Encoding.GetBytes(str);
			string textUTF8 = Encoding.UTF8.GetString(bytes);
			return System.Text.RegularExpressions.Regex.Unescape(textUTF8);
		}

		public string ResolveMaterialName()
		{
			string materialName = Font.ToString().ToLower();
			if (Material.GetFromResource(materialName) == null)
			{
				Log($"[CS_TextPanel] Material '{materialName}' not found, using default.", LogLevel.Warning);
				materialName = Material.GetDefaultMaterial().Name;
			}
			return materialName;
		}

		public void Render()
		{
			CheckCustomColorSyntax();
			EntityUtils.EnqueueTextPanel(this);
		}

		private void CheckCustomColorSyntax()
		{
			if (!CustomColor.StartsWith("#") || (CustomColor.Length != 7 && CustomColor.Length != 9))
			{
				Log($"[CS_TextPanel] CustomColor '{CustomColor}' has invalid syntax. Using default white.", LogLevel.Warning);
				CustomColor = "#ffffffff";
			}
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
		}

		protected override void OnInit()
		{
			base.OnInit();
			// Since Text is stored in ISO-8859-1 and escaped, clean it to get proper content
			CleanedText = CleanText(Text);
			EntityUtils.EnqueueTextPanel(this);
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			// Since Text is stored in ISO-8859-1 and escaped, clean it to get proper content
			CleanedText = CleanText(Text);
			EntityUtils.EnqueueTextPanel(this);
		}
	}
}
