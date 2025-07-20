using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core.Configuration.Models
{
	/// <summary>  
	/// Stores user-specific configurations for each player.  
	/// </summary>  
	public class UserConfigs
	{
		private readonly Dictionary<NetworkCommunicator, UserConfig> _userConfig = new();

		private static UserConfigs _instance;
		public static UserConfigs Instance => _instance ??= new UserConfigs();

		private UserConfigs() { }

		public UserConfig GetConfig(NetworkCommunicator user)
		{
			if (!_userConfig.TryGetValue(user, out UserConfig config))
			{
				config = new UserConfig();
				_userConfig[user] = config;
			}
			return config;
		}
	}
}
