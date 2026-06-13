using Alliance.Common.Core.Utils;
using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// MultiplayerOptionsInitial and MultiplayerOptionsImmediate messages are using the MultiplayerOptions enum BoundMin/Max as compression info.
	/// Since we use much higher values than the BoundMax, we need this patch to force the use of correct Compression info.
	/// </summary>
	class Patch_MultiplayerOptionsImmediate
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerOptionsImmediate));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(MultiplayerOptionsImmediate).GetMethod("OnRead",
						BindingFlags.Instance | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_MultiplayerOptionsImmediate).GetMethod(
						nameof(Prefix_OnRead), BindingFlags.Static | BindingFlags.Public)));
				Harmony.Patch(
					typeof(MultiplayerOptionsImmediate).GetMethod("OnWrite",
						BindingFlags.Instance | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_MultiplayerOptionsImmediate).GetMethod(
						nameof(Prefix_OnWrite), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MultiplayerOptionsImmediate)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Use the correct CompressionInfo to receive options from server.
		/// </summary>
		public static bool Prefix_OnRead(ref bool __result, ref List<MultiplayerOptions.MultiplayerOption> ____optionList)
		{
			bool flag = true;
			____optionList = new List<MultiplayerOptions.MultiplayerOption>();
			for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
				if (optionProperty.Replication == MultiplayerOptionsProperty.ReplicationOccurrence.Immediately)
				{
					MultiplayerOptions.MultiplayerOption multiplayerOption = MultiplayerOptions.MultiplayerOption.CreateMultiplayerOption(optionType);
					MultiplayerOptionsSerializer.ReadOptionAndUpdate(multiplayerOption, ref flag);
					____optionList.Add(multiplayerOption);
				}
			}
			__result = flag;

			return false;
		}

		/// <summary>
		/// Use the correct CompressionInfo to send options to clients.
		/// </summary>
		public static bool Prefix_OnWrite(ref List<MultiplayerOptions.MultiplayerOption> ____optionList)
		{
			foreach (MultiplayerOptions.MultiplayerOption multiplayerOption in ____optionList)
			{
				MultiplayerOptions.OptionType optionType = multiplayerOption.OptionType;
				MultiplayerOptionsSerializer.WriteOption(optionType, optionType.GetOptionValue());
			}

			return false;
		}
	}
}
