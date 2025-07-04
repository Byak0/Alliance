using Alliance.Common.Extensions.PlayerSpawn.Models;
using static Alliance.Common.Extensions.PlayerSpawn.Utilities.PlayerSpawnMenuNetworkHelper;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages
{
	public interface IPlayerSpawnMenuMessage
	{
		public PlayerSpawnMenuOperation Operation { get; set; }
		public int TeamIndex { get; set; }
		public int FormationIndex { get; set; }
		public PlayerTeam PlayerTeam { get; set; }
		public PlayerFormation PlayerFormation { get; set; }
		public AvailableCharacter AvailableCharacter { get; set; }
	}
}
