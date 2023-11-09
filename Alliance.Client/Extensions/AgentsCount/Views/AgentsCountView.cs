using Alliance.Client.Extensions.AgentsCount.ViewModels;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

namespace Alliance.Client.Extensions.AgentsCount.Views
{
    public class AgentsCountView : MissionView
    {
        private GauntletLayer _layer;
        private AgentsCountVM _dataSource;

        public AgentsCountView()
        {
        }

        public override void EarlyStart()
        {
            ViewOrderPriority = 20;
            _dataSource = new AgentsCountVM();
            _layer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _layer.LoadMovie("AgentsCountHUD", _dataSource);
            MissionScreen.AddLayer(_layer);
        }

        public override void OnMissionScreenFinalize()
        {
            MissionScreen.RemoveLayer(_layer);
            _layer = null;
            _dataSource?.OnFinalize();
            _dataSource = null;
        }

        // Calculated by client
        public void RefreshAgentsAlive()
        {
            if (GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.Team?.ActiveAgents?.Count != null)
            {
                _dataSource.AgentsAlive = GameNetwork.MyPeer.GetComponent<MissionPeer>().Team.ActiveAgents.Count;
            }
        }

        // Calculated by server, called by HandleServerEventAgentsCountMessage to get correct total kill count
        public void RefreshAgentsDead(int nbDead)
        {
            _dataSource.AgentsDead = nbDead;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            RefreshAgentsAlive();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            RefreshAgentsAlive();
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (Input.IsGameKeyDown(5))
            {
                _dataSource.IsVisible = true;
            }
            else
            {
                _dataSource.IsVisible = false;
            }
        }
    }
}