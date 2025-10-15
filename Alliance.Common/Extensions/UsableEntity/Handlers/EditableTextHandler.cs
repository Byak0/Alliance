using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.UsableEntity.Interfaces;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.UsableEntity.Behaviors.UsableEntityBehavior;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.UsableEntity.Handlers
{
	public class EditableTextHandler : IInteractionHandler
	{
		public bool CanHandle(GameEntity entity)
		{
			return entity != null && (entity.GetFirstScriptOfType<CS_TextPanel>()?.IsEditable ?? false);
		}

		public bool CanInteract(Agent agent, GameEntity entity)
		{
			return true;
		}

		public TextObject GetInteractionText(Agent agent, GameEntity entity)
		{
			TextObject to = new TextObject("Press {KEY} to change text");
			to.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return to;
		}

		public void Interact(Agent agent, InteractionTarget target)
		{
			CS_TextPanel textPanel = target.Entity.GetFirstScriptOfType<CS_TextPanel>();

			// Prompt a text inquiry for user to enter his pitch
			InformationManager.ShowTextInquiry(
				new TextInquiryData("Text Panel",
				"Change text:", true, true,
				new TextObject("{=WiNRdfsm}Confirm", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(),
				newText => OnTextConfirmed(newText, target, textPanel), null, false, null, "", textPanel.CleanedText),
				false);
		}

		private void OnTextConfirmed(string newText, InteractionTarget target, CS_TextPanel textPanel)
		{
			if (newText.Length > 128)
			{
				Log("Text cannot exceed 128 characters.", LogLevel.Warning);
				return;
			}

			if (textPanel.IsSynchronized)
			{
				Log($"Sending request to edit text of entity {target.Entity.Name}({target.ID}) to: {newText}", LogLevel.Debug);
				UsableEntityMsg.RequestTextPanelEdit(newText, target);
			}
			else
			{
				textPanel.UpdateText(newText);
				textPanel.Render();
			}
		}
	}
}
