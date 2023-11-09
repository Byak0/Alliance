using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionPeer
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionPeer));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionPeer).GetMethod(nameof(MissionPeer.TickInactivityStatus),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionPeer).GetMethod(
                        nameof(Prefix_TickInactivityStatus), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionPeer)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Disable inactivity check for players controlling formations
        // Disable for everyone
        public static bool Prefix_TickInactivityStatus(MissionPeer __instance,
                                                       ref MissionTime ____lastActiveTime,
                                                       ref bool ____inactiveWarningGiven)
        {
            ____lastActiveTime = MissionTime.Now;
            ____inactiveWarningGiven = false;
            return false;
            //if (__instance.ControlledFormation != null)
            //{
            //    ____lastActiveTime = MissionTime.Now;
            //    ____inactiveWarningGiven = false;
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }


        /* Original method 
         * 
		public void TickInactivityStatus()
		{
			NetworkCommunicator networkPeer = this.GetNetworkPeer();
			if (!networkPeer.IsMine)
			{
				if (this.ControlledAgent != null && this.ControlledAgent.IsActive())
				{
					if (this._lastActiveTime == MissionTime.Zero)
					{
						this._lastActiveTime = MissionTime.Now;
						this._previousActivityStatus = ValueTuple.Create<Agent.MovementControlFlag, Vec2, Vec3>(this.ControlledAgent.MovementFlags, this.ControlledAgent.MovementInputVector, this.ControlledAgent.LookDirection);
						this._inactiveWarningGiven = false;
						return;
					}
					ValueTuple<Agent.MovementControlFlag, Vec2, Vec3> valueTuple = ValueTuple.Create<Agent.MovementControlFlag, Vec2, Vec3>(this.ControlledAgent.MovementFlags, this.ControlledAgent.MovementInputVector, this.ControlledAgent.LookDirection);
					if (this._previousActivityStatus.Item1 != valueTuple.Item1 || this._previousActivityStatus.Item2.DistanceSquared(valueTuple.Item2) > 1E-05f || this._previousActivityStatus.Item3.DistanceSquared(valueTuple.Item3) > 1E-05f)
					{
						this._lastActiveTime = MissionTime.Now;
						this._previousActivityStatus = valueTuple;
						this._inactiveWarningGiven = false;
					}
					if (this._lastActiveTime.ElapsedSeconds > 180f)
					{
						DisconnectInfo disconnectInfo = networkPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
						disconnectInfo.Type = DisconnectType.Inactivity;
						networkPeer.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
						GameNetwork.AddNetworkPeerToDisconnectAsServer(networkPeer);
						return;
					}
					if (this._lastActiveTime.ElapsedSeconds > 120f && !this._inactiveWarningGiven)
					{
						MultiplayerGameNotificationsComponent missionBehavior = Mission.Current.GetMissionBehavior<MultiplayerGameNotificationsComponent>();
						if (missionBehavior != null)
						{
							missionBehavior.PlayerIsInactive(this.GetNetworkPeer());
						}
						this._inactiveWarningGiven = true;
						return;
					}
				}
				else
				{
					this._lastActiveTime = MissionTime.Now;
					this._inactiveWarningGiven = false;
				}
			}
		}*/
    }
}
