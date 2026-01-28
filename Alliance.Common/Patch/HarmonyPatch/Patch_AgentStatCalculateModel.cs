using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch for agent stats model.
	/// </summary>
	public static class Patch_AgentStatCalculateModel
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_AgentStatCalculateModel));

		private static bool _patched;

		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					AccessTools.Method(typeof(AgentStatCalculateModel), "CalculateAILevel"),
					postfix: new HarmonyMethod(typeof(Patch_AgentStatCalculateModel), nameof(Postfix_CalculateAILevel))
				);

				return true;
			}
			catch (Exception e)
			{
				Log("Error in Patch_AgentStatCalculateModel: " + e.ToString(), LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Multiply AI level by agent difficulty.
		/// </summary>
		public static void Postfix_CalculateAILevel(Agent agent, int relevantSkillLevel, ref float __result)
		{
			Log($"Original AI Level for Agent {agent.Name}: {__result}", LogLevel.Debug);
			__result *= AgentsInfoModel.Instance.Agents[agent.Index].Difficulty;
			Log($"Modified AI Level for Agent {agent.Name}: {__result}", LogLevel.Debug);
		}
	}
}
