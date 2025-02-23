using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch the BodyGeneratorView to make the created AgentVisuals use the correct action set and so skeleton when it is refreshed.
	/// Credits to The Old Realm's team.
	/// </summary>
	public class Patch_BodyGeneratorView
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_BodyGeneratorView));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				// Get the method to be patched
				MethodInfo original = typeof(BodyGeneratorView).GetMethod("RefreshCharacterEntityAux", BindingFlags.Instance | BindingFlags.NonPublic);

				// Apply the transpiler
				Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(Patch_BodyGeneratorView).GetMethod(nameof(Transpiler_RefreshCharacterEntityAux), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_BodyGeneratorView)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Method to get the correct action set for AgentVisuals.
		/// </summary>
		public static MBActionSet GetActionSet(BodyGeneratorView bodyGeneratorView)
		{
			var baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(bodyGeneratorView.BodyGen.Race);
			return MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, bodyGeneratorView.BodyGen.IsFemale, "_facegen");
		}

		/// <summary>
		/// Transpiler method that inserts the instructions for setting the correct action set for AgentVisualsData.
		/// </summary>
		public static IEnumerable<CodeInstruction> Transpiler_RefreshCharacterEntityAux(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
		{
			var newInstructions = new List<CodeInstruction>(instructions);
			var insertionIndex = -1;

			// Find the appropriate place to insert our new instructions
			for (int i = 0; i < newInstructions.Count; i++)
			{
				var instruction = newInstructions[i];
				// Find where AgentVisualsData is instantiated, and insert our new instructions after it
				if (instruction.opcode == OpCodes.Newobj && instruction.operand == AccessTools.Constructor(typeof(AgentVisualsData)))
				{
					insertionIndex = i + 1;
					break;
				}
			}

			if (insertionIndex < 0)
			{
				throw new ArgumentException("Cannot find instruction to insert new code. Patch: Patch_BodyGeneratorView");
			}
			else
			{
				var insertedInstructions = new List<CodeInstruction>();

				// Load "this" (the BodyGeneratorView instance) onto the stack
				insertedInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));

				// Call GetActionSet to get the appropriate action set
				insertedInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_BodyGeneratorView), nameof(GetActionSet))));

				// Callvirt to set the ActionSet on AgentVisualsData
				insertedInstructions.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AgentVisualsData), nameof(AgentVisualsData.ActionSet))));

				// Insert the new instructions at the determined location
				newInstructions.InsertRange(insertionIndex, insertedInstructions);
			}

			return newInstructions.AsEnumerable();
		}
	}
}
