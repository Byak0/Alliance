﻿using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Lobby
{
	public class LobbyGameModeSettings : GameModeSettings
	{
		public LobbyGameModeSettings() : base("Lobby", "Lobby", "Chill in the lobby.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			TWOptions[OptionType.NumberOfBotsTeam2] = 0;
		}

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
		}

		public override List<SceneInfo> GetAvailableMaps()
		{
			return base.GetAvailableMaps();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>
			{
				OptionType.CultureTeam1,
				OptionType.NumberOfBotsTeam1
			};
		}

		public override List<string> GetAvailableModOptions()
		{
			// Return full list of options for admins
			if (GameNetwork.MyPeer.IsAdmin())
			{
				return base.GetAvailableModOptions();
			}
			// Otherwise return only following options
			return new List<string>
			{
				nameof(Config.AllowCustomBody),
				nameof(Config.RandomizeAppearance),
				nameof(Config.ShowFlagMarkers),
				nameof(Config.ShowScore),
				nameof(Config.ShowOfficers),
				nameof(Config.ShowWeaponTrail),
				nameof(Config.KillFeedEnabled),
				nameof(Config.OfficerHPMultip)
			};
		}
	}
}