using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Extensions.TroopSpawner.Models
{
    public sealed class SpawnTroopsModel
    {
        public int TroopCount = 1;
        public int CustomTroopCount = 0;
        public int TroopCountButtonSelected = 0;
        public int FormationSelected = 1;
        public bool SpawnTroopOnCursor = true;
        public float Difficulty
        {
            get
            {
                return _difficulty;
            }
            set
            {
                if (_difficulty != value)
                {
                    _difficulty = value;
                    OnDifficultyUpdated?.Invoke(this, EventArgs.Empty);
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
                    SelectedTroop = MultiplayerClassDivisions.GetMPHeroClasses(_selectedFaction).First().TroopCharacter;
                    OnFactionSelected?.Invoke(this, EventArgs.Empty);
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
                    OnTroopSelected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private BasicCultureObject _selectedFaction;
        private BasicCharacterObject _selectedTroop;
        private float _difficulty;

        public event EventHandler OnFactionSelected;
        public event EventHandler OnTroopSelected;
        public event EventHandler OnFormationUpdated;
        public event EventHandler OnDifficultyUpdated;
        public event EventHandler<TroopSpawnedEventArgs> OnTroopSpawned;

        public void RefreshFormations()
        {
            if (OnFormationUpdated != null) OnFormationUpdated.Invoke(this, EventArgs.Empty);
        }

        public void RefreshTroopSpawn()
        {
            if (OnTroopSpawned == null) return;

            MBReadOnlyList<BasicCharacterObject> troops = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();
            foreach (BasicCharacterObject troop in troops)
            {
                OnTroopSpawned.Invoke(this, new TroopSpawnedEventArgs(troop, 0));
            }
        }

        public void RefreshTroopSpawn(BasicCharacterObject troop, int troopCount)
        {
            if (OnTroopSpawned != null) OnTroopSpawned.Invoke(this, new TroopSpawnedEventArgs(troop, troopCount));
        }

        // Singleton
        private static readonly SpawnTroopsModel instance = new();
        public static SpawnTroopsModel Instance { get { return instance; } }

        static SpawnTroopsModel()
        {
            BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            instance._selectedFaction = GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team?.Side == BattleSideEnum.Attacker ? culture1 : culture2;
            instance._selectedTroop = MultiplayerClassDivisions.GetMPHeroClasses(instance._selectedFaction).First().TroopCharacter;
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