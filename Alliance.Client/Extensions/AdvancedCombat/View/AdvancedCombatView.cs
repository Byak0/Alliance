using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.NetworkMessages.FromClient;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AdvancedCombat.View
{
	[DefaultView]
	public class AdvancedCombatView : MissionView
	{
		private float cooldownDuration = 1.0f; // Cooldown duration in seconds between two attacks.
		private DateTime lastAttackTime = DateTime.MinValue; // Initialize the last moment when the attack was used to a minimum value.

		public AdvancedCombatView() { }

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (Agent.Main != null)
			{
				if (Agent.Main.HasMount && Agent.Main.MountAgent.IsWarg() && Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Q))
				{
					if (CanUseWargAttack())
					{
						WargAttack();
						lastAttackTime = DateTime.UtcNow; // Update the last moment when the attack was used.                          
					}
					else
					{
						Log($"Warg Attack on cooldown! {TimeSpan.FromSeconds(cooldownDuration) - (DateTime.UtcNow - lastAttackTime)} secondes remaining", LogLevel.Information);
					}

				}
			}
		}

		private bool CanUseWargAttack()
		{
			// Check if enough time has passed since the last use.
			return DateTime.UtcNow - lastAttackTime >= TimeSpan.FromSeconds(cooldownDuration);
		}

		public void WargAttack()
		{
			Log($"Using WargAttack !", LogLevel.Information);

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestSpecialAttack()); // Send message to Common.
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
