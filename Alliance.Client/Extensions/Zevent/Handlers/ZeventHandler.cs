using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Zevent.Handlers
{
	internal class ZeventHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<ZEventUpdatePileNetworkServerMessage>(OnUpdateGoldPileRequest);
		}

		public void OnUpdateGoldPileRequest(ZEventUpdatePileNetworkServerMessage message)
		{
			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null) return;
			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();
			float pileTargetAmount = message.GoldPileTarget;
			goldPileScript.SetVolumeTarget(pileTargetAmount);
		}
	}
}
