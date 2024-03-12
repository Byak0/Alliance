using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.Models
{
    /// <summary>
    /// Singleton class to store current choice of troop to spawn (character, number, difficulty, formation, etc.).
    /// Changes to a property send events to its corresponding listeners.
    /// </summary>
    public sealed class SpawnTroopsModel
    {
        public event Action OnFactionSelected;
        public event Action OnFormationSelected;
        public event Action OnTroopSelected;
        public event Action OnPerkSelected;
        public event Action OnTroopCountUpdated;
        public event Action OnCustomTroopCountUpdated;
        public event Action OnFormationUpdated;
        public event Action OnDifficultyUpdated;
        public event Action<TroopSpawnedEventArgs> OnTroopSpawned;

        private int _customTroopCount = 100;
        private int _troopCount = 1;
        private int _formationSelected = 1;
        private BasicCultureObject _selectedFaction;
        private BasicCharacterObject _selectedTroop;
        private float _difficulty;
        private int _difficultyLevel;
        private List<int> _selectedPerks = new List<int>();
        private Team _selectedTeam;

        public int CustomTroopCount
        {
            get
            {
                return _customTroopCount;
            }
            set
            {
                if (_customTroopCount != value)
                {
                    _customTroopCount = value;
                    OnCustomTroopCountUpdated?.Invoke();
                }
            }
        }

        public int TroopCount
        {
            get
            {
                return _troopCount;
            }
            set
            {
                if (_troopCount != value)
                {
                    _troopCount = value;
                    OnTroopCountUpdated?.Invoke();
                }
            }
        }

        public int FormationSelected
        {
            get
            {
                return _formationSelected;
            }
            set
            {
                if (_formationSelected != value)
                {
                    _formationSelected = value;
                    OnFormationSelected?.Invoke();
                }
            }
        }

        public int DifficultyLevel
        {
            get
            {
                return _difficultyLevel;
            }
            set
            {
                if (_difficultyLevel != value)
                {
                    _difficultyLevel = value;
                    OnDifficultyUpdated?.Invoke();
                }
            }
        }

        public BasicCultureObject SelectedFaction
        {
            get
            {
                return _selectedFaction;
            }
            set
            {
                if (_selectedFaction != value)
                {
                    _selectedFaction = value;
                    BasicCharacterObject factionDefaultCharacter = MultiplayerClassDivisions.GetMPHeroClasses(_selectedFaction).FirstOrDefault()?.TroopCharacter;
                    factionDefaultCharacter ??= MultiplayerClassDivisions.GetMPHeroClasses().First().TroopCharacter;
                    SelectedTroop = factionDefaultCharacter;
                    OnFactionSelected?.Invoke();
                    Log($"Selected faction = {value.Name}");
                }
            }
        }

        public Team SelectedTeam
        {
            get
            {
                return _selectedTeam;
            }
            set
            {
                if (_selectedTeam != value)
                {
                    _selectedTeam = value;
                    BannerCode = BannerCode.CreateFrom(value.Banner);
                    OnFactionSelected?.Invoke();
                }
            }
        }

        public BannerCode BannerCode;

        public BasicCharacterObject SelectedTroop
        {
            get
            {
                return _selectedTroop;
            }
            set
            {
                if (_selectedTroop != value)
                {
                    _selectedTroop = value;
                    OnTroopSelected?.Invoke();
                }
            }
        }

        public List<int> SelectedPerks
        {
            get
            {
                return _selectedPerks;
            }
            set
            {
                if (_selectedPerks != value)
                {
                    _selectedPerks = value;
                    OnPerkSelected?.Invoke();
                }
            }
        }

        private static readonly SpawnTroopsModel instance = new();
        public static SpawnTroopsModel Instance { get { return instance; } }


        private SpawnTroopsModel()
        {
            BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            _selectedFaction = GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team?.Side == BattleSideEnum.Attacker ? culture1 : culture2;
            _selectedTroop = MultiplayerClassDivisions.GetMPHeroClasses(_selectedFaction).First().TroopCharacter;
            _selectedTeam = GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team;
            DifficultyLevel = 1;
        }

        public void RefreshFormations()
        {
            OnFormationUpdated?.Invoke();
        }

        public void RefreshTroopSpawn(BasicCharacterObject troop, int troopCount)
        {
            OnTroopSpawned?.Invoke(new TroopSpawnedEventArgs(troop, troopCount));
        }
    }

    public class TroopSpawnedEventArgs : EventArgs
    {
        public BasicCharacterObject Troop { get; }
        public int TroopCount { get; }

        public TroopSpawnedEventArgs(BasicCharacterObject troop, int troopCount)
        {
            Troop = troop;
            TroopCount = troopCount;
        }
    }
}