using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Library.Debug;

namespace Alliance.Common.Utilities
{
	public static class Logger
	{
		/// <summary>
		/// Generic logging method handling both server & client.
		/// </summary>
		public static void Log(string message, LogLevel level = LogLevel.Information)
		{
#if DEBUG
			// In Debug build, log everything
#else
            // In Release build, skip Debug logs
            if (level == LogLevel.Debug)
            {
                return;
            }
#endif

			if (GameNetwork.IsClient)
			{
				// Print to in-game chat
				InformationManager.DisplayMessage(new InformationMessage(message, GetColorForLogLevel(level)));
			}
			else
			{
				// Print to server console
				Print(message, 0, GetConsoleColorForLogLevel(level));
			}
		}

		private static Color GetColorForLogLevel(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Information:
					return Colors.Cyan;
				case LogLevel.Debug:
					return Colors.Cyan;
				case LogLevel.Warning:
					return Colors.Yellow;
				case LogLevel.Error:
					return Colors.Red;
				default:
					throw new ArgumentException("Invalid log level specified.");
			}
		}

		private static DebugColor GetConsoleColorForLogLevel(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Information:
					return DebugColor.Cyan;
				case LogLevel.Debug:
					return DebugColor.DarkCyan;
				case LogLevel.Warning:
					return DebugColor.Yellow;
				case LogLevel.Error:
					return DebugColor.Red;
				default:
					throw new ArgumentException("Invalid log level specified.");
			}
		}

		public enum LogLevel
		{
			Information,
			Debug,
			Warning,
			Error
		}

		public static void SendMessageToAll(string message)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ServerMessage(message));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendMessageToPeer(string message, NetworkCommunicator peer)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new ServerMessage(message));
			GameNetwork.EndModuleEventAsServer();
		}

		public static void SendInformationToAll(string message)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SendNotification(message, 1));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendNotificationToAll(string message)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SendNotification(message, 0));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendAdminNotificationToAll(string message, bool broadcast = true)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ServerAdminMessage(message, broadcast));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
