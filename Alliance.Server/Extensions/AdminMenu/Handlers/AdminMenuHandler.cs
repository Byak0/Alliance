using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Server.Core.Security;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.Extensions.AdminMenu.Handlers
{
    public class AdminMenuHandler : IHandlerRegister
    {
        private bool _invulnerable;

        public AdminMenuHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<AdminClient>(InitAdminServer);
            reg.Register<RequestNotification>(HandleNotificationRequest);
            reg.Register<SpawnHorseRequest>(HandleSpawnHorseRequest);
            reg.Register<TeleportRequest>(HandleTeleportRequest);
        }

        public bool HandleSpawnHorseRequest(NetworkCommunicator peer, SpawnHorseRequest req)
        {
            if (peer.IsAdmin())
            {
                string horseId = "mp_empire_horse_agile";
                string reinsId = "mp_imperial_riding_harness";

                ItemObject horseItem = Game.Current.ObjectManager.GetObject<ItemObject>(horseId);
                ItemObject reinsItem = Game.Current.ObjectManager.GetObject<ItemObject>(reinsId);

                // Ensure the ItemObject is a horse
                if (horseItem.IsMountable)
                {
                    EquipmentElement horseEquipmentElement = new EquipmentElement(horseItem);
                    EquipmentElement harnessEquipmentElement = new EquipmentElement(reinsItem);

                    // Spawn the horse agent
                    Agent horseAgent = Mission.Current.SpawnMonster(horseEquipmentElement, harnessEquipmentElement, new Vec3(10f, 10f, 1f), new Vec2(1, 0));

                    // Make the horse move to the player
                    WorldPosition target = peer.ControlledAgent.GetWorldPosition();
                    horseAgent.SetScriptedPositionAndDirection(ref target, 1f, false, AIScriptedFrameFlags.None);

                    return true;
                }
            }

            return false;
        }

        public bool HandleTeleportRequest(NetworkCommunicator peer, TeleportRequest req)
        {
            if (!peer.IsAdmin() || peer.ControlledAgent == null) return false;

            peer.ControlledAgent.TeleportToPosition(req.Position);
            Log($"[AdminPanel] L'admin {peer.UserName} s'est téléporté en {req.Position}", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] L'admin {peer.UserName} s'est téléporté en {req.Position}", AdminServerLog.ColorList.Success, true);

            return true;
        }

        public bool HandleNotificationRequest(NetworkCommunicator peer, RequestNotification notification)
        {
            if (peer.IsAdmin())
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SendNotification(notification.Text, notification.NotificationType));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            return false;
        }

        public bool InitAdminServer(NetworkCommunicator peer, AdminClient admin)
        {
            if (peer.IsAdmin())
            {
                if (admin.Heal)
                    return HealPlayer(peer, admin);
                if (admin.GodMod)
                    return GodMod(peer, admin);
                if (admin.Kill)
                    return Kill(peer, admin);
                if (admin.Kick)
                    return Kick(peer, admin);
                if (admin.Ban)
                    return Ban(peer, admin);
                if (admin.ToggleInvulnerable)
                    return ToggleInvulnerable(peer, admin);
                if (admin.TeleportToPlayer)
                    return TeleportToPlayer(peer, admin);
                if (admin.TeleportPlayerToYou)
                    return TeleportPlayerToYou(peer, admin);
            }
            if (peer.IsDev())
            {
                if (admin.SetAdmin)
                    return SetAdmin(peer, admin);
            }

            return false;
        }

        public bool TeleportPlayerToYou(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check if admin and target player both have an agent
            if (playerSelected == null || playerSelected.ControlledAgent == null || peer.ControlledAgent == null) return false;

            playerSelected.ControlledAgent.TeleportToPosition(peer.ControlledAgent.Position);

            Log($"[AdminPanel] Le joueur {playerSelected.UserName} a été téléporté par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été téléporté par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
            return true;
        }

        public bool TeleportToPlayer(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check if admin and target player both have an agent
            if (playerSelected == null || playerSelected.ControlledAgent == null || peer.ControlledAgent == null) return false;

            peer.ControlledAgent.TeleportToPosition(playerSelected.ControlledAgent.Position);

            Log($"[AdminPanel] L'administrateur {peer.UserName} s'est téléporté sur le joueur {playerSelected.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] L'administrateur {peer.UserName} s'est téléporté sur le joueur {playerSelected.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
            return true;
        }

        public bool HealPlayer(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe et contrôle un agent
            if (playerSelected == null || playerSelected.ControlledAgent == null) return false;

            playerSelected.ControlledAgent.Health = playerSelected.ControlledAgent.HealthLimit;
            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été soigné par l'administrateur {peer.UserName}", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} est soigné par {peer.UserName}", AdminServerLog.ColorList.Success, true);
            return true;
        }

        public bool GodMod(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe et contrôle un agent
            if (playerSelected == null || playerSelected.ControlledAgent == null) return false;

            playerSelected.ControlledAgent.BaseHealthLimit = 2000;
            playerSelected.ControlledAgent.HealthLimit = 2000;
            playerSelected.ControlledAgent.Health = 2000;
            playerSelected.ControlledAgent.SetMinimumSpeed(10);
            playerSelected.ControlledAgent.SetMaximumSpeedLimit(10, false);

            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} est en GodMod.", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été mis en GodMod par {peer.UserName}", AdminServerLog.ColorList.Success, true);

            return true;
        }

        public bool Kill(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe et contrôle un agent
            if (playerSelected == null || playerSelected.ControlledAgent == null) return false;

            Blow blow = new Blow(playerSelected.ControlledAgent.Index);
            blow.DamageType = DamageTypes.Pierce;
            blow.BoneIndex = playerSelected.ControlledAgent.Monster.HeadLookDirectionBoneIndex;
            blow.Position = playerSelected.ControlledAgent.Position;
            blow.Position.z = blow.Position.z + playerSelected.ControlledAgent.GetEyeGlobalHeight();
            blow.BaseMagnitude = 2000f;
            blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            blow.InflictedDamage = 2000;
            blow.SwingDirection = playerSelected.ControlledAgent.LookDirection;
            MatrixFrame frame = playerSelected.ControlledAgent.Frame;
            blow.SwingDirection = frame.rotation.TransformToParent(new Vec3(-1f, 0f, 0f, -1f));
            blow.SwingDirection.Normalize();
            blow.Direction = blow.SwingDirection;
            blow.DamageCalculated = true;
            sbyte mainHandItemBoneIndex = playerSelected.ControlledAgent.Monster.MainHandItemBoneIndex;
            AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.Position, Vec3.Zero, Vec3.Zero, playerSelected.ControlledAgent.Velocity, Vec3.Up);
            playerSelected.ControlledAgent.RegisterBlow(blow, attackCollisionDataForDebugPurpose);

            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été tué par l'admin {peer.UserName}.", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été tué par {peer.UserName}", AdminServerLog.ColorList.Success, true);

            return true;
        }

        public bool Kick(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe
            if (playerSelected == null) return false;

            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été kick.", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été kick par {peer.UserName}", AdminServerLog.ColorList.Success, true);
            MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();
            DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);
            return true;
        }

        public bool Ban(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe
            if (playerSelected == null) return false;

            MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();

            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été ban.", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été ban par {peer.UserName}", AdminServerLog.ColorList.Success);

            SecurityManager.AddBan(playerSelected.VirtualPlayer);

            DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);

            return true;
        }

        public bool SetAdmin(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check si joueur existe
            if (playerSelected == null) return false;

            if (playerSelected.IsAdmin())
            {
                Log($"[AdminPanel] Le joueur : {playerSelected.UserName} n'est plus admin.", LogLevel.Information);
                SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été retiré des admins par {peer.UserName}", AdminServerLog.ColorList.Success, true);
                SecurityManager.RemoveAdmin(playerSelected.VirtualPlayer);
            }
            else
            {
                Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été promu admin.", LogLevel.Information);
                SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été promu admin par {peer.UserName}", AdminServerLog.ColorList.Success, true);
                SecurityManager.AddAdmin(playerSelected.VirtualPlayer);
            }

            return true;
        }

        public bool ToggleInvulnerable(NetworkCommunicator peer, AdminClient admin)
        {
            if (admin.PlayerSelected == "")
            {
                MortalityState state = _invulnerable ? MortalityState.Mortal : MortalityState.Invulnerable;
                foreach (Agent agent in Mission.Current?.AllAgents)
                {
                    agent.SetMortalityState(state);
                }
                _invulnerable = !_invulnerable;
                Log($"[AdminPanel] Tous les agents ({Mission.Current?.AllAgents.Count}) ont été rendu {state.ToString()} par l'administrateur {peer.UserName}", LogLevel.Information);
                SendMessageToClient(peer, $"[Serveur] Tous les agents été rendu {state} par {peer.UserName}", AdminServerLog.ColorList.Success, true);
                return true;
            }

            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Si le joueur existe mais ne contrôle pas d'agent
            if (playerSelected != null && playerSelected.ControlledAgent == null) return false;
            playerSelected.ControlledAgent.ToggleInvulnerable();
            Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été rendu {playerSelected.ControlledAgent.CurrentMortalityState} par l'administrateur {peer.UserName}", LogLevel.Information);
            SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été rendu {playerSelected.ControlledAgent.CurrentMortalityState} par {peer.UserName}", AdminServerLog.ColorList.Success, true);
            return true;
        }

        private void SendMessageToClient(NetworkCommunicator targetPeer, string message, AdminServerLog.ColorList color, bool forAdmin = false)
        {
            if (!forAdmin)
            {
                GameNetwork.BeginModuleEventAsServer(targetPeer);
                GameNetwork.WriteMessage(new AdminServerLog(message, color));
                GameNetwork.EndModuleEventAsServer();
            }

            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new AdminServerLog(message, color));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
