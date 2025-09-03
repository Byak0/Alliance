using Alliance.Common.Extensions;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Client.Extensions.UsableEntity.Handlers
{
	public class UsableEntityHandler : IHandlerRegister
	{
		UsableEntityBehavior _gameMode;

		UsableEntityBehavior GameMode
		{
			get
			{
				return _gameMode ??= Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
			}
		}

		public void Register(NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<HideEntity>(HideEntity);
			reg.Register<ResetUsableEntityVisibility>(ResetUsableEntityVisibility);
		}

		public void HideEntity(HideEntity message)
		{
			try
			{
				GameMode.HideEntity(message.ID);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to hide entity {message.ID}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		public void ResetUsableEntityVisibility(ResetUsableEntityVisibility message)
		{
			try
			{
				GameMode.ResetItemsWithTagRespawnEachRound();
			}
			catch (Exception ex)
			{
				Log("Alliance - Failed to reset usable items visibility", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}
	}
}
