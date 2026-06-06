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
			// Result is multiply by cubed of difficulty 
			__result *= (float)Math.Pow(AgentsInfoModel.Instance.Agents[agent.Index].Difficulty, 3);
		}
	}
}
