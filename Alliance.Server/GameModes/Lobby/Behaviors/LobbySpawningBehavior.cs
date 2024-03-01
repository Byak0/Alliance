using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
    /// <summary>
    /// Simple spawn behavior for the Lobby.    
    /// </summary>
    public class LobbySpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
    {
        private float _lastSpawnCheck;
        private List<(EquipmentIndex, EquipmentElement)> _altEquipment;
        private static Random _random = new Random();
        private static List<float> _values = new List<float> { 0.5f, 1f, 1.5f, 2f, 2.5f };

        public LobbySpawningBehavior()
        {
            // Alternate equipment for heroes
            EquipmentElement horse = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("mp_vlandia_horse"), null, null, false);
            EquipmentElement harness = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("mp_stripped_leather_harness"), null, null, false);
            _altEquipment = new List<(EquipmentIndex, EquipmentElement)>
            {
                //(EquipmentIndex.Horse, horse),
                //(EquipmentIndex.HorseHarness, harness)
            };
        }

        public override void OnTick(float dt)
        {
            _lastSpawnCheck += dt;
            if (_lastSpawnCheck >= 2)
            {
                _lastSpawnCheck = 0;
                SpawnAgents();
            }
        }

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));

            // Spawn max 20 players at once
            int playersSpawn = 0;
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

                if (missionPeer == null || missionPeer.ControlledAgent != null)
                {
                    continue;
                }
                if (peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
                {
                    BasicCharacterObject basicCharacterObject;
                    MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer;

                    bool useAltEquipement = false;

                    if (peer.IsCommander() || peer.IsOfficer())
                    {
                        mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClasses(culture).GetRandomElementInefficiently();

                        basicCharacterObject = mPHeroClassForPeer.HeroCharacter;
                        useAltEquipement = true;
                    }
                    else
                    {
                        mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClasses(culture).GetRandomElementInefficiently();
                        basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
                    }

                    MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(mPHeroClassForPeer.GetAllAvailablePerksForListIndex(0));
                    SpawnHelper.SpawnPlayer(peer, onSpawnPerkHandler, basicCharacterObject, alternativeEquipment: useAltEquipement ? _altEquipment : null, customCulture: culture);
                    playersSpawn++;
                }
                if (playersSpawn >= 20) break;
            }

            // Spawn bots
            int nbBotsToSpawnAtt = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            while (Mission.AttackerTeam.ActiveAgents.Count() < nbBotsToSpawnAtt)
            {
                BasicCharacterObject troopCharacter;
                troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList().GetRandomElement().TroopCharacter;

                // Random difficulty between 0.5 and 2.5f
                float difficulty = _values[_random.Next(_values.Count)];
                SpawnHelper.SpawnBot(Mission.AttackerTeam, culture, troopCharacter, botDifficulty: difficulty);
            }
        }

        protected override bool IsRoundInProgress()
        {
            return Mission.Current.CurrentState == Mission.State.Continuing;
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
        {
            return true;
        }

        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }
    }
}
