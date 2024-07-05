using HarmonyLib;
using System;
using System.Reflection;
using static Alliance.Common.Utilities.Logger;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Alliance.Server.Patch.HarmonyPatch
{
	class Patch_Module
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Module));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				var originalMethod = typeof(Module).GetMethod(nameof(Module.ShutDownWithDelay),
					BindingFlags.Instance | BindingFlags.Public);

				if (originalMethod == null)
				{
					// Handle the case where the method is not found
					return false;
				}

				Harmony.Patch(originalMethod, postfix: new HarmonyMethod(typeof(Patch_Module).GetMethod(
					nameof(Postfix_ShutDownWithDelay), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_Module)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Try to warn all players before the server shuts down when losing connection to TW main server.
		/// </summary>
		public static void Postfix_ShutDownWithDelay(string reason)
		{
			SendAdminNotificationToAll("Server is forced to shut down. Reason: " + reason);
		}
	}
}
