using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomScripts.Handlers
{
	public class TextPanelHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncTextPanel>(HandleSyncTextPanel);
		}

		public void HandleSyncTextPanel(SyncTextPanel message)
		{
			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			CS_TextPanel script = missionObject?.GameEntity.GetFirstScriptOfType<CS_TextPanel>();
			if (script != null)
			{
				script.UpdateText(message.Text);
				script.Render();
			}
		}
	}
}
