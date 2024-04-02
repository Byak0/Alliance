using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using Alliance.Common.Extensions.SoundPlayer;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Core.ExtendedXML.Extension.ExtendedXMLExtension;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Extensions.UsableItem.Handlers
{
    public class UsableItemHandler : IHandlerRegister
    {
        UsableEntityBehavior _gameMode;

        UsableEntityBehavior GameMode
        {
            get
            {
                return _gameMode ??= Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
            }
        }

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
            else
            {
                Log($"Got a request from {peer.UserName} to use {itemEx.StringId}", LogLevel.Debug);
                if (itemEx.AnimationOnUse != null)
                {
                    Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == itemEx.AnimationOnUse);
                    AnimationSystem.Instance.PlayAnimation(userAgent, animation, true);
                }
                if (itemEx.SoundOnUse != null)
                {
                    SoundSystem.Instance.PlaySoundLocalized(itemEx.SoundOnUse, userAgent.Position, synchronize: true);
                }
                return true;
            }
        }
    }
}
