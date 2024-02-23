using Alliance.Common.Core.Configuration.Models;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.CaptainX.Behaviors
{
    public class PvCMissionMultiplayerFlagDomination : MissionMultiplayerFlagDomination
    {
        public PvCMissionMultiplayerFlagDomination(MultiplayerGameType gameType) : base(gameType)
        {
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            // Remove flag removal mechanic in case there is no flags to prevent crash
            FieldInfo pointRemovalTimeInSeconds = typeof(MissionMultiplayerFlagDomination).GetField("_pointRemovalTimeInSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            if (AllCapturePoints.Count == 0)
            {
                pointRemovalTimeInSeconds.SetValue(this, 4000f);
            }
            else
            {
                pointRemovalTimeInSeconds.SetValue(this, Config.Instance.TimeBeforeFlagRemoval);
            }

            FieldInfo moraleMultiplierForEachFlag = typeof(MissionMultiplayerFlagDomination).GetField("_moraleMultiplierForEachFlag", BindingFlags.Instance | BindingFlags.NonPublic);
            moraleMultiplierForEachFlag.SetValue(this, Config.Instance.MoraleMultiplierForFlag);

            FieldInfo moraleMultiplierOnLastFlag = typeof(MissionMultiplayerFlagDomination).GetField("_moraleMultiplierOnLastFlag", BindingFlags.Instance | BindingFlags.NonPublic);
            moraleMultiplierOnLastFlag.SetValue(this, Config.Instance.MoraleMultiplierForLastFlag);
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (MultiplayerOptions.OptionType.GameType.GetStrValue() != "CaptainX") return;
            if (agent.MissionPeer != null)
            {
                Debug.Print("1 agent.Formation = " + agent.Formation, 0, Debug.DebugColor.Blue);
                if (agent.Formation != null)
                {
                    Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + agent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
                    MissionPeer temp = agent.MissionPeer;
                    agent.MissionPeer = null;
                    agent.Formation.OnUndetachableNonPlayerUnitAdded(agent);
                    agent.MissionPeer = temp;
                    Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + agent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
                }
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (MultiplayerOptions.OptionType.GameType.GetStrValue() != "CaptainX") return;
            if (agent.MissionPeer != null)
            {
                Debug.Print("2 agent.Formation = " + agent.Formation, 0, Debug.DebugColor.Blue);
                if (agent.Formation != null)
                {
                    Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + agent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
                    MissionPeer temp = agent.MissionPeer;
                    agent.MissionPeer = null;
                    agent.Formation.OnUndetachableNonPlayerUnitAdded(agent);
                    agent.MissionPeer = temp;
                    Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + agent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CaptainX" && affectedAgent.MissionPeer != null && affectedAgent.Formation != null)
            {
                Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + affectedAgent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
                MissionPeer temp = affectedAgent.MissionPeer;
                affectedAgent.MissionPeer = null;
                affectedAgent.Formation.OnUndetachableNonPlayerUnitRemoved(affectedAgent);
                affectedAgent.MissionPeer = temp;
                Debug.Print("affectedAgent.Formation.CountOfUndetachableNonPlayerUnits = " + affectedAgent.Formation.CountOfUndetachableNonPlayerUnits, 0, Debug.DebugColor.Blue);
            }
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }
    }
}