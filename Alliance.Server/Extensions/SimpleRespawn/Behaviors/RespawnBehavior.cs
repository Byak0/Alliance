using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SimpleRespawn.Behaviors
{
    /// <summary>
    /// MissionBehavior used to spawn/respawn players joining during a round.
    /// </summary>
    public class RespawnBehavior : MissionNetwork, IMissionBehavior
    {
        private Dictionary<MissionPeer, BasicCharacterObject> _playersPreviousCharacter;
        private MultiplayerRoundController _roundController;
        private float _lastSpawnCheck;

        public RespawnBehavior() : base()
        {
            _playersPreviousCharacter = new Dictionary<MissionPeer, BasicCharacterObject>();
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
        }

        public override void OnRemoveBehavior()
        {
            _roundController.OnPostRoundEnded -= OnPostRoundEnd;
            base.OnRemoveBehavior();
        }

        public override void AfterStart()
        {
            _roundController.OnPostRoundEnded += OnPostRoundEnd;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_roundController.IsRoundInProgress && (Config.Instance.AllowSpawnInRound || GetFreeRespawnTime() > 0))
            {
                _lastSpawnCheck += dt;
                if (_lastSpawnCheck >= 10)
                {
                    _lastSpawnCheck = 0;
                    RespawnPlayersInRound();
                }
            }
        }

        /// <summary>
        /// Respawn players during a round, depending on the server configuration.
        /// AllowSpawnInRound : Allow players to join an ongoing round.
        /// FreeRespawnTimer : Grants free respawn for a limited time at the beginning of a round.
        /// </summary>
        private void RespawnPlayersInRound()
        {
            int freeRespawnTime = GetFreeRespawnTime();
            bool freeRespawnEnable = freeRespawnTime > 0;
            for (int i = 0; i < GameNetwork.NetworkPeers.Count(); i++)
            {
                NetworkCommunicator peer = GameNetwork.NetworkPeers.ElementAt(i);
                MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

                if (!CanPlayerBeRespawned(missionPeer, freeRespawnEnable))
                {
                    continue;
                }
                if (peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
                {
                    SpawnPlayer(peer, missionPeer, GetCharacterOfPeer(missionPeer));
                }
            }
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

        private bool CanPlayerBeRespawned(MissionPeer missionPeer, bool freeRespawnEnable)
        {
            if (missionPeer == null || missionPeer.ControlledAgent != null)
            {
                return false;
            }

            if (_playersPreviousCharacter.ContainsKey(missionPeer) && !freeRespawnEnable)
            {
                return false;
            }

            return true;
        }

        private int GetFreeRespawnTime()
        {
            return (int)Math.Max(0, Config.Instance.FreeRespawnTimer + _roundController.RemainingRoundTime - MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue());
        }

        public void SpawnPlayer(NetworkCommunicator peer, MissionPeer missionPeer, BasicCharacterObject basicCharacterObject = null)
        {
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(missionPeer);
            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);

            if (basicCharacterObject == null)
            {
                // If player is officer, spawn hero instead of standard troop
                if (peer.IsOfficer())
                {
                    basicCharacterObject = mPHeroClassForPeer.HeroCharacter;
                }
                else
                {
                    basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
                }
            }

            SpawnHelper.SpawnPlayer(peer, onSpawnPerkHandler, basicCharacterObject);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.MissionPeer != null)
            {
                // Add player to the spawn list and store its character
                _playersPreviousCharacter[agent.MissionPeer] = agent.Character;
            }
        }

        private void OnPostRoundEnd()
        {
            _playersPreviousCharacter.Clear();
        }
    }
}