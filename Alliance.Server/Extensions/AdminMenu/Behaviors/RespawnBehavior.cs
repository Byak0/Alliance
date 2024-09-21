using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Server.Extensions.AdminMenu.Behaviors
{
    /// <summary>
    /// MissionBehavior used to spawn/respawn players joining during a round.
    /// </summary>
    public class RespawnBehavior : MissionNetwork, IMissionBehavior
    {
        private Dictionary<MissionPeer, BasicCharacterObject> _playersPreviousCharacter;
        private float _lastSpawnCheck;
        private BasicCharacterObject _defaultCharacter;
        private List<int> _defaultPerks;

        public RespawnBehavior() : base()
        {
            _playersPreviousCharacter = new Dictionary<MissionPeer, BasicCharacterObject>();
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _defaultCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_heavy_infantry_vlandia_troop");
            _defaultPerks = new List<int>() { 0, 0 };
        }

        public override void OnRemoveBehavior()
        {
            
            _playersPreviousCharacter.Clear();
            base.OnRemoveBehavior();
        }

        /// <summary>
        /// Respawn players during a round, depending on the server configuration.
        /// AllowSpawnInRound : Allow players to join an ongoing round.
        /// FreeRespawnTimer : Grants free respawn for a limited time at the beginning of a round.
        /// </summary>
        public void RespawnPlayer(NetworkCommunicator peer)
        {

               MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

               if (CanPlayerBeRespawned(missionPeer) && peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
                {
                    SpawnPlayer(peer, missionPeer, GetCharacterOfPeer(missionPeer));
                }

           // }
        }

        /// <summary>
        /// Return character last used by peer if they have same culture. Otherwise return null.
        /// </summary>
        private BasicCharacterObject GetCharacterOfPeer(MissionPeer missionPeer)
        {
            if (_playersPreviousCharacter.ContainsKey(missionPeer) && _playersPreviousCharacter[missionPeer].Culture == missionPeer.Culture)
            {
                return _playersPreviousCharacter[missionPeer];
            }
            else
            {
                return null;
            }
        }

        private bool CanPlayerBeRespawned(MissionPeer missionPeer)
        {
            if (missionPeer == null || missionPeer.ControlledAgent != null)
            {
                return false;
            }

            return true;
        }

        public void SpawnPlayer(NetworkCommunicator peer, MissionPeer missionPeer, BasicCharacterObject basicCharacterObject)
        {
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForCharacter(basicCharacterObject);
            MPOnSpawnPerkHandler perkHandler = GetOnSpawnPerkHandler(missionPeer);
            BasicCultureObject _cultureTeam;
            MPHeroClass _defaultMpClassTeam;

            if (basicCharacterObject != null)
                SpawnHelper.SpawnPlayer(peer, perkHandler, basicCharacterObject);
            else

            {
                if (missionPeer.Team == Mission.AttackerTeam)
                {
                    _cultureTeam = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
                    _defaultMpClassTeam = MultiplayerClassDivisions.GetMPHeroClasses(_cultureTeam).FirstOrDefault();

                    // If player is officer, spawn hero instead of standard troop
                    if (peer.IsOfficer())
                    {
                        SpawnHelper.SpawnPlayer(peer, perkHandler, _defaultMpClassTeam.HeroCharacter);
                    }
                    else
                    {
                        SpawnHelper.SpawnPlayer(peer, perkHandler, _defaultMpClassTeam.TroopCharacter);
                    }

                }
                else if (missionPeer.Team == Mission.DefenderTeam)
                {
                    _cultureTeam = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
                    _defaultMpClassTeam = MultiplayerClassDivisions.GetMPHeroClasses(_cultureTeam).FirstOrDefault();

                    SpawnHelper.SpawnPlayer(peer, perkHandler, _defaultMpClassTeam.HeroCharacter);

                    // If player is officer, spawn hero instead of standard troop
                    if (peer.IsOfficer())
                    {
                        SpawnHelper.SpawnPlayer(peer, perkHandler, _defaultMpClassTeam.HeroCharacter);
                    }
                    else
                    {
                        SpawnHelper.SpawnPlayer(peer, perkHandler, _defaultMpClassTeam.TroopCharacter);
                    }

                }

            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.MissionPeer != null)
            {
                // Add player to the spawn list and store its character
                _playersPreviousCharacter[agent.MissionPeer] = agent.Character;
            }
        }

        private void SendMessageToClient(NetworkCommunicator targetPeer, string message, AdminServerLog.ColorList color, bool forAdmin = false)
        {
            if (!forAdmin)
            {
                GameNetwork.BeginModuleEventAsServer(targetPeer);
                GameNetwork.WriteMessage(new AdminServerLog(message, color));
                GameNetwork.EndModuleEventAsServer();
            }

            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new AdminServerLog(message, color));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}