using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
	class Patch_MissionLobbyComponent
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionLobbyComponent));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(MissionLobbyComponent).GetMethod(nameof(MissionLobbyComponent.OnMissionTick),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_MissionLobbyComponent).GetMethod(
						nameof(Prefix_OnMissionTick), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MissionLobbyComponent)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		// Prevent server from closing if only one player is connected after warmup
		public static bool Prefix_OnMissionTick(MissionLobbyComponent __instance, MultiplayerWarmupComponent ____warmupComponent, MultiplayerTimerComponent ____timerComponent, MissionMultiplayerGameModeBase ____gameMode)
		{
			if (__instance.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
			{
				bool timerPassed = ____timerComponent.CheckIfTimerPassed();
				bool isInWarmup = ____warmupComponent?.IsInWarmup ?? false;
				if (____warmupComponent == null || isInWarmup && timerPassed)
				{
					Log($"Alliance - Prefix_OnMissionTick => Forcing MissionLobbyComponent.SetStatePlayingAsServer()", LogLevel.Debug);

					// Force server playing even if not enough players
					typeof(MissionLobbyComponent).GetMethod("SetStatePlayingAsServer",
						BindingFlags.Instance | BindingFlags.NonPublic)?
						.Invoke(__instance, new object[] { });

					// Skip original method
					return false;
				}
			}
			else if (__instance.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing)
			{
				if (GameNetwork.IsServerOrRecorder && !____gameMode.UseRoundController())
				{
					//Log("Vulcain - Prefix_OnMissionTick => Skipping MissionLobbyComponent.SetStateEndingAsServer()", 0, Debug.DebugColor.Yellow);
					// Skip original method and prevent server ending
					return false;
				}
			}
			// Run original method
			return true;
		}


		/* Original method 
         * 		
        public override void OnMissionTick(float dt)
		{
			if (GameNetwork.IsClient && this._inactivityTimer.Check(base.Mission.CurrentTime))
			{
				NetworkMain.GameClient.IsInCriticalState = MBAPI.IMBNetwork.ElapsedTimeSinceLastUdpPacketArrived() > (double)MissionLobbyComponent.InactivityThreshold;
			}
			if (this.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
			{
				if (GameNetwork.IsServer && (this._warmupComponent == null || (!this._warmupComponent.IsInWarmup && this._timerComponent.CheckIfTimerPassed())))
				{
					int num = GameNetwork.NetworkPeers.Count((NetworkCommunicator x) => x.IsSynchronized);
					int num2 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
					int intValue = MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
					if (num + num2 >= intValue || MBCommon.CurrentGameType == MBCommon.GameType.MultiClientServer)
					{
						this.SetStatePlayingAsServer();
						return;
					}
				}
			}
			else if (this.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing)
			{
				bool flag = this._timerComponent.CheckIfTimerPassed();
				if (GameNetwork.IsServerOrRecorder && this._gameMode.RoundController == null && (flag || this._gameMode.CheckForMatchEnd()))
				{
					this._gameMode.GetWinnerTeam();
					this._gameMode.SpawnComponent.SpawningBehavior.RequestStopSpawnSession();
					this._gameMode.SpawnComponent.SpawningBehavior.SetRemainingAgentsInvulnerable();
					this.SetStateEndingAsServer();
				}
			}
		}*/
	}
}
