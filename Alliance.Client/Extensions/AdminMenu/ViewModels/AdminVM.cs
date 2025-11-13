using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
	public class AdminVM : ViewModel
	{
		private string _Username;
		private string _Health;
		private string _Position;
		private string _Kill;
		private string _Death;
		private string _Assist;
		private string _Score;
		private string _kickCounter;
		private string _banCounter;
		private string _warningCounter;
		private string _filterText;
		private CharacterViewModel _unitCharacter;
		private MBBindingList<NetworkPeerVM> _networkCommunicators;
		private MBBindingList<ServerMessageVM> _serverMessage;
		private MBBindingList<PlayerDataVM> _playersData;
		private MBBindingList<OptionVM> _nativeOptions;
		private TWConfig _newNativeOptions;
		private MBBindingList<OptionVM> _modOptions;
		private Config _newModOptions;
		private NetworkPeerVM _selectedPeer;
		private bool _isSudo;
		private bool _isVisible;
		private bool _showAdminTab;
		private bool _showPlayerTab;
		private bool _showToolsTab;
		private string _banReason = "";

		public AdminVM()
		{
			_isSudo = GameNetwork.MyPeer.IsSudo();
			_unitCharacter = new CharacterViewModel();
			_serverMessage = new MBBindingList<ServerMessageVM>();
			_showAdminTab = true;
			RefreshPlayerList();
			RefreshNativeOptions();
			RefreshModOptions();

			PlayerService.PlayerDataUpdated += OnPlayerDataUpdated;
		}

		~AdminVM()
		{
			PlayerService.PlayerDataUpdated -= OnPlayerDataUpdated;
		}

		[DataSourceProperty]
		public bool IsSudo
		{
			get
			{
				return _isSudo;
			}
			set
			{
				if (value != _isSudo)
				{
					_isSudo = value;
					OnPropertyChangedWithValue(value, "IsSudo");
				}
			}
		}

		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
				if (value != _isVisible)
				{
					_isVisible = value;
					OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowAdminTab
		{
			get
			{
				return _showAdminTab;
			}
			set
			{
				if (value != _showAdminTab)
				{
					_showAdminTab = value;
					OnPropertyChangedWithValue(value, "ShowAdminTab");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowPlayerTab
		{
			get
			{
				return _showPlayerTab;
			}
			set
			{
				if (value != _showPlayerTab)
				{
					_showPlayerTab = value;
					OnPropertyChangedWithValue(value, "ShowPlayerTab");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowOptionsTab
		{
			get
			{
				return _showToolsTab;
			}
			set
			{
				if (value != _showToolsTab)
				{
					_showToolsTab = value;
					OnPropertyChangedWithValue(value, "ShowOptionsTab");
				}
			}
		}


		[DataSourceProperty]
		public string Username
		{
			get
			{
				return _Username;
			}
			set
			{
				if (value != _Username)
				{
					_Username = value;
					OnPropertyChangedWithValue(value, "Username");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<ServerMessageVM> ServerMessage
		{
			get
			{
				return _serverMessage;
			}
			set
			{
				if (value != _serverMessage)
				{
					_serverMessage = value;
					OnPropertyChangedWithValue(value, "ServerMessage");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<NetworkPeerVM> NetworkPeers
		{
			get
			{
				return _networkCommunicators;
			}
			set
			{
				if (value != _networkCommunicators)
				{
					_networkCommunicators = value;
					OnPropertyChangedWithValue(value, "NetworkPeers");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerDataVM> PlayersData
		{
			get
			{
				return _playersData;
			}
			set
			{
				if (value != _playersData)
				{
					_playersData = value;
					OnPropertyChangedWithValue(value, "PlayersData");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<OptionVM> NativeOptions
		{
			get
			{
				return _nativeOptions;
			}
			set
			{
				if (value != _nativeOptions)
				{
					_nativeOptions = value;
					OnPropertyChangedWithValue(value, nameof(NativeOptions));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<OptionVM> ModOptions
		{
			get
			{
				return _modOptions;
			}
			set
			{
				if (value != _modOptions)
				{
					_modOptions = value;
					OnPropertyChangedWithValue(value, nameof(ModOptions));
				}
			}
		}

		[DataSourceProperty]
		public CharacterViewModel UnitCharacter
		{
			get
			{
				return _unitCharacter;
			}
			set
			{
				if (value != _unitCharacter)
				{
					_unitCharacter = value;
					OnPropertyChangedWithValue(value, "UnitCharacter");
				}
			}
		}

		[DataSourceProperty]
		public string Health
		{
			get
			{
				return _Health;
			}
			set
			{
				if (value != _Health)
				{
					_Health = value;
					OnPropertyChangedWithValue(value, "Health");
				}
			}
		}

		[DataSourceProperty]
		public string Position
		{
			get
			{
				return _Position;
			}
			set
			{
				if (value != _Position)
				{
					_Position = value;
					OnPropertyChangedWithValue(value, "Position");
				}
			}
		}

		[DataSourceProperty]
		public string Kill
		{
			get
			{
				return _Kill;
			}
			set
			{
				if (value != _Kill)
				{
					_Kill = value;
					OnPropertyChangedWithValue(value, "Kill");
				}
			}
		}

		[DataSourceProperty]
		public string Death
		{
			get
			{
				return _Death;
			}
			set
			{
				if (value != _Death)
				{
					_Death = value;
					OnPropertyChangedWithValue(value, "Death");
				}
			}
		}

		[DataSourceProperty]
		public string Assist
		{
			get
			{
				return _Assist;
			}
			set
			{
				if (value != _Assist)
				{
					_Assist = value;
					OnPropertyChangedWithValue(value, "Assist");
				}
			}
		}

		[DataSourceProperty]
		public string Score
		{
			get
			{
				return _Score;
			}
			set
			{
				if (value != _Score)
				{
					_Score = value;
					OnPropertyChangedWithValue(value, "Score");
				}
			}
		}

		[DataSourceProperty]
		public string KickCounter
		{
			get
			{
				return _kickCounter;
			}
			set
			{
				if (value != _kickCounter)
				{
					_kickCounter = value;
					OnPropertyChangedWithValue(value, "KickCounter");
				}
			}
		}

		[DataSourceProperty]
		public string BanCounter
		{
			get
			{
				return _banCounter;
			}
			set
			{
				if (value != _banCounter)
				{
					_banCounter = value;
					OnPropertyChangedWithValue(value, "BanCounter");
				}
			}
		}

		[DataSourceProperty]
		public string WarningCounter
		{
			get
			{
				return _warningCounter;
			}
			set
			{
				if (value != _warningCounter)
				{
					_warningCounter = value;
					OnPropertyChangedWithValue(value, "WarningCounter");
				}
			}
		}

		[DataSourceProperty]
		public string FilterText
		{
			get
			{
				return _filterText;
			}
			set
			{
				if (value != _filterText)
				{
					_filterText = value;
					FilterPlayers(_filterText);
					OnPropertyChangedWithValue(value, "FilterText");
				}
			}
		}

		[DataSourceProperty]
		public string BanReason
		{
			get => _banReason;
			set
			{
				if (value != _banReason)
				{
					_banReason = value;
					OnPropertyChanged(nameof(BanReason));
				}
			}
		}

		public void OpenAdminTab()
		{
			ShowAdminTab = true;
			ShowPlayerTab = false;
			ShowOptionsTab = false;
		}

		public void OpenPlayerTab()
		{
			ShowAdminTab = false;
			ShowPlayerTab = true;
			ShowOptionsTab = false;
		}

		public void OpenToolsTab()
		{
			ShowAdminTab = false;
			ShowPlayerTab = false;
			ShowOptionsTab = true;
		}

		/// <summary>
		/// Filter list of players with given text filter
		/// </summary>
		public void FilterPlayers(string filterText)
		{
			foreach (NetworkPeerVM networkPeerVM in _networkCommunicators)
			{
				if (networkPeerVM != null)
				{
					networkPeerVM.IsFiltered = !networkPeerVM.Username.ToLower().Contains(filterText);
				}
			}

			foreach (PlayerDataVM playerDataVM in _playersData)
			{
				playerDataVM.IsFiltered = !playerDataVM.Username.ToLower().Contains(filterText);
			}
		}

		[UsedImplicitly]
		public void Heal()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { Heal = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void HealAll()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { HealAll = true, PlayerSelected = PlayerId.Empty });
		}

		[UsedImplicitly]
		public void GodMod()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { GodMod = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void GodModAll()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { GodModAll = true, PlayerSelected = PlayerId.Empty });
		}

		[UsedImplicitly]
		public void KillPlayer()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { Kill = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void KillPlayers()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { KillPlayers = true, PlayerSelected = PlayerId.Empty });
		}

		[UsedImplicitly]
		public void KillBots()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { KillBots = true, PlayerSelected = PlayerId.Empty });
		}

		[UsedImplicitly]
		public void KickPlayer()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { Kick = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		public void SendWarningToPlayer(string customWarning)
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { SendWarningToPlayer = true, PlayerSelected = _selectedPeer.PlayerId, WarningMessageToPlayer = customWarning });
		}

		[UsedImplicitly]
		public void PrompWarningMessageSelection()
		{
			if (_selectedPeer == null) { return; }
			// Prompt a text inquiry for user to enter a custom message
			InformationManager.ShowTextInquiry(
				new TextInquiryData("Custom Warning message",
				"Write your warning message :",
				true,
				true,
				new TextObject("Done", null).ToString(),
				new TextObject("Cancel", null).ToString(),
				new Action<string>(SendWarningToPlayer),
				null,
				false,
				null,
				"",
				"Your warning message"),
				false);
		}

		[UsedImplicitly]
		public void BanPlayer()
		{
			if (_selectedPeer == null)
			{
				Log("No player selected.", LogLevel.Warning);
				return;
			}

			// // Prompt a text inquiry for user to enter ban reason
			InformationManager.ShowTextInquiry(
				new TextInquiryData(
					"Ban player",
					$"Ban reason for {_selectedPeer.Username}:",
					true,
					true,
					"Confirm",
					"Cancel",
					new Action<string>(reason =>
					{
						if (string.IsNullOrWhiteSpace(reason))
						{
							Log("Ban reason can't be empty.", LogLevel.Warning);
							return;
						}
						else if (reason.Length > CompressionHelper.StringMaxLength)
						{
							Log($"Ban reason can't exceed {CompressionHelper.StringMaxLength} characters.", LogLevel.Warning);
							return;
						}
						SendBanRequest(reason);
					}),
					null,
					false,
					null,
					"",
					""
				),
				false
			);
		}

		private void SendBanRequest(string reason)
		{
			try
			{
				ClientAdminMenuMsg.SendMessageToServer(new AdminClient()
				{
					Ban = true,
					PlayerSelected = _selectedPeer.PlayerId,
					BanReason = reason
				});
			}
			catch (Exception ex)
			{
				Log($"Erreur SendBanRequest: {ex.Message}", LogLevel.Error);
			}
		}

		[UsedImplicitly]
		public void ToggleMutePlayer()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { ToggleMutePlayer = true, PlayerSelected = _selectedPeer.PlayerId });
			_selectedPeer.IsMuted = !_selectedPeer.IsMuted;
		}

		[UsedImplicitly]
		public void Respawn()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { Respawn = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void TeleportToPlayer()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { TeleportToPlayer = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void TeleportPlayerToYou()
		{
			if (_selectedPeer == null) { return; }
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { TeleportPlayerToYou = true, PlayerSelected = _selectedPeer.PlayerId });
		}

		[UsedImplicitly]
		public void TeleportAllPlayerToYou()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { TeleportAllPlayerToYou = true, PlayerSelected = PlayerId.Empty });
		}

		[UsedImplicitly]
		public void ToggleModoVision()
		{
			bool isModoVisionActivated = !UserConfig.Instance.CanSeeAllPlayersNames;
			UserConfig.Instance.CanSeeAllPlayersNames = isModoVisionActivated;
			UserConfig.Instance.Save();
			Log("Modo vision is now : " + (isModoVisionActivated ? "ON" : "OFF"));
		}

		public void RefreshPlayerList()
		{
			NetworkPeers = new MBBindingList<NetworkPeerVM>();
			GameNetwork.NetworkPeers.ToList().ForEach(x =>
			{
				NetworkPeers.Add(new NetworkPeerVM()
				{
					Username = x.UserName,
					AgentIndex = x.ControlledAgent?.Index ?? -1,
					PlayerId = x.VirtualPlayer?.Id ?? PlayerId.Empty,
					IsSelected = x.VirtualPlayer.Id == _selectedPeer?.PlayerId,
					OnSelect = OnNetworkPeerSelected,
					IsMuted = x.IsMuted()
				});
			});
			_selectedPeer = NetworkPeers.FirstOrDefault(x => x.PeerId == _selectedPeer?.PeerId);

			PlayersData = new MBBindingList<PlayerDataVM>();
			// Populate from stored players data
			foreach (AL_PlayerData playerData in PlayerStore.Instance.AllPlayersData.Values)
			{
				PlayersData.Add(new PlayerDataVM(playerData) { IsOnline = PlayerStore.Instance.PlayerIdToCommunicator.ContainsKey(playerData.Id) });
			}
			// Add missing online players (not in the stored data)
			foreach (NetworkCommunicator player in GameNetwork.NetworkPeers)
			{
				if (PlayerStore.Instance.AllPlayersData.ContainsKey(player.VirtualPlayer.Id)) continue;
				PlayersData.Add(new PlayerDataVM(player));
			}
		}

		private void OnPlayerDataUpdated(AL_PlayerData updatedData, NetworkCommunicator player)
		{
			if (updatedData == null)
				return;

			// Find the matching PlayerDataVM by PlayerId
			PlayerDataVM vm = PlayersData?.FirstOrDefault(x => x.PlayerStringId == updatedData.Id.ToString());
			if (vm != null)
				vm.UpdateFrom(updatedData);
			else
				PlayersData?.Add(new PlayerDataVM(updatedData) { IsOnline = player != null });
		}

		public void SelectTarget(Agent agent)
		{
			NetworkPeerVM peerVM = null;
			// Look for player match
			if (agent.MissionPeer?.Peer != null) peerVM = _networkCommunicators.Where(x => x.PeerId == agent.MissionPeer?.Peer?.Id.ToString()).FirstOrDefault();
			// If nothing was found, look for agent match
			if (peerVM == null && agent.Index != -1) peerVM = _networkCommunicators.Where(x => x.AgentIndex == agent.Index).FirstOrDefault();
			// If still nothing was found, add new VM
			if (peerVM == null)
			{
				peerVM = new NetworkPeerVM()
				{
					Username = agent.MissionPeer?.Name ?? agent.Name,
					AgentIndex = agent.Index,
					PlayerId = agent.MissionPeer?.Peer?.Id ?? PlayerId.Empty,
					IsSelected = false,
					OnSelect = OnNetworkPeerSelected
				};
			}
			OnNetworkPeerSelected(peerVM);
		}

		/// <summary>
		/// Update player related informations.
		/// </summary>
		private void UpdatePlayerVM(NetworkCommunicator networkCommunicator)
		{
			MissionPeer peer = networkCommunicator?.GetComponent<MissionPeer>();
			if (peer != null)
			{
				Kill = peer.KillCount.ToString();
				Assist = peer.AssistCount.ToString();
				Death = peer.DeathCount.ToString();
				Score = peer.Score.ToString();
				PlayerStore.Instance.OnlinePlayersData.TryGetValue(networkCommunicator, out AL_PlayerData playerData);
				KickCounter = playerData?.KickCount.ToString() ?? "0";
				BanCounter = playerData?.BanCount.ToString() ?? "0";
				WarningCounter = playerData?.WarningCount.ToString() ?? "0";
			}
			if (networkCommunicator?.ControlledAgent != null)
			{
				UpdateAgentVM(networkCommunicator.ControlledAgent);
			}
		}

		/// <summary>
		/// Update agent related informations.
		/// </summary>
		private void UpdateAgentVM(Agent selectedAgent)
		{
			UnitCharacter = new CharacterViewModel();
			if (selectedAgent != null)
			{
				Health = selectedAgent.Health.ToString();
				Position = selectedAgent.Position.ToString();
				if (selectedAgent.Character != null) UnitCharacter.FillFrom(selectedAgent.Character);
			}
		}

		private void OnNetworkPeerSelected(NetworkPeerVM peer)
		{
			if (peer == null) return;
			if (_selectedPeer != null) _selectedPeer.IsSelected = false;
			NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == peer.PeerId).FirstOrDefault();
			// Player informations
			if (networkCommunicator != null)
			{
				Username = networkCommunicator.UserName;
				UpdatePlayerVM(networkCommunicator);
			}
			// Agent informations
			else if (peer.AgentIndex != -1)
			{
				Agent agent = Mission.Current.Agents.Where(x => x.Index == peer.AgentIndex).FirstOrDefault();
				Username = agent?.Name;
				UpdateAgentVM(agent);
			}
			peer.IsSelected = true;
			_selectedPeer = peer;
		}

		[UsedImplicitly]
		public void ResetOptions()
		{
			RefreshModOptions();
			RefreshNativeOptions();
		}

		[UsedImplicitly]
		public void ApplyOptions()
		{
			ClientAdminMenuMsg.RequestUpdateOptionsToServer(_newNativeOptions, _newModOptions);
		}

		private void RefreshNativeOptions()
		{
			NativeOptions = new MBBindingList<OptionVM>();
			_newNativeOptions = ConfigManager.Instance.GetNativeOptionsCopy();

			for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
			{
				OptionType currentType = optionType;
				MultiplayerOption option = MultiplayerOption.CreateMultiplayerOption(currentType);
				MultiplayerOptionsProperty optionProperty = currentType.GetOptionProperty();

				FieldInfo fi = typeof(TWConfig).GetField(currentType.ToString());
				if (fi == null) continue; // Skip if option type is not found

				// Retrieve attribute for option type
				ConfigPropertyAttribute attribute = fi.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute != null && !attribute.IsEditable) continue; // Skip if option is not editable

				switch (optionProperty.OptionValueType)
				{
					case OptionValueType.Bool:
						NativeOptions.Add(new BoolOptionVM(
							new TextObject(attribute.Label ?? currentType.ToString()),
							new TextObject(attribute.Tooltip ?? currentType.ToString()),
							() => (bool)_newNativeOptions[currentType],
							newValue => _newNativeOptions[currentType] = newValue));
						break;
					case OptionValueType.Integer:
						NativeOptions.Add(new NumericOptionVM(
							new TextObject(attribute.Label ?? currentType.ToString()),
							new TextObject(attribute.Tooltip ?? currentType.ToString()),
							() => (int)_newNativeOptions[currentType],
							newValue => _newNativeOptions[currentType] = (int)newValue,
							optionProperty.BoundsMin,
							optionProperty.BoundsMax,
							true, true));
						break;
					case OptionValueType.String:
						if (currentType == OptionType.CultureTeam1 || currentType == OptionType.CultureTeam2)
						{
							NativeOptions.Add(new SelectionOptionVM(
								new TextObject(attribute.Label ?? currentType.ToString()),
								new TextObject(attribute.Tooltip ?? currentType.ToString()),
								new SelectionOptionData(
									() => GetFactionChoices().FindIndex(item => item.Data == (string)_newNativeOptions[currentType]),
									newValue => _newNativeOptions[currentType] = GetFactionChoices().ElementAtOrDefault(newValue).Data,
									2,
									GetFactionChoices()),
								false));
						}
						break;
				}
			}
		}

		private void RefreshModOptions()
		{
			ModOptions = new MBBindingList<OptionVM>();
			_newModOptions = ConfigManager.Instance.GetModOptionsCopy();

			List<string> availableOptions = typeof(Config)
											.GetFields(BindingFlags.Public | BindingFlags.Instance)
											.Select(field => field.Name)
											.ToList();

			foreach (var field in ConfigManager.Instance.ConfigFields.Where(field => availableOptions.Contains(field.Value.Name)))
			{
				FieldInfo fieldInfo = field.Value;
				object fieldValue = fieldInfo.GetValue(_newModOptions);
				ConfigPropertyAttribute configPropertyAttribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();

				if (fieldInfo.FieldType == typeof(bool))
				{
					ModOptions.Add(
						new BoolOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (bool)fieldInfo.GetValue(_newModOptions),
							newValue => fieldInfo.SetValue(_newModOptions, newValue))
						);
				}
				else if (fieldInfo.FieldType == typeof(int))
				{
					ModOptions.Add(
						new NumericOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (int)fieldInfo.GetValue(_newModOptions),
							newValue => fieldInfo.SetValue(_newModOptions, (int)newValue),
							configPropertyAttribute.MinValue,
							configPropertyAttribute.MaxValue,
							true, true)
						);
				}
				else if (fieldInfo.FieldType == typeof(float))
				{
					ModOptions.Add(
						new NumericOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (float)fieldInfo.GetValue(_newModOptions),
							newValue => fieldInfo.SetValue(_newModOptions, newValue),
							configPropertyAttribute.MinValue,
							configPropertyAttribute.MaxValue,
							false, true)
						);
				}
				// TODO : add an option for string ? (other than enums)
				else if (fieldInfo.FieldType == typeof(string) && configPropertyAttribute.DataType != AllianceData.DataTypes.None)
				{
					List<SelectionItem> selectionItems = GetSelectionItemsFromValues(configPropertyAttribute.PossibleValues.ToList());
					ModOptions.Add(
						new SelectionOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							new SelectionOptionData(
								() => selectionItems.FindIndex(item => item.Data == (string)fieldInfo.GetValue(_newModOptions)),
								newValue => fieldInfo.SetValue(_newModOptions, selectionItems.ElementAtOrDefault(newValue).Data),
								2,
								selectionItems),
							false)
						);
				}
			}
		}

		private List<SelectionItem> GetSelectionItemsFromValues(List<string> values)
		{
			List<SelectionItem> optionValues = new List<SelectionItem>();
			foreach (string value in values)
			{
				optionValues.Add(new SelectionItem(false, value));
			}
			return optionValues;
		}

		private List<SelectionItem> GetFactionChoices()
		{
			List<SelectionItem> choices = new List<SelectionItem>();
			foreach (string faction in Factions.Instance.AvailableCultures.Keys)
			{
				choices.Add(new SelectionItem(false, faction));
			}

			return choices;
		}
	}
}
