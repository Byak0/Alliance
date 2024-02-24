using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.PvC.Behaviors
{
    public class PvCSpawnFrameBehavior : SpawnFrameBehaviorBase, ISpawnFrameBehavior
    {
        public override void Initialize()
        {
            base.Initialize();

            isSiegeBattle = MultiplayerOptions.OptionType.Map.GetStrValue().Contains("siege");

            _spawnPointsByTeam = new IEnumerable<GameEntity>[2];
            _spawnZonesByTeam = new IEnumerable<GameEntity>[2];
            _spawnPointsByTeam[1] = SpawnPoints.Where((x) => x.HasTag("attacker")).ToList();
            _spawnPointsByTeam[0] = SpawnPoints.Where((x) => x.HasTag("defender")).ToList();
            _spawnZonesByTeam[1] = (from sz in _spawnPointsByTeam[1].Select((sp) => sp.Parent).Distinct()
                                    where sz != null
                                    select sz).ToList();
            _spawnZonesByTeam[0] = (from sz in _spawnPointsByTeam[0].Select((sp) => sp.Parent).Distinct()
                                    where sz != null
                                    select sz).ToList();
            _activeSpawnZoneIndex = 0;
        }

        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            if (isSiegeBattle)
            {
                List<GameEntity> list = new List<GameEntity>();
                GameEntity gameEntity = _spawnZonesByTeam[(int)team.Side].First((sz) => sz.HasTag(string.Format("{0}{1}", "sp_zone_", _activeSpawnZoneIndex)));
                list.AddRange(from sp in gameEntity.GetChildren()
                              where sp.HasTag("spawnpoint")
                              select sp);
                return GetSpawnFrameFromSpawnPoints(list, team, hasMount);
            }
            else
            {
                GameEntity bestZone = GetBestZone(team, isInitialSpawn);
                List<GameEntity> list;
                if (bestZone != null)
                {
                    list = _spawnPointsByTeam[(int)team.Side].Where((sp) => sp.Parent == bestZone).ToList();
                }
                else
                {
                    list = _spawnPointsByTeam[(int)team.Side].ToList();
                }
                return GetBestSpawnPoint(list, hasMount);
            }
        }

        public void OnFlagDeactivated(FlagCapturePoint flag)
        {
            _activeSpawnZoneIndex++;
        }

        private GameEntity GetBestZone(Team team, bool isInitialSpawn)
        {
            if (!_spawnZonesByTeam[(int)team.Side].Any())
            {
                return null;
            }
            if (isInitialSpawn)
            {
                return _spawnZonesByTeam[(int)team.Side].Single((sz) => sz.HasTag("starting"));
            }
            List<GameEntity> list = _spawnZonesByTeam[(int)team.Side].Where((sz) => !sz.HasTag("starting")).ToList();
            if (!list.Any())
            {
                return null;
            }
            float[] array = new float[list.Count];
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                if ((component != null ? component.Team : null) != null && component.Team.Side != BattleSideEnum.None && component.ControlledAgent != null && component.ControlledAgent.IsActive())
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Vec3 globalPosition = list[i].GlobalPosition;
                        if (component.Team != team)
                        {
                            array[i] -= 1f / (0.0001f + component.ControlledAgent.Position.Distance(globalPosition)) * 1f;
                        }
                        else
                        {
                            array[i] += 1f / (0.0001f + component.ControlledAgent.Position.Distance(globalPosition)) * 1.5f;
                        }
                    }
                }
            }
            int num = -1;
            for (int j = 0; j < array.Length; j++)
            {
                if (num < 0 || array[j] > array[num])
                {
                    num = j;
                }
            }
            return list[num];
        }

        private MatrixFrame GetBestSpawnPoint(List<GameEntity> spawnPointList, bool hasMount)
        {
            float bestSpawnScore = float.MinValue;
            int bestSpawnIndex = -1;
            MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;
            for (int i = 0; i < spawnPointList.Count; i++)
            {
                float spawnScore = MBRandom.RandomFloat * 0.2f;
                foreach (Agent agent in Mission.Current.GetNearbyAgents(spawnPointList[i].GlobalPosition.AsVec2, 2f, agents))
                {
                    float lengthSquared = (agent.Position - spawnPointList[i].GlobalPosition).LengthSquared;
                    if (lengthSquared < 4f)
                    {
                        float length = MathF.Sqrt(lengthSquared);
                        spawnScore -= (2f - length) * 5f;
                    }
                }
                if (hasMount && spawnPointList[i].HasTag("exclude_mounted"))
                {
                    spawnScore -= 100f;
                }
                if (spawnScore > bestSpawnScore)
                {
                    bestSpawnScore = spawnScore;
                    bestSpawnIndex = i;
                }
            }
            MatrixFrame globalFrame = spawnPointList[bestSpawnIndex].GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return globalFrame;
        }

        // Alternative method used to find spawn frame in PvC. Return the closest spawn frame from a specified position.
        public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos)
        {
            if (isSiegeBattle)
            {
                List<GameEntity> list = new List<GameEntity>();
                GameEntity gameEntity = _spawnZonesByTeam[(int)team.Side].First((sz) => sz.HasTag(string.Format("{0}{1}", "sp_zone_", _activeSpawnZoneIndex)));
                list.AddRange(from sp in gameEntity.GetChildren()
                              where sp.HasTag("spawnpoint")
                              select sp);
                return GetSpawnFrameFromSpawnPoints(list, team, hasMount);
            }
            else
            {
                GameEntity bestZone = GetBestZone(team, isInitialSpawn);
                List<GameEntity> list;
                if (bestZone != null)
                {
                    list = _spawnPointsByTeam[(int)team.Side].Where((sp) => sp.Parent == bestZone).ToList();
                }
                else
                {
                    list = _spawnPointsByTeam[(int)team.Side].ToList();
                }
                return GetBestClosestSpawnFrame(list, hasMount, spawnPos);
            }
        }

        private MatrixFrame GetBestClosestSpawnFrame(List<GameEntity> spawnPointList, bool hasMount, MatrixFrame spawnPos)
        {
            float bestSpawnScore = float.MinValue;
            int bestSpawnIndex = -1;
            int nbAgentsAtBest = 0;
            MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;
            for (int i = 0; i < spawnPointList.Count; i++)
            {
                float spawnScore = 0;
                // The farther away the spawnPoint is from spawnPos, the lesser it scores
                spawnScore -= Math.Abs((spawnPos.origin - spawnPointList[i].GlobalPosition).Length);
                // The more agents are already close to the spawn, the less it scores
                int nbAgents = Mission.Current.GetNearbyAgents(spawnPointList[i].GlobalPosition.AsVec2, 5f, agents).Count();
                spawnScore -= nbAgents;

                if (hasMount && spawnPointList[i].HasTag("exclude_mounted"))
                {
                    spawnScore -= 100f;
                }
                if (spawnScore > bestSpawnScore)
                {
                    nbAgentsAtBest = nbAgents;
                    bestSpawnScore = spawnScore;
                    bestSpawnIndex = i;
                }
            }
            MatrixFrame globalFrame = spawnPointList[bestSpawnIndex].GetGlobalFrame();
            if (nbAgentsAtBest > 30)
            {
                Debug.Print("Too much agents already spawned at this position, spawning slightly to the side.", 0, Debug.DebugColor.Yellow);
                globalFrame.Advance(MBRandom.RandomFloatRanged(-10f, 10f));
                globalFrame.Strafe(MBRandom.RandomFloatRanged(-10f, 10f));
            }
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return globalFrame;
        }

        public PvCSpawnFrameBehavior()
        {
        }

        private IEnumerable<GameEntity>[] _spawnPointsByTeam;

        private IEnumerable<GameEntity>[] _spawnZonesByTeam;

        private int _activeSpawnZoneIndex;

        private bool isSiegeBattle;
    }
}
