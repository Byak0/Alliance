using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient
{
	/// <summary>
	/// NetworkMessage to request a new GameMode poll to the server.
	/// Contains all options related to that GameMode.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class GameModePollRequestMessage : GameNetworkMessage
	{
		public TWConfig NativeOptions { get; set; }
		public Config ModOptions { get; set; }
		public bool SkipPoll { get; private set; }

		public GameModePollRequestMessage() { }

		public GameModePollRequestMessage(TWConfig twConfig, Config config, bool skipPoll)
		{
			NativeOptions = twConfig;
			ModOptions = config;
			SkipPoll = skipPoll;
		}

		protected override void OnWrite()
		{
			WriteNativeOptions();
			WriteModOptions();
			WriteBoolToPacket(SkipPoll);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			ReadNativeOptions(ref bufferReadValid);
			ReadModOptions(ref bufferReadValid);
			SkipPoll = ReadBoolFromPacket(ref bufferReadValid);
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
			return "Request a new game mode poll to the server";
		}
	}
}