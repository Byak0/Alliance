using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromClient;
using Alliance.Server.Extensions.ToggleEntities.Behaviors;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.ToggleEntities.Handlers
{
	public class ToggleEntitiesHandler : IHandlerRegister
	{
		public ToggleEntitiesHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestToggleEntities>(HandleToggleEntities);
		}

		public bool HandleToggleEntities(NetworkCommunicator peer, RequestToggleEntities message)
		{
			if (!peer.IsAdmin())
			{
				Log($"ATTENTION : {peer.UserName} is requesting to {(message.Show ? "show" : "hide")} entities with tag {message.EntitiesTag} despite not being admin !", LogLevel.Error);
				return false;
			}
			Log($"Alliance - {peer.UserName} is requesting to {(message.Show ? "show" : "hide")} entities with tag {message.EntitiesTag}.", LogLevel.Information);

			ToggleEntitiesBehavior behavior = Mission.Current?.GetMissionBehavior<ToggleEntitiesBehavior>();
			if (behavior == null)
			{
				Log($"Error in ToggleEntitiesHandler - ToggleEntitiesBehavior not found in mission.", LogLevel.Error);
				return false;
			}

			behavior.SetTagVisibility(message.EntitiesTag, message.Show);

			return true;
		}
	}
}
