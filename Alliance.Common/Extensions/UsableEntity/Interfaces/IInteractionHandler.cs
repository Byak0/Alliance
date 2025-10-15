using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.UsableEntity.Behaviors.UsableEntityBehavior;

namespace Alliance.Common.Extensions.UsableEntity.Interfaces
{
	public interface IInteractionHandler
	{
		// Check if the handler can handle this entity
		public bool CanHandle(GameEntity entity);

		// Check for distance, LOS, etc.
		public bool CanInteract(Agent agent, GameEntity entity);

		// Text displayed on HUD when looking at the entity
		public TextObject GetInteractionText(Agent agent, GameEntity entity);

		// Perform the interaction
		public void Interact(Agent agent, InteractionTarget entity);
	}
}
