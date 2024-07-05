using Alliance.Common.Core.Utils;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	public class CS_StandingPointWithItemRequirement : StandingPoint
	{
		public string RequiredItemId = "isengard_bomb";
		public bool UpdateParentState = true;
		public bool KillAgentOnUse = true;

		private ItemObject _requiredItem;

		public CS_StandingPointWithItemRequirement()
		{
			AutoSheathWeapons = false;
		}

		protected override void OnInit()
		{
			base.OnInit();
			_requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
		}

		public override void OnUse(Agent userAgent)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				if (UpdateParentState)
				{
					CS_StateObject parentEntity = GameEntity?.Parent.GetFirstScriptOfTypeInFamily<CS_StateObject>();
					parentEntity?.SetState(parentEntity.CurrentStateIndex + 1);

					if (userAgent?.WieldedWeapon.Item == _requiredItem)
					{
						EquipmentIndex index = userAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
						if (index != EquipmentIndex.None) userAgent.RemoveEquippedWeapon(index);
					}
					else if (userAgent?.WieldedWeapon.Item == _requiredItem)
					{
						EquipmentIndex index = userAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
						if (index != EquipmentIndex.None) userAgent.RemoveEquippedWeapon(index);
					}
				}
				if (KillAgentOnUse)
				{
					KillWithDelay(userAgent, 2000);

				}
			}

			base.OnUse(userAgent);
		}

		public async void KillWithDelay(Agent agent, int waitTime)
		{
			await Task.Delay(waitTime);
			CoreUtils.TakeDamage(agent, 2000, 1000);
		}

		public override bool IsDisabledForAgent(Agent agent)
		{
			if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem)
			{
				return true;
			}
			return false;
		}
	}
}
