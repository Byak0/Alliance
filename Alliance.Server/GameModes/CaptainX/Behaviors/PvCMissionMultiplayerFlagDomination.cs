using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

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
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
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
            if (affectedAgent.MissionPeer != null && affectedAgent.Formation != null)
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

        public override void AfterStart()
        {
            base.AfterStart();
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner banner = new Banner(@object.BannerKey, @object.BackgroundColor1, @object.ForegroundColor1);
            Banner banner2 = new Banner(object2.BannerKey, object2.BackgroundColor2, object2.ForegroundColor2);
            //for(int i = 0; i < 10; i++)
            //{
            //    Mission.Teams.Add(BattleSideEnum.Attacker, @object.BackgroundColor1, @object.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true);
            //    Mission.Teams.Add(BattleSideEnum.Defender, object2.BackgroundColor2, object2.ForegroundColor2, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
            //}            
        }
    }
}