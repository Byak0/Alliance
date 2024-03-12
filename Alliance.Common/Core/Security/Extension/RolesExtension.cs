using Alliance.Common.Core.Security.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security.Extension
{
    public static class RolesExtension
    {
        public static string GetPlayerName(this PlayerId playerId)
        {
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (networkPeer.VirtualPlayer.Id == playerId)
                {
                    return networkPeer.VirtualPlayer.UserName;
                }
            }

            return "";
        }

        public static bool IsOfficer(this NetworkCommunicator player)
        {
            return IsOfficer(player.VirtualPlayer);
        }

        public static bool IsOfficer(this VirtualPlayer player)
        {
            foreach (Player officer in Roles.Instance.Officers)
            {
                if (officer.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsCommander(this NetworkCommunicator player)
        {
            return IsCommander(player.VirtualPlayer);
        }

        public static bool IsCommander(this VirtualPlayer player)
        {
            Team team = player.GetComponent<MissionPeer>()?.Team;
            bool validTeam = team == Mission.Current.AttackerTeam || team == Mission.Current.DefenderTeam;

            if (validTeam && MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC")
            {
                return true;
            }
            foreach (Player commander in Roles.Instance.Commanders)
            {
                if (commander.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsAdmin(this NetworkCommunicator player)
        {
            return IsAdmin(player.VirtualPlayer);
        }

        public static bool IsAdmin(this VirtualPlayer player)
        {
            foreach (Player admin in Roles.Instance.Admins)
            {
                if (admin.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsDev(this NetworkCommunicator player)
        {
            return IsDev(player.VirtualPlayer);
        }

        public static bool IsDev(this VirtualPlayer player)
        {
            foreach (Player dev in Roles.Instance.Devs)
            {
                if (dev.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsModerator(this NetworkCommunicator player)
        {
            return IsModerator(player.VirtualPlayer);
        }

        public static bool IsModerator(this VirtualPlayer player)
        {
            foreach (Player moderator in Roles.Instance.Moderators)
            {
                if (moderator.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsBanned(this NetworkCommunicator player)
        {
            return IsBanned(player.VirtualPlayer);
        }

        public static bool IsBanned(this VirtualPlayer player)
        {
            foreach (Player banned in Roles.Instance.Banned)
            {
                if (banned.Id.Equals(player.Id))
                {
                    return true;
                }
            }
            return false;
        }
    }
}