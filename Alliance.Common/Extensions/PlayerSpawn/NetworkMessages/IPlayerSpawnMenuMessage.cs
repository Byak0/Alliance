using Alliance.Common.Extensions.PlayerSpawn.Models;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages
{
	public interface IPlayerSpawnMenuMessage
	{
		public PlayerSpawnMenuOperation Operation { get; }
		public int TeamIndex { get; }
		public int FormationIndex { get; }
		public PlayerTeam PlayerTeam { get; }
		public PlayerFormation PlayerFormation { get; }
		public AvailableCharacter AvailableCharacter { get; }
	}
}
