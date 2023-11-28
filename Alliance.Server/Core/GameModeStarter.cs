using Alliance.Common.Core.Configuration;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Lobby;
using NetworkMessages.FromServer;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Server.Core
{
    /// <summary>
    /// Start custom game modes. Inspired by mentalrob's ChatCommands.
    /// </summary>
    public class GameModeStarter
    {
        private static readonly GameModeStarter instance = new GameModeStarter();
        public static GameModeStarter Instance { get { return instance; } }

        public bool MissionIsRunning
        {
            get
            {
                return Mission.Current != null;
            }
        }
        public bool EndingCurrentMissionThenStartingNewMission;

        public void SyncMultiplayerOptionsToClients()
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new MultiplayerOptionsInitial());
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, null);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, null);
        }

        public void StartMission(GameModeSettings gameModeSettings)
        {
            if (!EndingCurrentMissionThenStartingNewMission)
            {
                if (!MissionIsRunning)
                {
                    StartMissionOnly(gameModeSettings);
                    return;
                }
                EndMissionThenStartMission(gameModeSettings);
            }
        }

        private void EndMissionThenStartMission(GameModeSettings gameModeSettings)
        {
            // Try to stop everyone from using objects to prevent crash
            Log("Scene=" + Mission.Current?.SceneName, LogLevel.Debug);
            Log("NB Agents=" + Mission.Current?.Agents.Count, LogLevel.Debug);
            foreach (MissionObject missionObj in Mission.Current?.MissionObjects)
            {
                if (missionObj is UsableMachine machine)
                {
                    Log($"Disabling {machine.GameEntity?.Name} - {machine.IsDisabled}", LogLevel.Debug);
                    machine.Disable();
                }
            }
            foreach (Agent agent in Mission.Current?.AllAgents)
            {
                agent.SetMortalityState(Agent.MortalityState.Invulnerable);
                //UsableMissionObject missionObject = agent.CurrentlyUsedGameObject;
                //if (missionObject != null)
                //{
                //    Log("agent using " + missionObject?.GameEntity?.Name, 0, Debug.DebugColor.Blue);
                //    agent.StopUsingGameObject();
                //    Log("agent now using " + agent.CurrentlyUsedGameObject?.GameEntity?.Name, 0, Debug.DebugColor.Blue);
                //    missionObject.SetDisabled();
                //}
                //agent.AIStateFlags = Agent.AIStateFlag.Alarmed;
                //agent.SetScriptedCombatFlags(Agent.AISpecialCombatModeFlags.None);
                //agent.DisableScriptedMovement();
                //agent.ClearTargetFrame();
                //agent.Detachment?.RemoveAgent(agent);
                Log($"{agent.Name} using {agent.CurrentlyUsedGameObject?.GameEntity?.Name} - flag : {agent.AIStateFlags}  | {agent.GetScriptedCombatFlags()}", LogLevel.Debug);
            }


            MissionListener missionListener = new MissionListener();
            Mission.Current.AddListener(missionListener);
            MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = false;
            MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = false;
            EndingCurrentMissionThenStartingNewMission = true;
            missionListener.SetGameModeSettings(gameModeSettings);
            DedicatedCustomServerSubModule.Instance.ServerSideIntermissionManager.EndMission();
        }

        public void ApplyGameModeSettings(GameModeSettings gameModeSettings)
        {
            ConfigManager.Instance.ApplyNativeOptions(gameModeSettings.NativeOptions);
            ConfigManager.Instance.ApplyModOptions(gameModeSettings.ModOptions);
            SyncMultiplayerOptionsToClients();
        }

        public bool StartMissionOnly(GameModeSettings gameModeSettings)
        {
            if (!MissionIsRunning)
            {
                ApplyGameModeSettings(gameModeSettings);
                DedicatedCustomServerSubModule.Instance.ServerSideIntermissionManager.StartMission();
                return true;
            }
            return false;
        }

        public bool EndMission()
        {
            if (MissionIsRunning)
            {
                DedicatedCustomServerSubModule.Instance.ServerSideIntermissionManager.StartMission();
            }
            return false;
        }

        public void StartLobby(string map, string culture1, string culture2, int nbBots = -1)
        {
            LobbyGameModeSettings lobby = new LobbyGameModeSettings();
            lobby.SetNativeOption(OptionType.Map, map);
            lobby.SetNativeOption(OptionType.CultureTeam1, culture1);
            lobby.SetNativeOption(OptionType.CultureTeam2, culture2);
            if (nbBots > -1) lobby.SetNativeOption(OptionType.NumberOfBotsTeam1, nbBots);
            StartMission(lobby);
        }

        public GameModeStarter()
        {
        }
    }

    public class MissionListener : IMissionListener
    {
        public void SetGameModeSettings(GameModeSettings gameModeSettings)
        {
            _gameModeSettings = gameModeSettings;
        }

        public void OnEndMission()
        {
            new Thread(new ParameterizedThreadStart(StartMissionThread.ThreadProc)).Start(_gameModeSettings);
            Mission.Current.RemoveListener(this);
        }

        public void OnInitialDeploymentPlanMade(BattleSideEnum battleSide, bool isFirstPlan)
        {
        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
        }

        public void OnResetMission()
        {
        }

        public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType)
        {
        }

        public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType)
        {
        }

        public void OnConversationCharacterChanged()
        {
        }

        public MissionListener()
        {
        }

        private GameModeSettings _gameModeSettings;
    }

    internal class StartMissionThread
    {
        public static void ThreadProc(object gameModeSettings)
        {
            Thread.Sleep(500);
            GameModeStarter.Instance.StartMissionOnly((GameModeSettings)gameModeSettings);
            GameModeStarter.Instance.EndingCurrentMissionThenStartingNewMission = false;
        }

        public StartMissionThread()
        {
        }
    }
}
