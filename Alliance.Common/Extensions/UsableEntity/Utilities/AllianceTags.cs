using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Common.Extensions.UsableEntity.Utilities
{
    public static class AllianceTags
    {
        public static readonly Dictionary<string, BattleSideEnum> StringToBattleSide = new Dictionary<string, BattleSideEnum>() {
            { BattleSideEnum.None.ToString().ToLower(), BattleSideEnum.None },
            { BattleSideEnum.Attacker.ToString().ToLower(), BattleSideEnum.Attacker },
            { BattleSideEnum.Defender.ToString().ToLower(), BattleSideEnum.Defender }
        };

        public const string InteractiveTag = "al_interactive";
        public const string InteractiveItemTag = "al_item_";
        public const string InteractiveNameTag = "al_name_"; // Not implemented yet
        public const string InteractiveTeamTag = "al_team_"; // Not implemented yet
        public const string InteractiveAmountTag = "al_amount_"; // Not implemented yet

        public const string ENTITY_TO_RESPAWN_ON_EACH_ROUND_TAG = "al_respawn_on_each_round";

        /// <summary>
        /// Return the tag value that maches the prefix. Returns empty if no match found.
        /// </summary>
        public static string GetTagValue(this GameEntity entity, string prefix)
        {
            string tagValue = entity.Tags?.FirstOrDefault(tag => tag.StartsWith(prefix));
            return tagValue != null ? tagValue.Substring(prefix.Length) : "";
        }
    }
}
