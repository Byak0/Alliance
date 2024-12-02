using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// MultiplayerOptionsDefault message is using the MultiplayerOptions enum BoundMin/Max as compression info.
	/// Since we use much higher values than the BoundMax, we need this patch to force the use of correct Compression info.
	/// </summary>
	class Patch_MultiplayerOptionsDefault
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerOptionsDefault));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched) return false;
				_patched = true;
				Harmony.Patch(
					typeof(MultiplayerOptionsDefault).GetMethod(
						"OnRead",
						BindingFlags.Instance | BindingFlags.NonPublic
					),
					prefix: new HarmonyMethod(
						typeof(Patch_MultiplayerOptionsDefault).GetMethod(
							nameof(Prefix_OnRead),
							BindingFlags.Static | BindingFlags.Public
						)
					)
				);
				Harmony.Patch(
					typeof(MultiplayerOptionsDefault).GetMethod(
						"OnWrite",
						BindingFlags.Instance | BindingFlags.NonPublic
					),
					prefix: new HarmonyMethod(
						typeof(Patch_MultiplayerOptionsDefault).GetMethod(
							nameof(Prefix_OnWrite),
							BindingFlags.Static | BindingFlags.Public
						)
					)
				);
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MultiplayerOptionsDefault)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Use the correct CompressionInfo to receive options from server.
		/// </summary>
		public static bool Prefix_OnRead(ref bool __result, ref List<MultiplayerOptions.OptionType> ____optionList)
		{
			bool flag = true;

			foreach (MultiplayerOptions.OptionType optionType in ____optionList)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();

				// Use the correct Compression Info for the options we edited
				switch (optionType)
				{
					case MultiplayerOptions.OptionType.MaxNumberOfPlayers:
						optionType.SetValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MaxNumberOfPlayersCompressionInfo, ref flag));
						break;
					case MultiplayerOptions.OptionType.RoundTimeLimit:
						optionType.SetValue(GameNetworkMessage.ReadIntFromPacket(CompressionMission.RoundTimeCompressionInfo, ref flag));
						break;
					case MultiplayerOptions.OptionType.MapTimeLimit:
						optionType.SetValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MapTimeLimitCompressionInfo, ref flag));
						break;
					case MultiplayerOptions.OptionType.NumberOfBotsPerFormation:
						optionType.SetValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.NumberOfBotsPerFormationCompressionInfo, ref flag));
						break;
					// Native
					default:
						switch (optionProperty.OptionValueType)
						{
							case MultiplayerOptions.OptionValueType.Bool:
								optionType.SetValue(GameNetworkMessage.ReadBoolFromPacket(ref flag));
								break;
							case MultiplayerOptions.OptionValueType.Integer:
							case MultiplayerOptions.OptionValueType.Enum:
								optionType.SetValue(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true), ref flag));
								break;
							case MultiplayerOptions.OptionValueType.String:
								optionType.SetValue(GameNetworkMessage.ReadStringFromPacket(ref flag));
								break;
						}
						break;
				}
			}

			__result = flag;

			return false;
		}

		/// <summary>
		/// Use the correct CompressionInfo to send options to client.
		/// </summary>
		public static bool Prefix_OnWrite(ref List<MultiplayerOptions.OptionType> ____optionList)
		{
			foreach (MultiplayerOptions.OptionType optionType in ____optionList)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();

				// Use the correct Compression Info for the options we edited
				switch (optionType)
				{
					case MultiplayerOptions.OptionType.MaxNumberOfPlayers:
						GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), CompressionBasic.MaxNumberOfPlayersCompressionInfo);
						break;
					case MultiplayerOptions.OptionType.RoundTimeLimit:
						GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), CompressionMission.RoundTimeCompressionInfo);
						break;
					case MultiplayerOptions.OptionType.MapTimeLimit:
						GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), CompressionBasic.MapTimeLimitCompressionInfo);
						break;
					case MultiplayerOptions.OptionType.NumberOfBotsPerFormation:
						GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), CompressionBasic.NumberOfBotsPerFormationCompressionInfo);
						break;
					// Native
					default:
						switch (optionProperty.OptionValueType)
						{
							case MultiplayerOptions.OptionValueType.Bool:
								GameNetworkMessage.WriteBoolToPacket(optionType.GetBoolValue());
								break;
							case MultiplayerOptions.OptionValueType.Integer:
							case MultiplayerOptions.OptionValueType.Enum:
								GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true));
								break;
							case MultiplayerOptions.OptionValueType.String:
								GameNetworkMessage.WriteStringToPacket(optionType.GetStrValue());
								break;
						}
						break;
				}
			}

			return false;
		}
	}
}
