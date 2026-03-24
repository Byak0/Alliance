using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security.Extension
{
	public static class RolesExtension
	{
		public static bool IsOfficer(this NetworkCommunicator player) => IsOfficer(player.VirtualPlayer);
		public static bool IsOfficer(this VirtualPlayer player)
		{
			// Commanders are always considered officers
			if (IsCommander(player)) return true;
			return PlayerSpawnMenu.Instance.MyAssignment?.Formation?.Officer == player.Communicator;
		}

		public static bool IsCommander(this NetworkCommunicator player) => IsCommander(player.VirtualPlayer);
		public static bool IsCommander(this VirtualPlayer player)
		{
			Team team = player.GetComponent<MissionPeer>()?.Team;
			bool validTeam = team == Mission.Current.AttackerTeam || team == Mission.Current.DefenderTeam;
			return validTeam && MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC";
		}

		public static bool IsAdmin(this NetworkCommunicator player) => PlayerStore.Instance.OnlineAdmins.Contains(player);
		public static bool IsAdmin(this PlayerId playerId) => PlayerStore.Instance.AllPlayersData.TryGetValue(playerId, out var data) && data.IsAdmin;
		public static bool IsAdmin(this VirtualPlayer player) => IsAdmin(player.Id);

		public static bool IsSudo(this NetworkCommunicator player) => PlayerStore.Instance.OnlinePlayersData.TryGetValue(player, out var data) && data.IsSudo;
		public static bool IsSudo(this PlayerId playerId) => PlayerStore.Instance.AllPlayersData.TryGetValue(playerId, out var data) && data.IsSudo;
		public static bool IsSudo(this VirtualPlayer player) => IsSudo(player.Id);

		public static bool IsBanned(this NetworkCommunicator player) => PlayerStore.Instance.OnlinePlayersData.TryGetValue(player, out var data) && data.IsBanned;
		public static bool IsBanned(this PlayerId playerId) => PlayerStore.Instance.AllPlayersData.TryGetValue(playerId, out var data) && data.IsBanned;
		public static bool IsBanned(this VirtualPlayer player) => IsBanned(player.Id);

		public static bool IsMuted(this NetworkCommunicator player) => PlayerStore.Instance.OnlinePlayersData.TryGetValue(player, out var data) && data.IsMuted;
		public static bool IsMuted(this PlayerId playerId) => PlayerStore.Instance.AllPlayersData.TryGetValue(playerId, out var data) && data.IsMuted;
		public static bool IsMuted(this VirtualPlayer player) => IsMuted(player.Id);

		public static bool IsVIP(this NetworkCommunicator player) => PlayerStore.Instance.OnlinePlayersData.TryGetValue(player, out var data) && data.VIP;
		public static bool IsVIP(this PlayerId playerId) => PlayerStore.Instance.AllPlayersData.TryGetValue(playerId, out var data) && data.VIP;
		public static bool IsVIP(this VirtualPlayer player) => IsVIP(player.Id);
	}
}