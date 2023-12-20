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

        public const string InteractiveTag = "Al.interactive";
        public const string InteractiveNameTag = "Al.name.";
        public const string InteractiveTeamTag = "Al.team.";
        public const string InteractiveItemTag = "Al.item.";
        public const string InteractiveAmountTag = "Al.amount.";

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
