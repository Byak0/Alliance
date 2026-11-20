using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Configuration.NetworkMessages.FromServer
{
	/// <summary>
	/// NetworkMessage to synchronize whole config between server and clients.    
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncConfigAll : GameNetworkMessage
	{
		public Config Config { get; set; }

		public SyncConfigAll() { }

		public SyncConfigAll(Config config)
		{
			Config = config;
		}

		protected override void OnWrite()
		{
			foreach (var field in ConfigManager.Instance.ConfigFields)
			{
				FieldInfo fieldInfo = field.Value;
				MultiplayerOptionsSerializer.WriteModOption(fieldInfo, fieldInfo.GetValue(Config));
			}
		}

		protected override bool OnRead()
		{
			Config = new Config();
			bool bufferReadValid = true;

			foreach (var field in ConfigManager.Instance.ConfigFields)
			{
				MultiplayerOptionsSerializer.ReadModOption(field.Value, Config, ref bufferReadValid);
			}

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Sync mod configuration (all fields)";
		}
	}
}