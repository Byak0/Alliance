using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core.Utils
{
	public static class AgentExtensions
	{
		private static readonly int _trollRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("troll");
		private static readonly int _entRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("ent");
		private static readonly int _dwarfRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("dwarf");

		public static void DealDamage(this Agent agent, Agent victim, int damage, float magnitude = 50f)
		{
			CoreUtils.TakeDamage(victim, agent, damage, magnitude);
		}

		public static bool IsTroll(this Agent agent)
		{
			return agent.Character?.Race == _trollRaceId;
		}

		public static bool IsEnt(this Agent agent)
		{
			return agent.Character?.Race == _entRaceId;
		}

		public static bool IsDwarf(this Agent agent)
		{
			return agent.Character?.Race == _dwarfRaceId;
		}

		public static bool IsWarg(this Agent agent)
		{
			return agent.Monster.StringId == "warg";
		}
	}
}
