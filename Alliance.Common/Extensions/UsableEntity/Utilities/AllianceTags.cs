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
        public const string InteractiveNameTag = "al_name_";
        public const string InteractiveTeamTag = "al_team_";
        public const string InteractiveItemTag = "al_item_";
        public const string InteractiveAmountTag = "al_amount_";

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
