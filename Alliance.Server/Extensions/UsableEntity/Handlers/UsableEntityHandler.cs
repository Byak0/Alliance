using Alliance.Common.Extensions;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Extensions.UsableEntity.Handlers
{
	public class UsableEntityHandler : IHandlerRegister
	{
		UsableEntityBehavior _usableEntityBehavior;

		UsableEntityBehavior UsableEntityBehavior
		{
			get
			{
				return _usableEntityBehavior ??= Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
			}
		}

		public void Register(NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestUseEntity>(HandleRequestUseEntity);
		}

		/// <summary>
		/// Handle player request to use entity.
		/// </summary>
		public bool HandleRequestUseEntity(NetworkCommunicator peer, RequestUseEntity model)
		{
			GameEntity entity = UsableEntityBehavior.FindClosestUsableEntity(model.Position, 0.05f);
			if (entity == null || peer.ControlledAgent == null)
			{
				string reason = entity == null ? "Entity not found" : "No agent controlled";
				BeginModuleEventAsServer(peer);
				WriteMessage(new ServerMessage($"Cannot use entity : {reason}", false));
				EndModuleEventAsServer();
				return false;
			}
			UsableEntityBehavior.UseEntity(entity, peer.ControlledAgent);
			return true;
		}
	}
}
