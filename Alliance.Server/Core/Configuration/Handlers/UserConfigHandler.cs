using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.NetworkMessages.FromClient;
using Alliance.Common.Extensions;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Configuration.Handlers
{
	public class UserConfigHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<UpdateUserConfig>(HandleUpdateUserConfig);
		}

		private bool HandleUpdateUserConfig(NetworkCommunicator peer, UpdateUserConfig message)
		{
			UserConfig userConfig = UserConfigs.Instance.GetConfig(peer);
			userConfig.PreferredLanguageIndex = message.PreferredLanguageIndex;
			Log($"{peer.UserName} preferred language is now {userConfig.PreferredLanguage}", LogLevel.Debug);
			return true;
		}
	}
}
