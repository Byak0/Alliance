using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace Alliance.Server.GameModes.Story.Models
{
    /// <summary>
    /// Store data between acts (agents positions, state, etc.)
    /// </summary>
    public class ScenarioPersistentData
    {
        private static readonly ScenarioPersistentData instance = new ScenarioPersistentData();
        public static ScenarioPersistentData Instance { get { return instance; } }

        public Dictionary<NetworkCommunicator, AgentInfos> PlayersInfos => _playersInfos;
        public List<AgentInfos> BotInfos => _botInfos;

        private Dictionary<NetworkCommunicator, AgentInfos> _playersInfos;
        private List<AgentInfos> _botInfos;

        public ScenarioPersistentData()
        {
            _playersInfos = new Dictionary<NetworkCommunicator, AgentInfos>();
            _botInfos = new List<AgentInfos>();
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
