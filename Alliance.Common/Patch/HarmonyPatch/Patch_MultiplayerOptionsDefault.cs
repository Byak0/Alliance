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
				MultiplayerOptions.MultiplayerOption option = MultiplayerOptions.Instance.GetOptionFromOptionType(optionType);
				MultiplayerOptionsSerializer.ReadOptionAndUpdate(option, ref flag);
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
				MultiplayerOptionsSerializer.WriteOption(optionType, optionType.GetOptionValue());
			}

			return false;
		}
	}
}