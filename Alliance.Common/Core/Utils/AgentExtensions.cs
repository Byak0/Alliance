using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Common.Core.Utils
{
	public static class AgentExtensions
	{
		public enum ClassType
		{
			Troop,
			Hero,
			BannerBearer
		}

		private static readonly int _trollRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("troll");
		private static readonly int _ologRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("olog");
		private static readonly int _olog2RaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("olog2");
		private static readonly int _entRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("ent");
		private static readonly int _dwarfRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("dwarf");

		public static void DealDamage(this Agent agent, Agent victim, int damage, float magnitude = 50f, bool knockDown = false)
		{
			CoreUtils.TakeDamage(victim, agent, damage, magnitude, knockDown);
		}

		public static bool IsTroll(this Agent agent)
		{
			return agent.Character?.IsTroll() ?? false;
		}

		public static bool IsTroll(this BasicCharacterObject character)
		{
			return character.Race == _trollRaceId || character.Race == _ologRaceId || character.Race == _olog2RaceId;
		}

		public static bool IsEnt(this Agent agent)
		{
			return agent.Character?.IsEnt() ?? false;
		}

		public static bool IsEnt(this BasicCharacterObject character)
		{
			return character.Race == _entRaceId;
		}

		public static bool IsDwarf(this Agent agent)
		{
			return agent.Character?.IsDwarf() ?? false;
		}

		public static bool IsDwarf(this BasicCharacterObject character)
		{
			return character.Race == _dwarfRaceId;
		}

		public static bool IsWarg(this Agent agent)
		{
			return agent.Monster.StringId == "warg";
		}

		public static bool IsHorse(this Agent agent)
		{
			return agent.Monster.StringId == "horse";
		}

		public static bool IsCamel(this Agent agent)
		{
			return agent.Monster.StringId == "camel";
		}

		public static MPHeroClass GetHeroClass(this BasicCharacterObject character)
		{
			return MBObjectManager.Instance.GetObjectTypeList<MPHeroClass>().FirstOrDefault((MPHeroClass x) => x.HeroCharacter == character || x.TroopCharacter == character || x.BannerBearerCharacter == character);
		}

		public static List<List<IReadOnlyPerkObject>> GetMPPerks(this BasicCharacterObject character)
		{
			MPHeroClass heroClass = character.GetHeroClass();

			if (heroClass == null)
			{
				Log($"Hero class for character {character.Name} is null", LogLevel.Warning);
				return null;
			}

			// Determine the troop type based on the hero class
			ClassType troopType = ClassType.Troop;
			if (heroClass.HeroCharacter == character)
			{
				troopType = ClassType.Hero;
			}
			else if (heroClass.BannerBearerCharacter == character)
			{
				troopType = ClassType.BannerBearer;
			}

			List<List<IReadOnlyPerkObject>> allPerksForHeroClass = MultiplayerClassDivisions.GetAllPerksForHeroClass(heroClass);

			// Ignore perks if it only contains a default one
			if (allPerksForHeroClass.Count >= 0 && allPerksForHeroClass[0].Count == 1 && allPerksForHeroClass[0][0].Name.Value.Contains("Default"))
			{
				return null;
			}

			List<List<IReadOnlyPerkObject>> perksToShow = new List<List<IReadOnlyPerkObject>>();

			// Filter out BannerBearer perks if necessary
			bool isTroopTypeBannerBearer = troopType == ClassType.BannerBearer;
			for (int i = 0; i < allPerksForHeroClass.Count; i++)
			{
				bool hasBannerBearerPerk = allPerksForHeroClass[i].Exists(perk => ((MPPerkObject)perk).HasBannerBearer);

				// Show the perk if it's not a BannerBearer perk, or if it's a BannerBearer perk and the troop is a BannerBearer.
				bool showPerk = !hasBannerBearerPerk || (isTroopTypeBannerBearer && hasBannerBearerPerk);
				if (allPerksForHeroClass[i].Count > 0 && showPerk)
				{
					perksToShow.Add(allPerksForHeroClass[i]);
				}
			}

			return perksToShow;
		}
	}
}
