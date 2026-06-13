using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Duel;
using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
	public class GameModeMenuVM : ViewModel
	{
		public event EventHandler OnCloseMenu;
		public GameModeSettings GameModeSettings;

		private MBBindingList<GameModeCardVM> _gameModes;
		private GameModeCardVM _selectedGameMode;
		private string _filterText;
		private MBBindingList<MapCardVM> _maps;
		private MapCardVM _selectedMap;
		private MBBindingList<OptionVM> _nativeOptions;
		private MBBindingList<OptionVM> _modOptions;
		private MBBindingList<OptionCategoryVM> _modOptionCategories;
		private string _playerSpawnMenuShortDesc;
		private PlayerSpawnMenu _playerSpawnMenuInstance;

		public GameModeMenuVM()
		{
			_playerSpawnMenuInstance = new PlayerSpawnMenu();

			GameModes = new MBBindingList<GameModeCardVM>()
			{
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new LobbyGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new CvCGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new PvCGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new BRGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new CaptainGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new BattleGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new SiegeGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new DuelGameModeSettings()),
				new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new ScenarioGameModeSettings())
			};

			OnGameModeSelected(GameModes[0]);
		}

		[DataSourceProperty]
		public string PlayerSpawnMenuShortDesc
		{
			get
			{
				return _playerSpawnMenuShortDesc;
			}
			set
			{
				if (value != _playerSpawnMenuShortDesc)
				{
					_playerSpawnMenuShortDesc = value;
					OnPropertyChangedWithValue(value, nameof(PlayerSpawnMenuShortDesc));
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
					FilterMaps(_filterText);
					OnPropertyChangedWithValue(value, nameof(FilterText));
				}
			}
		}

		/// <summary>
		/// Filter list of maps with given text filter
		/// </summary>
		public void FilterMaps(string filterText)
		{
			foreach (MapCardVM mapCardVM in _maps)
			{
				if (mapCardVM != null)
				{
					mapCardVM.IsFiltered = !mapCardVM.Name.ToLower().Contains(filterText);
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<GameModeCardVM> GameModes
		{
			get
			{
				return _gameModes;
			}
			set
			{
				if (value != _gameModes)
				{
					_gameModes = value;
					OnPropertyChangedWithValue(value, nameof(GameModes));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<MapCardVM> Maps
		{
			get
			{
				return _maps;
			}
			set
			{
				if (value != _maps)
				{
					_maps = value;
					OnPropertyChangedWithValue(value, nameof(Maps));
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
		public MBBindingList<OptionCategoryVM> ModOptionCategories
		{
			get => _modOptionCategories;
			set
			{
				if (_modOptionCategories != value)
				{
					_modOptionCategories = value;
					OnPropertyChangedWithValue(value, nameof(ModOptionCategories));
				}
			}
		}

		[DataSourceProperty]
		public bool IsAdmin
		{
			get
			{
				return MBNetwork.MyPeer.IsAdmin();
			}
		}

		public void EditPlayerSpawnMenu()
		{
			PlayerSpawnMenuView spawnMenuView = Mission.Current.GetMissionBehavior<PlayerSpawnMenuView>();
			if (spawnMenuView == null) return;
			_playerSpawnMenuInstance ??= new PlayerSpawnMenu();
			spawnMenuView.OpenMenu(_playerSpawnMenuInstance, OnPlayerSpawnMenuEdited, true);
		}

		private void OnPlayerSpawnMenuEdited(PlayerSpawnMenu menu)
		{
			_playerSpawnMenuInstance = menu;
			RefreshPlayerSpawnMenuShortDesc();
		}

		private void RefreshPlayerSpawnMenuShortDesc()
		{
			string defenderTeamsDesc = "Defender team(s):";
			string attackerTeamsDesc = "Attacker team(s):";
			foreach (PlayerTeam team in _playerSpawnMenuInstance.Teams)
			{
				if (team.TeamSide == BattleSideEnum.Defender)
				{
					defenderTeamsDesc += " " + team.Name + "(" + team.Formations.Count + ")";
				}
				else if (team.TeamSide == BattleSideEnum.Attacker)
				{
					attackerTeamsDesc += " " + team.Name + "(" + team.Formations.Count + ")";
				}
			}
			PlayerSpawnMenuShortDesc = defenderTeamsDesc + " | " + attackerTeamsDesc;
		}

		public void RequestVoteForGameMode()
		{
			// TODO : request a vote for selected game mode
			CloseMenu();
		}

		public void RequestGameModeAsAdmin()
		{
			if (_selectedMap is ActCardVM)
			{
				Scenario scenario = ((ActCardVM)_selectedMap).Scenario;
				int actIndex = scenario.Acts.IndexOf(((ActCardVM)_selectedMap).Act);
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new ScenarioPollRequestMessage(
					scenario.Id,
					actIndex,
					true));
				GameNetwork.EndModuleEventAsClient();
			}
			else
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new GameModePollRequestMessage(
					_selectedGameMode.GameModeSettings.TWOptions,
					_selectedGameMode.GameModeSettings.ModOptions,
					true));
				GameNetwork.EndModuleEventAsClient();
			}
			PlayerSpawnMenuMsg.RequestUpdatePlayerSpawnMenu(_playerSpawnMenuInstance);
			CloseMenu();
		}

		private void OnGameModeSelected(GameModeCardVM gameModeCardVM)
		{
			if (_selectedGameMode != null) _selectedGameMode.IsSelected = false;
			ScenarioManager.Instance.RefreshAvailableScenarios();
			_selectedGameMode = gameModeCardVM;
			_selectedGameMode.IsSelected = true;
			RefreshMaps();
			RefreshPlayerSpawnMenuShortDesc();
			RefreshNativeOptions();
			RefreshModOptions();
		}

		private void OnMapSelected(MapCardVM mapCardVM)
		{
			if (_selectedMap != null) _selectedMap.IsSelected = false;
			_selectedMap = mapCardVM;
			_selectedMap.IsSelected = true;
			_selectedGameMode.GameModeSettings.TWOptions[OptionType.Map] = mapCardVM.MapInfo.Name;
			if (mapCardVM is ActCardVM actCardVM)
			{
				_playerSpawnMenuInstance = actCardVM.Act.PlayerSpawnMenu;
				RefreshPlayerSpawnMenuShortDesc();
				_selectedGameMode.GameModeSettings = actCardVM.Act.ActSettings;
				RefreshNativeOptions();
				RefreshModOptions();
			}
		}

		private void RefreshMaps()
		{
			Maps = new MBBindingList<MapCardVM>();

			if (_selectedGameMode.Name == "Scenario")
			{
				foreach (Scenario scenario in ScenarioManager.Instance.AvailableScenario)
				{
					foreach (Act act in scenario.Acts)
					{
						Maps.Add(new ActCardVM(Scenes.Find(scene => scene.Name == act.MapID), scenario, act, new Action<MapCardVM>(OnMapSelected)));
					}
				}
			}
			else
			{
				List<SceneInfo> availableMaps = _selectedGameMode.GameModeSettings.GetAvailableMaps();

				foreach (SceneInfo map in availableMaps)
				{
					Maps.Add(new MapCardVM(map, new Action<MapCardVM>(OnMapSelected)));
				}
			}

			// Retrieve previously selected map index
			int mapIndex = -1;
			if (_selectedMap != null)
			{
				mapIndex = Maps.FindIndex(map => map.MapInfo.Name == _selectedMap.MapInfo.Name);
			}

			// Select same map as before if possible, otherwise select default
			if (mapIndex != -1) OnMapSelected(Maps[mapIndex]);
			else if (Maps.Count > 0) OnMapSelected(Maps[0]);
		}

		private void RefreshNativeOptions()
		{
			NativeOptions = new MBBindingList<OptionVM>();

			// Set default options only if selected map is not a Scenario Act
			if (_selectedMap is not ActCardVM)
			{
				_selectedGameMode.GameModeSettings.SetDefaultNativeOptions();
				_selectedGameMode.GameModeSettings.TWOptions[OptionType.Map] = _selectedMap.MapInfo.Name;
			}

			List<OptionType> optionTypes = _selectedGameMode.GameModeSettings.GetAvailableNativeOptions();
			foreach (OptionType optionType in optionTypes)
			{
				MultiplayerOption option = MultiplayerOption.CreateMultiplayerOption(optionType);
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();

				FieldInfo fi = typeof(TWConfig).GetField(optionType.ToString());
				if (fi == null) continue; // Skip if option type is not found

				// Retrieve attribute for option type
				ConfigPropertyAttribute attribute = fi.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute != null && !attribute.IsEditable) continue; // Skip if option is not editable

				switch (optionProperty.OptionValueType)
				{
					case OptionValueType.Bool:
						NativeOptions.Add(new BoolOptionVM(
							new TextObject(attribute.Label ?? optionType.ToString()),
							new TextObject(attribute.Tooltip ?? optionType.ToString()),
							() => (bool)_selectedGameMode.GameModeSettings.TWOptions[optionType],
							newValue => _selectedGameMode.GameModeSettings.TWOptions[optionType] = newValue));
						break;
					case OptionValueType.Integer:
						int boundMin = optionProperty.BoundsMin;
						int boundMax = optionProperty.BoundsMax;
						TWConfig.GetAdjustedBounds(optionType, ref boundMin, ref boundMax);
						
						NativeOptions.Add(new NumericOptionVM(
							new TextObject(attribute.Label ?? optionType.ToString()),
							new TextObject(attribute.Tooltip ?? optionType.ToString()),
							() => (int)_selectedGameMode.GameModeSettings.TWOptions[optionType],
							newValue => _selectedGameMode.GameModeSettings.TWOptions[optionType] = (int)newValue,
							boundMin,
							boundMax,
							true, true));
						break;
					case OptionValueType.String:
						if (option.OptionType == OptionType.CultureTeam1 || option.OptionType == OptionType.CultureTeam2)
						{
							NativeOptions.Add(new SelectionOptionVM(
								new TextObject(attribute.Label ?? optionType.ToString()),
								new TextObject(attribute.Tooltip ?? optionType.ToString()),
								new SelectionOptionData(
									() => GetFactionChoices().FindIndex(item => item.Data == (string)_selectedGameMode.GameModeSettings.TWOptions[optionType]),
									newValue => _selectedGameMode.GameModeSettings.TWOptions[optionType] = GetFactionChoices().ElementAtOrDefault(newValue).Data,
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
			if (_selectedMap is not ActCardVM || _selectedGameMode.GameModeSettings.ModOptions == null)
			{
				_selectedGameMode.GameModeSettings.SetDefaultModOptions();
			}

			RefreshModOptionsUI();
		}

		private void RefreshModOptionsUI()
		{
			// Store expanded states
			Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
			if (ModOptionCategories != null)
			{
				foreach (var category in ModOptionCategories)
				{
					expandedStates[category.Name] = category.IsExpanded;
				}
			}

			ModOptionCategories = new MBBindingList<OptionCategoryVM>();
			Dictionary<string, OptionCategoryVM> categories = new Dictionary<string, OptionCategoryVM>();

			Config modOptions = _selectedGameMode.GameModeSettings.ModOptions;
			List<string> availableOptions = _selectedGameMode.GameModeSettings.GetAvailableModOptions();

			foreach (var field in ConfigManager.Instance.ConfigFields.Where(field => availableOptions.Contains(field.Value.Name)))
			{
				FieldInfo fieldInfo = field.Value;
				ConfigPropertyAttribute configPropertyAttribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();

				// Check if dependency is satisfied
				if (!configPropertyAttribute.IsDependencySatisfied(modOptions))
				{
					continue;
				}

				// Get or create category
				string categoryName = configPropertyAttribute.Category ?? "General";
				if (!categories.ContainsKey(categoryName))
				{
					// Restore expanded state
					bool isExpanded = expandedStates.ContainsKey(categoryName)
						? expandedStates[categoryName]
						: (categoryName == "General");

					categories[categoryName] = new OptionCategoryVM(categoryName, isExpanded);
				}

				// Create option VM
				OptionVM optionVM = CreateOptionVM(fieldInfo, configPropertyAttribute, modOptions);
				if (optionVM != null)
				{
					categories[categoryName].Options.Add(optionVM);
				}
			}

			// Add categories to list
			foreach (var category in categories.Values)
			{
				ModOptionCategories.Add(category);
			}
		}

		private OptionVM CreateOptionVM(FieldInfo fieldInfo, ConfigPropertyAttribute configPropertyAttribute, Config modOptions)
		{
				if (fieldInfo.FieldType == typeof(bool))
				{
				return new BoolOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (bool)fieldInfo.GetValue(modOptions),
							newValue =>
							{
								fieldInfo.SetValue(modOptions, newValue);
								if (ConfigPropertyAttribute.HasDependents(fieldInfo.Name, modOptions))
								{
									RefreshModOptionsUI();
								}
					});
				}
				else if (fieldInfo.FieldType == typeof(int))
				{
				return new NumericOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (int)fieldInfo.GetValue(modOptions),
							newValue => fieldInfo.SetValue(modOptions, (int)newValue),
							configPropertyAttribute.MinValue,
							configPropertyAttribute.MaxValue,
					true, true);
				}
				else if (fieldInfo.FieldType == typeof(float))
				{
				return new NumericOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							() => (float)fieldInfo.GetValue(modOptions),
							newValue => fieldInfo.SetValue(modOptions, newValue),
							configPropertyAttribute.MinValue,
							configPropertyAttribute.MaxValue,
					false, true);
				}
				// TODO : add an option for string ? (other than enums)
				else if (fieldInfo.FieldType == typeof(string) && configPropertyAttribute.DataType != AllianceData.DataTypes.None)
				{
					List<SelectionItem> selectionItems = GetSelectionItemsFromValues(configPropertyAttribute.PossibleValues.ToList());
				return new SelectionOptionVM(
							new TextObject(configPropertyAttribute.Label),
							new TextObject(configPropertyAttribute.Tooltip),
							new SelectionOptionData(
								() => selectionItems.FindIndex(item => item.Data == (string)fieldInfo.GetValue(modOptions)),
								newValue => fieldInfo.SetValue(modOptions, selectionItems.ElementAtOrDefault(newValue).Data),
								2,
								selectionItems),
					false);
				}

			return null;
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

		public void CloseMenu()
		{
			OnCloseMenu(this, EventArgs.Empty);
		}
	}
}