using Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Models
{
	/// <summary>
	/// Store data between acts (agents positions, state, etc.)
	/// </summary>
	public class ScenarioPersistentData
	{
		private static readonly ScenarioPersistentData instance = new ScenarioPersistentData();
		public static ScenarioPersistentData Instance { get { return instance; } }

		public Dictionary<NetworkCommunicator, int> PlayerUsedLives { get; set; }
		public Dictionary<NetworkCommunicator, int> PlayerRemainingLives { get; set; }
		public Dictionary<NetworkCommunicator, SpawnLocation> SelectedLocations { get; set; }
		public Dictionary<BattleSideEnum, int> TeamRemainingLives { get; set; }

		public Dictionary<NetworkCommunicator, AgentInfos> PlayersInfos => _playersInfos;
		public List<AgentInfos> BotInfos => _botInfos;

		private Dictionary<NetworkCommunicator, AgentInfos> _playersInfos;
		private List<AgentInfos> _botInfos;

		public ScenarioPersistentData()
		{
			_playersInfos = new Dictionary<NetworkCommunicator, AgentInfos>();
			_botInfos = new List<AgentInfos>();
			SelectedLocations = new Dictionary<NetworkCommunicator, SpawnLocation>();
			PlayerUsedLives = new Dictionary<NetworkCommunicator, int>();
			PlayerRemainingLives = new Dictionary<NetworkCommunicator, int>();
			TeamRemainingLives = new Dictionary<BattleSideEnum, int>();
		}

		public void Reset()
		{
			_playersInfos = new Dictionary<NetworkCommunicator, AgentInfos>();
			_botInfos = new List<AgentInfos>();
		}

		public void AddPlayerInfo(NetworkCommunicator peer, BattleSideEnum side, int group, BasicCharacterObject character, MatrixFrame frame)
		{
			_playersInfos.Add(peer, new AgentInfos(side, group, character, frame));
		}

		public void AddBotInfo(BattleSideEnum side, int group, BasicCharacterObject character, MatrixFrame frame)
		{
			_botInfos.Add(new AgentInfos(side, group, character, frame));
		}
	}

	public struct AgentInfos
	{
		public BattleSideEnum Side;
		public int Group;
		public BasicCharacterObject Character;
		public MatrixFrame Location;

		public AgentInfos(BattleSideEnum side, int group, BasicCharacterObject character, MatrixFrame location)
		{
			Side = side;
			Group = group;
			Character = character;
			Location = location;
		}
	}
}
