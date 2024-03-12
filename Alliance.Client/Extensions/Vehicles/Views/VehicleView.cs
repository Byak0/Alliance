using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Common.Extensions.Vehicles.Scripts;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

namespace Alliance.Client.Extensions.Vehicles.Views
{
    /// <summary>
    /// View responsible for the vehicle camera.
    /// </summary>
    [DefaultView]
    public class VehicleView : MissionView, IUseKeyBinder
    {
        private static string KeyCategoryId = "vehicle";
        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
        {
            CategoryId = KeyCategoryId,
            Category = "Alliance - Vehicle",
            Keys = new List<BindedKey>()
                {
                    new BindedKey()
                    {
                        Id = "key_vehicle_camera",
                        Description = "Switch view",
                        Name = "Switch view",
                        DefaultInputKey = InputKey.V,
                    },
                    new BindedKey()
                    {
                        Id = "key_vehicle_honk",
                        Description = "Tut tut",
                        Name = "Honk",
                        DefaultInputKey = InputKey.C,
                    },
                    new BindedKey()
                    {
                        Id = "key_vehicle_light",
                        Description = "Toggle lights.",
                        Name = "Lights",
                        DefaultInputKey = InputKey.X,
                    }
                }
        };

        public CS_Vehicle Vehicle;

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
        private int _currentCameraIndex = 0;
        private bool _updateCamera;


        private GameKey cameraKey;
        private GameKey honkKey;
        private GameKey lightKey;
        private float rotationDeltaX;
        private float rotationDeltaY;

        public VehicleView()
        {
        }

        public override void EarlyStart()
        {
            cameraKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_vehicle_camera");
            honkKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_vehicle_honk");
            lightKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_vehicle_light");
        }

        public override void AfterStart()
        {
            List<GameEntity> vehicles = Mission.GetActiveEntitiesWithScriptComponentOfType<CS_Vehicle>().ToMBList();
            foreach (GameEntity vehicle in vehicles)
            {
                CS_Vehicle vehicleScript = vehicle.GetFirstScriptOfType<CS_Vehicle>();
                vehicleScript.OnUseEvent += UseVehicle;
                vehicleScript.OnUseStoppedEvent += StopUsingVehicle;
            }
        }

        public override void OnMissionScreenInitialize()
        {
        }

        public override void OnMissionScreenFinalize()
        {
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            if (Vehicle != null)
            {
                if (_updateCamera)
                {
                    UpdateCamera(dt);
                }
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (Vehicle != null)
            {
                TickInputs();
            }
        }

        private void TickInputs()
        {
            if (Input.IsKeyPressed(cameraKey.KeyboardKey.InputKey) || Input.IsKeyPressed(cameraKey.ControllerKey.InputKey))
            {
                SwitchCameraView();
            }
            if (Input.IsKeyPressed(honkKey.KeyboardKey.InputKey) || Input.IsKeyPressed(honkKey.ControllerKey.InputKey))
            {
                Honk();
            }
            if (Input.IsKeyPressed(lightKey.KeyboardKey.InputKey) || Input.IsKeyPressed(lightKey.ControllerKey.InputKey))
            {
                ToggleLights();
            }
        }

        private void SwitchCameraView()
        {
            _currentCameraIndex++;
            if (_currentCameraIndex >= _cameraPositions.Count)
            {
                _currentCameraIndex = 0;
            }
        }

        public void UseVehicle(CS_Vehicle vehicle, Agent agent)
        {
            if (agent == Agent.Main && vehicle is CS_Car)
            {
                MissionCameraFadeView mcfv = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
                mcfv.BeginFadeOutAndIn(0, 0, 1);
                Vehicle = vehicle;
                SetupCamera();
            }
        }

        public void StopUsingVehicle(CS_Vehicle vehicle, Agent agent)
        {
            if (Vehicle == vehicle)
            {
                Vehicle = null;
                ReleaseCamera();
            }
        }

        private void ToggleLights()
        {
            if (Vehicle is CS_Car car) car.RequestLight(!car.LightOn, true);
        }

        private void Honk()
        {
            if (Vehicle is CS_Car car) car.RequestHonk(true);
        }

        public void SetupCamera()
        {
            _camera = Camera.CreateCamera();
            Camera combatCamera = MissionScreen.CombatCamera;
            if (combatCamera != null)
            {
                _camera.FillParametersFrom(combatCamera);
            }
            else
            {
                Debug.FailedAssert("Combat camera is null.", "C:\\Develop\\MB3\\Source\\Bannerlord\\SandBox.View\\Missions\\MissionHideoutCinematicView.cs", "SetupCamera", 66);
            }
            MatrixFrame newCameraFrame = Vehicle.GameEntity.GetGlobalFrame();
            SetCameraFrame(newCameraFrame, out _cameraFrame);
            _camera.Frame = _cameraFrame;
            MissionScreen.CustomCamera = _camera;
            _updateCamera = true;
        }

        public void SetCameraFrame(MatrixFrame newFrame, out MatrixFrame cameraFrame)
        {
            cameraFrame.origin = newFrame.origin;
            cameraFrame.rotation.s = newFrame.rotation.s;//Vec3.Side;
            cameraFrame.rotation.f = newFrame.rotation.u;
            cameraFrame.rotation.u = -newFrame.rotation.f;
            cameraFrame.rotation.RotateAboutSide(rotationDeltaY);
            cameraFrame.rotation.RotateAboutForward(rotationDeltaX);
            cameraFrame.rotation.Orthonormalize();
        }

        public void UpdateCamera(float dt)
        {
            Vec3 _cameraPosition = _cameraPositions[_currentCameraIndex];
            MatrixFrame newCameraFrame = Vehicle.GameEntity.GetGlobalFrame().Advance(_cameraPosition.x).Strafe(_cameraPosition.y).Elevate(_cameraPosition.z);
            //MatrixFrame newCameraFrame = Vehicle.GameEntity.GetGlobalFrame().Advance(0.38f).Strafe(0.25f).Elevate(1.10f);            
            SetCameraFrame(newCameraFrame, out _cameraFrame);
            HandleBasicCameraMovement(dt, ref _cameraFrame);
            _camera.Frame = _cameraFrame;
        }

        public void ReleaseCamera()
        {
            _updateCamera = false;
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
