using Alliance.Client.Extensions.UsableEntity.ViewModels;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.UsableEntity.Views
{
    /// <summary>
    /// Interact with items in the scene.    
    /// </summary>
    public class UsableEntityView : MissionView
    {
        private const int LeftAltGameKey = 5;
        private UsableEntityBehavior _entityInteractionBehavior;
        private GauntletLayer _gauntletLayer;
        private EntityInteractionVM _dataSource;
        private GameEntity _targetEntity;

        public UsableEntityView() { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _entityInteractionBehavior = Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
            _dataSource = new EntityInteractionVM();
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("EntityInteractionHUD", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Input.IsGameKeyDown(LeftAltGameKey))
            {
                _dataSource.IsEnabled = true;
            }
            else
            {
                _dataSource.IsEnabled = false;
            }

            if (Agent.Main != null)
            {
                GameEntity closestEntity = _entityInteractionBehavior.FindEntityUsableByAgent(Agent.Main);

                if (_targetEntity != closestEntity)
                {
                    if (_targetEntity != null)
                    {
                        DisableInteraction(_targetEntity);
                    }
                    if (closestEntity != null)
                    {
                        EnableInteraction(closestEntity);
                    }
                    _targetEntity = closestEntity;
                }
            }

            if (_targetEntity != null && Input.IsKeyReleased(InputKey.F))
            {
                RequestToUseEntity(_targetEntity);
                _targetEntity = null;
            }
        }

        private void EnableInteraction(GameEntity entity)
        {
            entity.SetContourColor(Colors.Green.ToUnsignedInteger(), true);
            _dataSource.InteractionInterface.IsActive = true;
            TextObject textObject = new TextObject("Press {KEY} to use " + entity.Name);
            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            _dataSource.InteractionInterface.PrimaryInteractionMessage = textObject.ToString();
        }

        private void DisableInteraction(GameEntity entity)
        {
            _dataSource.InteractionInterface.IsActive = false;
            entity.SetContourColor(null, false);
        }

        public void RequestToUseEntity(GameEntity entity)
        {
            _dataSource.InteractionInterface.IsActive = false;
            entity?.SetContourColor(null, false);

            Log($"Requesting to use entity {entity.Name} at {entity.GlobalPosition}", LogLevel.Debug);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestUseEntity(entity.GlobalPosition));
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
