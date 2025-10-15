using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.UsableItems.NetworkMessages.FromClient;
using Alliance.Server.Extensions.Zevent;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Core.ExtendedXML.Extension.ExtendedXMLExtension;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Extensions.UsableItem.Handlers
{
	public class UsableItemHandler : IHandlerRegister
	{
		public void Register(NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestUseItem>(HandleRequestUseItem);
		}

		/// <summary>
		/// Handle player request to use entity.
		/// </summary>
		public bool HandleRequestUseItem(NetworkCommunicator peer, RequestUseItem model)
		{
			if (peer.ControlledAgent == null)
			{
				return false;
			}

			Agent userAgent = peer.ControlledAgent;
			// TEST play horn on random horn carrier
			//Agent userAgent = Mission.Current.Agents.FindAll(agent => agent.Health > 0 && agent.Character == peer.ControlledAgent.Character).GetRandomElement();

			ItemObject item = userAgent.Equipment[model.EquipmentIndex].Item;
			ExtendedItem itemEx = item?.GetExtendedItem();
			if (itemEx == null)
			{
				return false;
			}

			Log($"Got a request from {peer.UserName} to use {itemEx.StringId}", LogLevel.Debug);
			if (itemEx.AnimationOnUse != null)
			{
				Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == itemEx.AnimationOnUse);
				AnimationSystem.Instance.PlayAnimation(userAgent, animation, true);
			}
			if (itemEx.SoundOnUse != null)
			{
				AudioPlayer.Instance.Play(itemEx.SoundOnUse, itemEx.SoundVolume, true, itemEx.SoundDistance, userAgent.Position, true);
				//NativeAudioPlayer.Instance.PlaySoundLocalized(itemEx.SoundOnUse, userAgent.Position, synchronize: true);
			}

			if (itemEx.Effects.Count != 0)
			{
				itemEx.Effects.ForEach(async effect =>
				{
					if (effect.Type == "ZEVENT")
					{
						await ZeventService.Instance.RefreshZeventGoldPileAsync();
					}
				});
			}

			return true;
		}
	}
}
