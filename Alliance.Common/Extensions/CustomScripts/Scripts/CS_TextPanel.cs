using Alliance.Common.Core.Utils;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Core.Utils.EntityUtils;
using static Alliance.Common.Utilities.Logger;
using Material = TaleWorlds.Engine.Material;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// Display custom text on an object. 
	/// TODO: Can be synchronized.
	/// TODO: Can optionally be edited by players.
	/// Generates ONE mesh (two triangles per glyph) on the XZ plane.
	/// </summary>
	public class CS_TextPanel : SynchedMissionObject, IFocusable
	{
		public string Text = "Hello world";
		public bool IsSynchronized = true;
		public bool IsEditable = false;
		public float FontSize = 0.5f; // meters (glyph height)
		public float LetterSpacing = 0f; // in "em" relative to glyph width; e.g., 0.1 = +10%
		public float LineSpacing = 1f; // multiplier for space beween lines;
		public float PanelMaxWidth = 4f; // meters; <=0 = no wrap
		public TextHorizontalAlignment TextAlignment = TextHorizontalAlignment.Center;
		public AvailableFonts Font = AvailableFonts.Galahad;

		public SimpleButton RENDER;

		public FocusableObjectType FocusableObjectType => FocusableObjectType.None;

		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == nameof(RENDER) ||
				variableName == nameof(Text) ||
				variableName == nameof(FontSize) ||
				variableName == nameof(PanelMaxWidth) ||
				variableName == nameof(TextAlignment) ||
				variableName == nameof(Font) ||
				variableName == nameof(LetterSpacing) ||
				variableName == nameof(LineSpacing))
			{
				Render();
			}
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
			EntityUtils.EnqueueTextPanel(this);
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
		}

		protected override void OnInit()
		{
			base.OnInit();
			EntityUtils.EnqueueTextPanel(this);
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			EntityUtils.EnqueueTextPanel(this);
		}

		public void OnFocusGain(Agent userAgent)
		{
		}

		public void OnFocusLose(Agent userAgent)
		{
		}

		public TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
		{
			return new TextObject();
		}

		public string GetDescriptionText(GameEntity gameEntity = null)
		{
			if (int.TryParse(gameEntity.Name.Split(new char[] { '_' }).Last(), out int num))
			{
				string text = gameEntity.Name;
				text = text.Remove(text.Count() - num.ToString().Count());
				text += "x";
				TextObject textObject;
				if (GameTexts.TryGetText("str_destructible_component", out textObject, text))
				{
					return textObject.ToString();
				}
				return "";
			}
			else
			{
				TextObject textObject2;
				if (GameTexts.TryGetText("str_destructible_component", out textObject2, gameEntity.Name))
				{
					return textObject2.ToString();
				}
				return "";
			}
		}
	}
}
