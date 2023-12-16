using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Extensions.TroopSpawner.Models
{
    public sealed class SpawnTroopsModel
    {
        public event Action OnFactionSelected;
        public event Action OnTroopSelected;
        public event Action OnTroopCountUpdated;
        public event Action OnFormationUpdated;
        public event Action OnDifficultyUpdated;
        public event Action<TroopSpawnedEventArgs> OnTroopSpawned;

        public int CustomTroopCount = 100;
        public int TroopCountButtonSelected = 0;
        public int FormationSelected = 1;
        public bool SpawnTroopOnCursor = true;

        private int _troopCount = 1;
        private BasicCultureObject _selectedFaction;
        private BasicCharacterObject _selectedTroop;
        private float _difficulty;
        private int _difficultyLevel;

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
                    switch (value)
                    {
                        case 0:
                            Difficulty = 0.5f; break;
                        case 1:
                            Difficulty = 1f; break;
                        case 2:
                            Difficulty = 1.5f; break;
                        case 3:
                            Difficulty = 2f; break;
                        case 4:
                            Difficulty = 2.5f; break;
                    }
                    OnDifficultyUpdated?.Invoke();
                }
            }
        }

        public float Difficulty
        {
            get
            {
                return _difficulty;
            }
            private set
            {
                _difficulty = value;
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
                    SelectedTroop = MultiplayerClassDivisions.GetMPHeroClasses(_selectedFaction).First().TroopCharacter;
                    OnFactionSelected?.Invoke();
                }
            }
        }

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

        public void RefreshFormations()
        {
            OnFormationUpdated?.Invoke();
        }

        public void RefreshTroopSpawn(BasicCharacterObject troop, int troopCount)
        {
            OnTroopSpawned?.Invoke(new TroopSpawnedEventArgs(troop, troopCount));
        }

        // Singleton
        private static readonly SpawnTroopsModel instance = new();
        public static SpawnTroopsModel Instance { get { return instance; } }

        private SpawnTroopsModel()
        {
            BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            _selectedFaction = GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team?.Side == BattleSideEnum.Attacker ? culture1 : culture2;
            _selectedTroop = MultiplayerClassDivisions.GetMPHeroClasses(_selectedFaction).First().TroopCharacter;
            DifficultyLevel = 1;
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