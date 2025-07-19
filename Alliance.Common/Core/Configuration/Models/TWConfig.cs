using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.Core.Configuration.Models
{
	/// <summary>
	/// Serializable wrapper class for MultiplayerOptions, since native options are not serializable.
	/// Usage: TWConfig config = new TWConfig();
	/// config[OptionType.CultureTeam1] = "vlandia";
	/// </summary>
	[Serializable]
	public class TWConfig
	{
		//[ConfigProperty(isEditable: false)]
		//public string ServerName;
		[ConfigProperty(label: "Welcome Message", tooltip: "Welcome messages which is shown to all players when they enter the server.")]
		public string WelcomeMessage = "Hello";
		//[ConfigProperty(isEditable: false)]
		//public string GamePassword;
		//[ConfigProperty(isEditable: false)]
		//public string AdminPassword;
		//[ConfigProperty(isEditable: false)]
		//public int GameDefinitionId;
		//[ConfigProperty(isEditable: false)]
		//public bool AllowPollsToKickPlayers;
		//[ConfigProperty(isEditable: false)]
		//public bool AllowPollsToBanPlayers;
		//[ConfigProperty(isEditable: false)]
		//public bool AllowPollsToChangeMaps;
		[ConfigProperty(label: "Allow individual banner.", tooltip: "Allow players to use their custom banner.")]
		public bool AllowIndividualBanners;
		[ConfigProperty(label: "Realistic blocking", tooltip: "Use animation progress dependent blocking.")]
		public bool UseRealisticBlocking;
		//[ConfigProperty(isEditable: false)]
		//public string PremadeMatchGameMode;
		[ConfigProperty(label: "Game mode", dataType: AllianceData.DataTypes.GameMode)]
		public string GameType;
		//[ConfigProperty(isEditable: false)]
		//public Enum PremadeGameType;
		[ConfigProperty(label: "Map", dataType: AllianceData.DataTypes.Map)]
		public string Map;
		[ConfigProperty(label: "Culture for attacker", dataType: AllianceData.DataTypes.Culture)]
		public string CultureTeam1;
		[ConfigProperty(label: "Culture for defender", dataType: AllianceData.DataTypes.Culture)]
		public string CultureTeam2;
		//[ConfigProperty(isEditable: false)]
		//public int MaxNumberOfPlayers;
		//[ConfigProperty(isEditable: false)]
		//public int MinNumberOfPlayersForMatchStart;
		[ConfigProperty(label: "Number of bots for attacker")]
		public int NumberOfBotsTeam1;
		[ConfigProperty(label: "Number of bots for defender")]
		public int NumberOfBotsTeam2;
		[ConfigProperty(label: "Amount of bots per formation")]
		public int NumberOfBotsPerFormation;
		[ConfigProperty(label: "Friendly fire to self (melee)", tooltip: "A percentage of how much melee damage inflicted upon a friend is dealt back to the inflictor.")]
		public int FriendlyFireDamageMeleeSelfPercent;
		[ConfigProperty(label: "Friendly fire (melee)", tooltip: "A percentage of how much melee damage inflicted upon a friend is actually dealt.")]
		public int FriendlyFireDamageMeleeFriendPercent;
		[ConfigProperty(label: "Friendly fire to self (ranged)", tooltip: "A percentage of how much ranged damage inflicted upon a friend is dealt back to the inflictor.")]
		public int FriendlyFireDamageRangedSelfPercent;
		[ConfigProperty(label: "Friendly fire (ranged)", tooltip: "A percentage of how much ranged damage inflicted upon a friend is actually dealt.")]
		public int FriendlyFireDamageRangedFriendPercent;
		[ConfigProperty(label: "Who can spectators look at, and how.")]
		public SpectatorCameraTypes SpectatorCamera;
		[ConfigProperty(label: "Warmup duration", tooltip: "Maximum duration for the warmup. In minutes.")]
		public int WarmupTimeLimit;
		[ConfigProperty(label: "Map max duration", tooltip: "Maximum duration for the map. In minutes.")]
		public int MapTimeLimit;
		[ConfigProperty(label: "Round max duration", tooltip: "Maximum duration for each round. In seconds.")]
		public int RoundTimeLimit;
		[ConfigProperty(label: "Round preparation time", tooltip: "Time available to select class/equipment. In seconds.")]
		public int RoundPreparationTimeLimit;
		[ConfigProperty(label: "Round total", tooltip: "Maximum amount of rounds before the game ends.")]
		public int RoundTotal;
		[ConfigProperty(label: "Respawn period (attacker)", tooltip: "Wait time after death, before respawning again. In seconds.")]
		public int RespawnPeriodTeam1;
		[ConfigProperty(label: "Respawn period (defender)", tooltip: "Wait time after death, before respawning again. In seconds.")]
		public int RespawnPeriodTeam2;
		[ConfigProperty(label: "Unlimited gold")]
		public bool UnlimitedGold;
		[ConfigProperty(label: "Gold gain multiplier from agent deaths.")]
		public int GoldGainChangePercentageTeam1;
		[ConfigProperty(label: "Gold gain multiplier from agent deaths.")]
		public int GoldGainChangePercentageTeam2;
		[ConfigProperty(label: "Min score to win match.")]
		public int MinScoreToWinMatch;
		[ConfigProperty(label: "Min score to win duel.")]
		public int MinScoreToWinDuel;
		[ConfigProperty(label: "Minimum needed difference in poll results before it is accepted.")]
		public int PollAcceptThreshold;
		[ConfigProperty(label: "Auto team balancing", tooltip: "Maximum player imbalance between team 1 and team 2. Selecting 0 will disable auto team balancing.")]
		public int AutoTeamBalanceThreshold;
		[ConfigProperty(label: "Enables mission recording.")]
		public bool EnableMissionRecording;
		[ConfigProperty(label: "Sets if the game mode uses single spawning.")]
		public bool SingleSpawn;
		[ConfigProperty(label: "Disables the inactivity kick timer.")]
		public bool DisableInactivityKick;

		public TWConfig() { }

		/// <summary>
		/// Indexer for accessing options. If the option is not set, it will return the current value used by server.
		/// Uses Reflection so performance is not optimal.
		/// </summary>
		/// <param name="option">Option type from MultiplayerOptions class</param>
		/// <returns>Value of the option.</returns>
		public object this[OptionType option]
		{
			get => GetBoundedOption(option, typeof(TWConfig).GetField(option.ToString())?.GetValue(this) ?? GetDefaultValue(option));
			set => SetBoundedOption(option, value);
		}

		/// <summary>
		/// Ensure the value is within the bounds of the option.
		/// </summary>
		private void SetBoundedOption(OptionType option, object value)
		{
			if (option.GetOptionProperty().HasBounds)
			{
				if (option.GetMaximumValue() is int max && option.GetMinimumValue() is int min)
				{
					if (value is int val)
					{
						// Override max value for some options
						switch (option)
						{
							case OptionType.MaxNumberOfPlayers:
								max = CompressionBasic.MaxNumberOfPlayersCompressionInfo.GetMaximumValue();
								break;
							case OptionType.RoundTimeLimit:
								max = CompressionMission.RoundTimeCompressionInfo.GetMaximumValue();
								break;
							case OptionType.MapTimeLimit:
								max = CompressionBasic.MapTimeLimitCompressionInfo.GetMaximumValue();
								break;
							case OptionType.NumberOfBotsPerFormation:
								max = CompressionBasic.NumberOfBotsPerFormationCompressionInfo.GetMaximumValue();
								break;
							default:
								break;
						}
						if (val > max)
						{
							Log($"Value for option {option} is too high, setting to maximum value : {max}", LogLevel.Debug);
							value = max;
						}
						if (val < min)
						{
							Log($"Value for option {option} is too low, setting to minimum value : {min}", LogLevel.Debug);
							value = min;
						}
					}
				}
			}
			typeof(TWConfig).GetField(option.ToString())?.SetValue(this, value);
		}

		/// <summary>
		/// Ensure the value is within the bounds of the option.
		/// </summary>
		private object GetBoundedOption(OptionType option, object value)
		{
			if (option.GetOptionProperty().HasBounds)
			{
				if (option.GetMaximumValue() is int max && option.GetMinimumValue() is int min)
				{
					if (value is int val)
					{
						if (val > max)
						{
							Log($"Value for option {option} is too high, setting to maximum value : {max}", LogLevel.Debug);
							return max;
						}
						if (val < min)
						{
							Log($"Value for option {option} is too low, setting to minimum value : {min}", LogLevel.Debug);
							return min;
						}
					}
				}
			}
			return value;
		}

		private object GetDefaultValue(OptionType key)
		{
			Log($"No value set for option {key}, using server value : {key.GetValueText()}", LogLevel.Debug);
			return key.GetOptionProperty().OptionValueType switch
			{
				OptionValueType.Bool => key.GetBoolValue(),
				OptionValueType.Integer => key.GetIntValue(),
				OptionValueType.Enum => key.GetIntValue(),
				OptionValueType.String => key.GetStrValue(),
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}
}