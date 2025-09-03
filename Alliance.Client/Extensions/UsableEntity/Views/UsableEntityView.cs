using Alliance.Client.Extensions.UsableEntity.ViewModels;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Extensions.UsableEntity.Behaviors.UsableEntityBehavior;

namespace Alliance.Client.Extensions.UsableEntity.Views
{
	/// <summary>
	/// Interact with items in the scene.
	/// </summary>
	[DefaultView]
	public class UsableEntityView : MissionView
	{
		private const int LeftAltGameKey = 5;
		private UsableEntityBehavior _entityInteractionBehavior;
		private GauntletLayer _gauntletLayer;
		private EntityInteractionVM _dataSource;
		private InteractionTarget? _targetEntity;

		public UsableEntityView() { }

		public override void OnBehaviorInitialize()
		{
			_entityInteractionBehavior = Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
			_dataSource = new EntityInteractionVM();
			_gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
			_gauntletLayer.LoadMovie("EntityInteractionHUD", _dataSource);
			MissionScreen.AddLayer(_gauntletLayer);
		}

		public override void OnMissionScreenFinalize()
		{
			MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
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
				InteractionTarget? closestTarget = _entityInteractionBehavior.FindEntityUsableByAgent(Agent.Main);

				if (_targetEntity?.ID != closestTarget?.ID)
				{
					if (_targetEntity != null)
					{
						DisableInteraction(_targetEntity.Value);
					}
					if (closestTarget != null)
					{
						EnableInteraction(closestTarget.Value);
					}
					_targetEntity = closestTarget;
				}
			}

			if (_targetEntity != null && Input.IsKeyReleased(InputKey.F))
			{
				RequestToUseEntity(_targetEntity.Value);
				_targetEntity = null;
			}
		}

		private void EnableInteraction(InteractionTarget target)
		{
			target.Entity.SetContourColor(Colors.Green.ToUnsignedInteger(), true);
			_dataSource.InteractionInterface.IsActive = true;
			_dataSource.InteractionInterface.PrimaryInteractionMessage = target.Handler.GetInteractionText(Agent.Main, target.Entity).ToString();
		}

		private void DisableInteraction(InteractionTarget target)
		{
			_dataSource.InteractionInterface.IsActive = false;
			target.Entity.SetContourColor(null, false);
		}

		public void RequestToUseEntity(InteractionTarget entity)
		{
			_dataSource.InteractionInterface.IsActive = false;
			entity.Entity?.SetContourColor(null, false);
			entity.Handler.Interact(Agent.Main, entity);
		}
	}
}
