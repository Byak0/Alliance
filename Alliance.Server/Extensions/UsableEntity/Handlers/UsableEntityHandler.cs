using Alliance.Common.Extensions;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Extensions.UsableEntity.Handlers
{
	public class UsableEntityHandler : IHandlerRegister
	{
		UsableEntityBehavior UsableEntityBehavior => Mission.Current.GetMissionBehavior<UsableEntityBehavior>();

		public void Register(NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestUseEntity>(HandleRequestUseEntity);
			reg.Register<RequestEditTextPanel>(HandleRequestEditTextPanel);
		}

		public bool HandleRequestUseEntity(NetworkCommunicator peer, RequestUseEntity model)
		{
			UsableEntityBehavior.InteractWithEntity(model.ID, peer.ControlledAgent);
			return true;
		}

		public bool HandleRequestEditTextPanel(NetworkCommunicator peer, RequestEditTextPanel model)
		{
			UsableEntityBehavior.InteractWithTextPanel(peer, model.ID, model.Text);
			return true;
		}
	}
}
