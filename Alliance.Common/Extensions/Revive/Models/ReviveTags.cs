using System.Collections.Generic;
using TaleWorlds.Core;

namespace Alliance.Common.Extensions.Revive.Models
{
    public class ReviveTags
    {
        public static readonly Dictionary<string, BattleSideEnum> StringToBattleSide = new Dictionary<string, BattleSideEnum>() {
            { BattleSideEnum.None.ToString().ToLower(), BattleSideEnum.None },
            { BattleSideEnum.Attacker.ToString().ToLower(), BattleSideEnum.Attacker },
            { BattleSideEnum.Defender.ToString().ToLower(), BattleSideEnum.Defender }
        };

        public const string WoundedTag = "Al.wounded";
        public const string WoundedNameTag = "Al.name.";
        public const string WoundedTeamTag = "Al.team.";
        public const string WoundedFormationTag = "Al.formation.";
        public const string WoundedCharacterTag = "Al.character.";
    }
}