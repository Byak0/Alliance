using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
	/// <summary>
	/// Request to update game options on the server.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestUpdateOptions : GameNetworkMessage
	{
		public TWConfig NativeOptions { get; set; }
		public Config ModOptions { get; set; }

		public RequestUpdateOptions() { }

		public RequestUpdateOptions(TWConfig twConfig, Config config)
		{
			NativeOptions = twConfig;
			ModOptions = config;
		}

		protected override void OnWrite()
		{
			WriteNativeOptions();
			WriteModOptions();
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			ReadNativeOptions(ref bufferReadValid);
			ReadModOptions(ref bufferReadValid);
			return bufferReadValid;
		}

		private void WriteNativeOptions()
		{
			for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
			{
				MultiplayerOptionsSerializer.WriteOption(optionType, NativeOptions[optionType]);
			}
		}

		private void WriteModOptions()
		{
			foreach (var field in ConfigManager.Instance.ConfigFields)
			{
				FieldInfo fieldInfo = field.Value;
				MultiplayerOptionsSerializer.WriteModOption(fieldInfo, fieldInfo.GetValue(ModOptions));
			}
		}

		private void ReadNativeOptions(ref bool bufferReadValid)
		{
			NativeOptions = new TWConfig();
			for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
			{
				NativeOptions[optionType] = MultiplayerOptionsSerializer.ReadOption(optionType, ref bufferReadValid);
			}
		}

		private void ReadModOptions(ref bool bufferReadValid)
		{
			ModOptions = new Config();
			foreach (var field in ConfigManager.Instance.ConfigFields)
			{
				FieldInfo fieldInfo = field.Value;
				MultiplayerOptionsSerializer.ReadModOption(fieldInfo, ModOptions, ref bufferReadValid);
			}
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Request server to update options";
		}
	}
}