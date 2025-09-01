using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Zevent.Handlers
{
	public class ZeventHandler : IHandlerRegister
	{
		public ZeventHandler() { }

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<ZEventUpdatePileNetworkServerMessage>(OnUpdateGoldPileRequest);
		}

		public void OnUpdateGoldPileRequest(ZEventUpdatePileNetworkServerMessage message)
		{
			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null) return;
			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();
			// We need to divide by 1000 because CS_DynamicPile max value is 20_000 instead of 20_000_000
			float pileTargetAmount = message.GoldPileTarget / 1000;
			goldPileScript.SetVolumeTarget(pileTargetAmount);
		}
	}
}
