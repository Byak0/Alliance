using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.ExNativeUI.SpectatorView.Views
{
    public class SpectatorView : MissionView
    {
        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
        }

        public override void AfterStart()
        {
            for (int i = 0; i < 10; i++)
            {
                _spectateCamerFrames.Add(MatrixFrame.Identity);
            }
            for (int j = 0; j < 10; j++)
            {
                string text = "spectate_cam_" + j.ToString();
                List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag(text).ToList();
                if (list.Count > 0)
                {
                    _spectateCamerFrames[j] = list[0].GetGlobalFrame();
                }
            }

            MissionScreen.SetCustomAgentListToSpectateGatherer(AgentListToSpectateGatherer);
        }

        private List<Agent> AgentListToSpectateGatherer(Agent forcedAgentToInclude)
        {
            List<Agent> agentList = new List<Agent>();

            if (GameNetwork.MyPeer.IsCommander())
            {
                MissionPeer myMissionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                List<FormationClass> controlledForms = FormationControlModel.Instance.GetControlledFormations(myMissionPeer);
                agentList = Mission.AllAgents.Where((x) => x.Team == myMissionPeer.Team && x.Formation != null && controlledForms.Contains(x.Formation.FormationIndex) && x.MissionPeer == null && x.IsCameraAttachable()).ToList();
            }
            else
            {
                agentList = Mission.AllAgents.Where((x) => x.Team == Mission.PlayerTeam && x.MissionPeer != null && x.IsCameraAttachable()).ToList();
            }

            if (agentList.Count == 0)
            {
                agentList = Mission.AllAgents.Where((x) => x.IsCameraAttachable()).ToList();
            }

            return agentList;
        }

        private List<MatrixFrame> _spectateCamerFrames = new List<MatrixFrame>();
    }
}
