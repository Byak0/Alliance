using Alliance.Client.Extensions.GameModeMenu.ViewModels.Options;
using Alliance.Client.GameModes.Story.Scenarios;
using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Utilities;
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

        public GameModeMenuVM()
        {
            GameModes = new MBBindingList<GameModeCardVM>()
            {
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new LobbyGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new CvCGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new PvCGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new BRGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new CaptainGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new BattleGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new SiegeGameModeSettings()),
                new GameModeCardVM(new Action<GameModeCardVM>(OnGameModeSelected), new ScenarioGameModeSettings())
            };

            OnGameModeSelected(GameModes[0]);
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
                    OnPropertyChangedWithValue(value, "FilterText");
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
                    OnPropertyChangedWithValue(value, "GameModes");
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
                    OnPropertyChangedWithValue(value, "Maps");
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
                    OnPropertyChangedWithValue(value, "NativeOptions");
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
                    OnPropertyChangedWithValue(value, "ModOptions");
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

        public void RequestVoteForGameMode()
        {
            // TODO : request a vote for selected game mode
            CloseMenu();
        }

        public void RequestGameModeAsAdmin()
        {
            if (_selectedGameMode.GameModeSettings.GameMode == "Scenario")
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
                    _selectedGameMode.GameModeSettings.NativeOptions,
                    _selectedGameMode.GameModeSettings.ModOptions,
                    true));
                GameNetwork.EndModuleEventAsClient();
            }
            CloseMenu();
        }

        private void OnGameModeSelected(GameModeCardVM gameModeCardVM)
        {
            if (_selectedGameMode != null) _selectedGameMode.IsSelected = false;
            _selectedGameMode = gameModeCardVM;
            _selectedGameMode.IsSelected = true;
            RefreshMaps();
            RefreshNativeOptions();
            RefreshModOptions();
        }

        private void OnMapSelected(MapCardVM mapCardVM)
        {
            if (_selectedMap != null) _selectedMap.IsSelected = false;
            _selectedMap = mapCardVM;
            _selectedMap.IsSelected = true;
            _selectedGameMode.GameModeSettings.SetNativeOption(OptionType.Map, mapCardVM.MapInfo.Name);
            if (mapCardVM is ActCardVM)
            {
                _selectedGameMode.GameModeSettings = (mapCardVM as ActCardVM).Act.ActSettings;
                RefreshNativeOptions();
                RefreshModOptions();
            }
        }

        private void RefreshMaps()
        {
            Maps = new MBBindingList<MapCardVM>();

            if (_selectedGameMode.GameModeSettings.GameMode == "Scenario")
            {
                Type clientScenariosType = typeof(ClientScenarios);
                MethodInfo[] methodInfos = clientScenariosType.GetMethods(BindingFlags.Public | BindingFlags.Static);

                // Iterate over each method and invoke it
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    // Check if the method returns a Scenario object
                    if (methodInfo.ReturnType == typeof(Scenario))
                    {
                        object result = methodInfo.Invoke(null, null);
                        Scenario scenario = result as Scenario;
                        foreach (Act act in scenario.Acts)
                        {
                            Maps.Add(new ActCardVM(Scenes.Find(scene => scene.Name == act.MapID), scenario, act, new Action<MapCardVM>(OnMapSelected)));
                        }
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
            // Refresh native options
            NativeOptions = new MBBindingList<OptionVM>();

            _selectedGameMode.GameModeSettings.SetDefaultNativeOptions();
            List<OptionType> optionTypes = _selectedGameMode.GameModeSettings.GetAvailableNativeOptions();
            foreach (OptionType optionType in optionTypes)
            {
                MultiplayerOption option = Enumerable.First(_selectedGameMode.GameModeSettings.NativeOptions, (MultiplayerOption x) => x.OptionType == optionType);
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();

                switch (optionProperty.OptionValueType)
                {
                    case OptionValueType.Bool:
                        NativeOptions.Add(new BoolOptionVM(option));
                        break;
                    case OptionValueType.Integer:
                        NativeOptions.Add(new NumericOptionVM(option));
                        break;
                    case OptionValueType.String:
                        if (option.OptionType == OptionType.CultureTeam1 || option.OptionType == OptionType.CultureTeam2)
                        {
                            NativeOptions.Add(new SelectionOptionVM(option, GetFactionChoices()));
                        }
                        break;
                }
            }
        }

        private void RefreshModOptions()
        {
            ModOptions = new MBBindingList<OptionVM>();

            _selectedGameMode.GameModeSettings.SetDefaultModOptions();
            Config modOptions = _selectedGameMode.GameModeSettings.ModOptions;
            List<string> availableOptions = _selectedGameMode.GameModeSettings.GetAvailableModOptions();

            foreach (var field in ConfigManager.Instance.ConfigFields.Where(field => availableOptions.Contains(field.Value.Name)))
            {
                FieldInfo fieldInfo = field.Value;
                object fieldValue = fieldInfo.GetValue(modOptions);
                ConfigPropertyAttribute configPropertyAttribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();

                switch (configPropertyAttribute.ValueType)
                {
                    case ConfigValueType.Bool:
                        ModOptions.Add(
                        new BoolOptionVM(
                            new TextObject(configPropertyAttribute.Name),
                            new TextObject(configPropertyAttribute.Description),
                            () => (bool)fieldInfo.GetValue(modOptions),
                            newValue => fieldInfo.SetValue(modOptions, newValue))
                        );
                        break;
                    case ConfigValueType.Integer:
                        ModOptions.Add(
                        new NumericOptionVM(
                            new TextObject(configPropertyAttribute.Name),
                            new TextObject(configPropertyAttribute.Description),
                            () => (int)fieldInfo.GetValue(modOptions),
                            newValue => fieldInfo.SetValue(modOptions, (int)newValue),
                            configPropertyAttribute.MinValue,
                            configPropertyAttribute.MaxValue,
                            true, true)
                        );
                        break;
                    case ConfigValueType.Float:
                        ModOptions.Add(
                        new NumericOptionVM(
                            new TextObject(configPropertyAttribute.Name),
                            new TextObject(configPropertyAttribute.Description),
                            () => (float)fieldInfo.GetValue(modOptions),
                            newValue => fieldInfo.SetValue(modOptions, newValue),
                            configPropertyAttribute.MinValue,
                            configPropertyAttribute.MaxValue,
                            false, true)
                        );
                        break;
                    case ConfigValueType.Enum:
                        List<SelectionItem> selectionItems = GetSelectionItemsFromValues(DefaultConfig.GetAvailableValuesForOption(fieldInfo));
                        ModOptions.Add(
                        new SelectionOptionVM(
                            new TextObject(configPropertyAttribute.Name),
                            new TextObject(configPropertyAttribute.Description),
                            new SelectionOptionData(
                                () => selectionItems.FindIndex(item => item.Data == (string)fieldInfo.GetValue(modOptions)),
                                newValue => fieldInfo.SetValue(modOptions, selectionItems.ElementAtOrDefault(newValue).Data),
                                2,
                                selectionItems),
                            false)
                        );
                        break;
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

        public void CloseMenu()
        {
            OnCloseMenu(this, EventArgs.Empty);
        }
    }
}