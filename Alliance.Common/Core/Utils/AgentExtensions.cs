using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core.Utils
{
	public static class AgentExtensions
	{
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
	}
}
