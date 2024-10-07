using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
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
		//[ScenarioEditor(isEditable: false)]
		//public string ServerName;
		[ScenarioEditor(label: "Welcome Message", tooltip: "Welcome messages which is shown to all players when they enter the server.")]
		public string WelcomeMessage = "Hello";
		//[ScenarioEditor(isEditable: false)]
		//public string GamePassword;
		//[ScenarioEditor(isEditable: false)]
		//public string AdminPassword;
		//[ScenarioEditor(isEditable: false)]
		//public int GameDefinitionId;
		//[ScenarioEditor(isEditable: false)]
		//public bool AllowPollsToKickPlayers;
		//[ScenarioEditor(isEditable: false)]
		//public bool AllowPollsToBanPlayers;
		//[ScenarioEditor(isEditable: false)]
		//public bool AllowPollsToChangeMaps;
		[ScenarioEditor(label: "Allow individual banner.", tooltip: "Allow players to use their custom banner.")]
		public bool AllowIndividualBanners;
		[ScenarioEditor(label: "Realistic blocking", tooltip: "Use animation progress dependent blocking.")]
		public bool UseRealisticBlocking;
		//[ScenarioEditor(isEditable: false)]
		//public string PremadeMatchGameMode;
		[ScenarioEditor(label: "Game mode", dataType: ScenarioData.DataTypes.GameMode)]
		public string GameType;
		//[ScenarioEditor(isEditable: false)]
		//public Enum PremadeGameType;
		[ScenarioEditor(label: "Map", dataType: ScenarioData.DataTypes.Map)]
		public string Map;
		[ScenarioEditor(label: "Culture for attacker", dataType: ScenarioData.DataTypes.Culture)]
		public string CultureTeam1;
		[ScenarioEditor(label: "Culture for defender", dataType: ScenarioData.DataTypes.Culture)]
		public string CultureTeam2;
		//[ScenarioEditor(isEditable: false)]
		//public int MaxNumberOfPlayers;
		//[ScenarioEditor(isEditable: false)]
		//public int MinNumberOfPlayersForMatchStart;
		[ScenarioEditor(label: "Number of bots for attacker")]
		public int NumberOfBotsTeam1;
		[ScenarioEditor(label: "Number of bots for defender")]
		public int NumberOfBotsTeam2;
		[ScenarioEditor(label: "Amount of bots per formation")]
		public int NumberOfBotsPerFormation;
		[ScenarioEditor(label: "Friendly fire to self (melee)", tooltip: "A percentage of how much melee damage inflicted upon a friend is dealt back to the inflictor.")]
		public int FriendlyFireDamageMeleeSelfPercent;
		[ScenarioEditor(label: "Friendly fire (melee)", tooltip: "A percentage of how much melee damage inflicted upon a friend is actually dealt.")]
		public int FriendlyFireDamageMeleeFriendPercent;
		[ScenarioEditor(label: "Friendly fire to self (ranged)", tooltip: "A percentage of how much ranged damage inflicted upon a friend is dealt back to the inflictor.")]
		public int FriendlyFireDamageRangedSelfPercent;
		[ScenarioEditor(label: "Friendly fire (ranged)", tooltip: "A percentage of how much ranged damage inflicted upon a friend is actually dealt.")]
		public int FriendlyFireDamageRangedFriendPercent;
		[ScenarioEditor(label: "Who can spectators look at, and how.")]
		public SpectatorCameraTypes SpectatorCamera;
		[ScenarioEditor(label: "Warmup duration", tooltip: "Maximum duration for the warmup. In minutes.")]
		public int WarmupTimeLimit;
		[ScenarioEditor(label: "Map max duration", tooltip: "Maximum duration for the map. In minutes.")]
		public int MapTimeLimit;
		[ScenarioEditor(label: "Round max duration", tooltip: "Maximum duration for each round. In seconds.")]
		public int RoundTimeLimit;
		[ScenarioEditor(label: "Round preparation time", tooltip: "Time available to select class/equipment. In seconds.")]
		public int RoundPreparationTimeLimit;
		[ScenarioEditor(label: "Round total", tooltip: "Maximum amount of rounds before the game ends.")]
		public int RoundTotal;
		[ScenarioEditor(label: "Respawn period (attacker)", tooltip: "Wait time after death, before respawning again. In seconds.")]
		public int RespawnPeriodTeam1;
		[ScenarioEditor(label: "Respawn period (defender)", tooltip: "Wait time after death, before respawning again. In seconds.")]
		public int RespawnPeriodTeam2;
		[ScenarioEditor(label: "Unlimited gold")]
		public bool UnlimitedGold;
		[ScenarioEditor(label: "Gold gain multiplier from agent deaths.")]
		public int GoldGainChangePercentageTeam1;
		[ScenarioEditor(label: "Gold gain multiplier from agent deaths.")]
		public int GoldGainChangePercentageTeam2;
		[ScenarioEditor(label: "Min score to win match.")]
		public int MinScoreToWinMatch;
		[ScenarioEditor(label: "Min score to win duel.")]
		public int MinScoreToWinDuel;
		[ScenarioEditor(label: "Minimum needed difference in poll results before it is accepted.")]
		public int PollAcceptThreshold;
		[ScenarioEditor(label: "Maximum player imbalance between team 1 and team 2. Selecting 0 will disable auto team balancing.")]
		public int AutoTeamBalanceThreshold;
		[ScenarioEditor(label: "Enables mission recording.")]
		public bool EnableMissionRecording;
		[ScenarioEditor(label: "Sets if the game mode uses single spawning.")]
		public bool SingleSpawn;
		[ScenarioEditor(label: "Disables the inactivity kick timer.")]
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
			Log($"No value set for option {key}, using default value : {key.GetValueText()}", LogLevel.Debug);
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