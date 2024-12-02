using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.TroopSpawner.Models
{
    public class TroopTypeModel
    {
        private static readonly TroopTypeModel instance = new();
        public static TroopTypeModel Instance { get { return instance; } }

        public readonly List<BasicCharacterObject> AvailableTroops = new();
        public readonly Dictionary<BasicCharacterObject, string> TroopsType = new();
        public readonly Dictionary<BasicCultureObject, Dictionary<string, List<BasicCharacterObject>>> TroopsPerCultureAndType = new();
        public string InfantryType = GameTexts.FindText("str_troop_type_name", "Infantry").ToString();
        public string RangedType = GameTexts.FindText("str_troop_type_name", "Ranged").ToString();
        public string CavalryType = GameTexts.FindText("str_troop_type_name", "Cavalry").ToString();
        public string HorseArcherType = GameTexts.FindText("str_troop_type_name", "HorseArcher").ToString();

        private TroopTypeModel()
        {
            foreach (MultiplayerClassDivisions.MPHeroClass heroClass in MultiplayerClassDivisions.GetMPHeroClasses().ToList())
            {
                AvailableTroops.Add(heroClass.TroopCharacter);
                TroopsType.Add(heroClass.TroopCharacter, heroClass.IconType.ToString());
            }

            foreach (BasicCharacterObject troop in AvailableTroops)
            {
                if (!TroopsPerCultureAndType.ContainsKey(troop.Culture))
                {
                    TroopsPerCultureAndType[troop.Culture] = new Dictionary<string, List<BasicCharacterObject>>
                    {
                        { InfantryType, new List<BasicCharacterObject>() },
                        { RangedType, new List<BasicCharacterObject>() },
                        { CavalryType, new List<BasicCharacterObject>() },
                        { HorseArcherType, new List<BasicCharacterObject>() }
                    };
                }

                string troopType = troop.IsMounted ? (troop.IsRanged ? HorseArcherType : CavalryType) : (troop.IsRanged ? RangedType : InfantryType);
                TroopsPerCultureAndType[troop.Culture][troopType].Add(troop);
            }
        }
    }
}
