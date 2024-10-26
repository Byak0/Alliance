using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Common.Extensions.Revive.Models
{
    /// <summary>
    /// Store all necessary informations about a wounded agent.
    /// </summary>
    public class WoundedAgentInfos
    {
        public GameEntity WoundedAgentEntity { get; }
        public string Name { get; }
        public NetworkCommunicator Player { get; }
        public BattleSideEnum Side { get; }
        public BasicCultureObject Culture { get; }
        public BasicCharacterObject Character { get; }
        public MPOnSpawnPerkHandler OnSpawnPerkHandler { get; }
        //public MatrixFrame Position { get; }
        public int PreviousFormation { get; }
        public float AgentDifficulty { get; }
        public IEnumerable<(EquipmentIndex, EquipmentElement)> AlternativeEquipment { get; }
        public int RemainingLives { get; }

        /// <summary>
        /// Standard constructor for wounded agents created at runtime.
        /// </summary>
        public WoundedAgentInfos(GameEntity woundedAgentEntity, string name, NetworkCommunicator player, BattleSideEnum side, BasicCultureObject culture, BasicCharacterObject character, MPOnSpawnPerkHandler onSpawnPerkHandler, MatrixFrame position, int previousFormation, float agentDifficulty, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipment, int remainingLives)
        {
            WoundedAgentEntity = woundedAgentEntity;
            Name = name;
            Player = player;
            Side = side;
            Culture = culture;
            Character = character;
            OnSpawnPerkHandler = onSpawnPerkHandler;
            //Position = position;
            PreviousFormation = previousFormation;
            AgentDifficulty = agentDifficulty;
            AlternativeEquipment = alternativeEquipment;
            RemainingLives = remainingLives;
        }

        /// <summary>
        /// Alternative constructor for entities placed on the scene by the map maker. Use Entity tags.
        /// </summary>
        public WoundedAgentInfos(GameEntity woundedAgentEntity)
        {
            WoundedAgentEntity = woundedAgentEntity;

            Name = WoundedAgentEntity.Tags.SingleOrDefault((tag) => tag.StartsWith(ReviveTags.WoundedNameTag)).Replace(ReviveTags.WoundedNameTag, "");

            string side = WoundedAgentEntity.Tags.SingleOrDefault((tag) => tag.StartsWith(ReviveTags.WoundedTeamTag)).Replace(ReviveTags.WoundedTeamTag, "");
            Side = ReviveTags.StringToBattleSide.ContainsKey(side) ? ReviveTags.StringToBattleSide[side] : BattleSideEnum.None;

            int.TryParse(WoundedAgentEntity.Tags.SingleOrDefault((tag) => tag.StartsWith(ReviveTags.WoundedFormationTag)).Replace(ReviveTags.WoundedFormationTag, ""), out int previousFormation);
            PreviousFormation = previousFormation;

            string character = WoundedAgentEntity.Tags.SingleOrDefault((tag) => tag.StartsWith(ReviveTags.WoundedNameTag)).Replace(ReviveTags.WoundedNameTag, "");
            if (character.IsEmpty())
                character = "mp_character";
            Character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(character);

            //Position = WoundedAgentEntity.GetGlobalFrame();

            RemainingLives = 1;
        }
    }
}