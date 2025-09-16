using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Zevent.Behaviors;
using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.Zevent.Handlers
{
	public class ZeventHandler : IHandlerRegister
	{
		public ZeventHandler() { }

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<ZEventUpdatePileNetworkServerMessage>(OnUpdateGoldPileRequest);
			reg.Register<ZEventInitTent>(OnInitTentRequest);
		}

		public void OnInitTentRequest(ZEventInitTent message)
		{
			Log($"Server requested me to init tent id {message.TentId}, tier {message.Tier}, variant {message.Variant}, total donations {message.TotalDonations}, name {message.Name} and message {message.Message}", LogLevel.Debug);
			ZeventCommonBehavior zeventBehavior = Mission.Current.GetMissionBehavior<ZeventCommonBehavior>();
			if (zeventBehavior == null) return;
			zeventBehavior.SpawnTent(message.TentId, message.Tier, message.Variant, message.TotalDonations, message.Name, message.Message);
		}

		public void OnUpdateGoldPileRequest(ZEventUpdatePileNetworkServerMessage message)
		{
			Log($"Server requested me to update gold pile to target {message.GoldPileTarget} and base value {message.BaseAmount}", LogLevel.Debug);

			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null) return;
			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();

			if (message.BaseAmount != -1)
			{
				float pileBase = message.BaseAmount / 1000f;
				goldPileScript.SetVolume(pileBase);
			}

			// We need to divide by 1000 because CS_DynamicPile max value is 20_000 instead of 20_000_000
			float pileTargetAmount = message.GoldPileTarget / 1000f;
			goldPileScript.SetVolumeTarget(pileTargetAmount);
		}
	}
}
