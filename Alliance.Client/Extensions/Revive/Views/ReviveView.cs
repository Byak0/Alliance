using Alliance.Client.Extensions.Revive.ViewModels;
using Alliance.Common.Extensions.Revive.Behaviors;
using Alliance.Common.Extensions.Revive.Models;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.Revive.Views
{
    /// <summary>
    /// Show wounded allies. 
    /// Lock camera when wounded.
    /// </summary>
    public class ReviveView : MissionView
    {
        private WoundedAgentInfos _playerWoundedInfos;
        private ReviveBehavior _reviveBehavior;
        private MissionCameraFadeView _cameraFadeView;
        private bool _lockCamera;
        private Camera _camera;
        private MatrixFrame _cameraFrame = MatrixFrame.Identity;
        private List<Vec3> _cameraPositions = new List<Vec3>()
        {
            new Vec3(0.38f, 0.25f, 1.1f), // View from pilot
            new Vec3(-5.45f, 0.0f, 5.20f), // View from behind and some height
            new Vec3(-1f, -0.25f, 2.25f), // View from behind
            new Vec3(1.5f, 0.5f, 1f), // View from front right wheel
            new Vec3(0f, 0f, 0f)
        };
        private float rotationDeltaX;
        private float rotationDeltaY;
        private GauntletLayer _gauntletLayer;
        private ReviveVM _dataSource;
        private WoundedAgentInfos _targetWoundedAgent;

        public ReviveView() { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _reviveBehavior = Mission.Current.GetMissionBehavior<ReviveBehavior>();
            _cameraFadeView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();

            _dataSource = new ReviveVM(MissionScreen.CombatCamera);
            _reviveBehavior.OnNewWounded += OnNewWounded;

            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("ReviveHUD", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _reviveBehavior.OnNewWounded -= OnNewWounded;
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Input.IsGameKeyDown(5))
            {
                _dataSource.IsEnabled = true;
            }
            else
            {
                _dataSource.IsEnabled = false;
            }
            _dataSource.Tick(dt);

            // Lock camera as long as player is wounded
            if (_lockCamera)
            {
                if (_playerWoundedInfos.WoundedAgentEntity != null && (Agent.Main == null || Agent.Main.Health <= 0))
                {
                    UpdateCamera(dt);
                }
                else
                {
                    ReleaseCamera();
                }
            }
            else if (Agent.Main != null)
            {
                WoundedAgentInfos nearbyWoundedAgent = _reviveBehavior.WoundedAgents.Find(wai => wai.WoundedAgentEntity.GlobalPosition.Distance(Agent.Main.Position) < 1f);

                if (_targetWoundedAgent != null && _targetWoundedAgent != nearbyWoundedAgent)
                {
                    _dataSource.InteractionInterface.IsActive = false;
                    Log($"Disabled interaction - {_dataSource.InteractionInterface.PrimaryInteractionMessage}", LogLevel.Debug);
                    _targetWoundedAgent.WoundedAgentEntity.SetContourColor(null, false);
                }
                else if (nearbyWoundedAgent != null)
                {
                    uint color = nearbyWoundedAgent.Side == Agent.Main.Team.Side ? Colors.Green.ToUnsignedInteger() : Colors.Red.ToUnsignedInteger();
                    nearbyWoundedAgent.WoundedAgentEntity.SetContourColor(color, true);
                    _dataSource.InteractionInterface.IsActive = true;
                    TextObject textObject = new TextObject("Press {KEY} to rescue " + nearbyWoundedAgent.Name);
                    textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                    _dataSource.InteractionInterface.PrimaryInteractionMessage = textObject.ToString();
                    Log($"Activated interaction - {_dataSource.InteractionInterface.PrimaryInteractionMessage}", LogLevel.Debug);
                }
                _targetWoundedAgent = nearbyWoundedAgent;

                Log("nb : " + _reviveBehavior.WoundedAgents.Count, LogLevel.Debug);
            }

            if (_targetWoundedAgent != null && Input.IsKeyReleased(InputKey.F))
            {
                _reviveBehavior.RescueAgent(_targetWoundedAgent);
                _targetWoundedAgent = null;
            }
        }

        public void OnNewWounded(WoundedAgentInfos woundedAgentInfos)
        {
            if (woundedAgentInfos.Player == GameNetwork.MyPeer)
            {
                _playerWoundedInfos = woundedAgentInfos;
                _cameraFadeView.BeginFadeOutAndIn(3, 1, 2);
                _lockCamera = true;
                SetupCamera();
            }
            else
            {
                _dataSource.OnNewWounded(woundedAgentInfos);
            }
        }

        private void SetupCamera()
        {
            _camera = Camera.CreateCamera();
            Camera combatCamera = MissionScreen.CombatCamera;
            if (combatCamera != null)
            {
                _camera.FillParametersFrom(combatCamera);
            }
            else
            {
                Log("ReviveView - Combat camera is null.", LogLevel.Error);
            }
            MatrixFrame newCameraFrame = _playerWoundedInfos.WoundedAgentEntity.GetGlobalFrame();
            SetCameraFrame(newCameraFrame, out _cameraFrame);
            _camera.Frame = _cameraFrame;
            MissionScreen.CustomCamera = _camera;
            _lockCamera = true;
        }

        private void SetCameraFrame(MatrixFrame newFrame, out MatrixFrame cameraFrame)
        {
            cameraFrame.origin = newFrame.origin;
            cameraFrame.rotation.s = newFrame.rotation.s;
            cameraFrame.rotation.f = newFrame.rotation.u;
            cameraFrame.rotation.u = -newFrame.rotation.f;
            cameraFrame.rotation.RotateAboutSide(rotationDeltaY);
            cameraFrame.rotation.RotateAboutForward(rotationDeltaX);
            cameraFrame.rotation.Orthonormalize();
        }

        private void UpdateCamera(float dt)
        {
            Vec3 _cameraPosition = _cameraPositions[1];
            MatrixFrame newCameraFrame = _playerWoundedInfos.WoundedAgentEntity.GetGlobalFrame().Advance(_cameraPosition.x).Strafe(_cameraPosition.y).Elevate(_cameraPosition.z);
            SetCameraFrame(newCameraFrame, out _cameraFrame);
            HandleBasicCameraMovement(dt, ref _cameraFrame);
            _camera.Frame = _cameraFrame;
        }

        private void ReleaseCamera()
        {
            _lockCamera = false;
            MissionScreen.UpdateFreeCamera(MissionScreen.CustomCamera.Frame);
            MissionScreen.CustomCamera = null;
            _camera.ReleaseCamera();
        }

        private void HandleBasicCameraMovement(float dt, ref MatrixFrame cameraFrame)
        {
            // Get mouse sensitivity
            float mouseSensitivity = 0.2f * Input.GetMouseSensitivity();

            // Check if the mouse is active and calculate changes in rotation
            if (!MissionScreen.MouseVisible && !Input.IsKeyDown(InputKey.RightMouseButton))
            {
                rotationDeltaX += mouseSensitivity * -dt * Input.GetMouseMoveX();
                rotationDeltaY += mouseSensitivity * -dt * Input.GetMouseMoveY();
            }
        }
    }
}
