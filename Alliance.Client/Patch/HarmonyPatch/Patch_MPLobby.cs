using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
	/// <summary>
	/// Patches the MPLobbyClassFilterVM to allow previewing custom factions in the Lobby.
	/// Thanks to DarthKiller454 for the original code.
	/// </summary>
	class Patch_MPLobby
	{
		private static List<string> _randomLobbyAnimation = new List<string>
		{
			"act_idle_unarmed_1",
			"act_idle_unarmed_1",
			"act_idle_unarmed_1",
			"act_idle_unarmed_1",
			"act_idle_unarmed_1",
			"act_idle_unarmed_1",
			"act_taunt_cheer_3",
			"act_cheering_high_01",
			"act_cheering_low_01",
			"act_arena_winner_1",
			"act_arena_spectator",
			"act_childhood_toddler_vigor",
			"act_childhood_toddler_endurance",
			"act_childhood_toddler_social",
			"act_main_story_conspirator_kneel_down_1_continue",
			"act_main_story_conspirator_kneel_down_2_continue",
			"act_main_story_conspirator_kneel_down_3_continue",
			"act_cutscene_kingdom_made_pose_02",
			"act_cutscene_kingdom_made_pose_04",
			"act_cutscene_kingdom_made_pose_05",
			"act_cutscene_kingdom_made_pose_06",
			"act_cutscene_join_faction_crew_d_loop",
			"act_character_creation_nord_shipwrights_father",
			"act_character_creation_nord_shipwrights_mother",
			"act_talk_to_1"
		};

		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MPLobby));
		private static bool _patched;

		public static bool Patch()
		{
			try
			{
				if (_patched) return false;
				_patched = true;

				Harmony.Patch(AccessTools.Constructor(typeof(MPLobbyClassFilterVM), new Type[]
					{
						typeof(Action<MPLobbyClassFilterClassItemVM, bool>)
					}),
					postfix: new HarmonyMethod(typeof(Patch_MPLobby).GetMethod(
						nameof(Postfix_MPLobbyClassFilterVM), BindingFlags.Static | BindingFlags.NonPublic))
				);

				Harmony.Patch(AccessTools.Method(typeof(MPArmoryHeroPreviewVM), nameof(MPArmoryHeroPreviewVM.SetCharacter)),
					postfix: new HarmonyMethod(typeof(Patch_MPLobby).GetMethod(
						nameof(Postfix_HeroPreviewSetCharacter), BindingFlags.Static | BindingFlags.NonPublic)));
			}
			catch (Exception ex)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MPLobby)}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		// Add all factions to the class filter VM
		private static void Postfix_MPLobbyClassFilterVM(MPLobbyClassFilterVM __instance)
		{
			// Create delegates for privates methods, we need to pass them to the faction items
			Action<MPLobbyClassFilterFactionItemVM> _onFactionFilterChanged =
				(item) => AccessTools.Method(typeof(MPLobbyClassFilterVM), "OnFactionFilterChanged").Invoke(__instance, new object[] { item });
			Action<MPLobbyClassFilterClassItemVM> _onSelectionChanged =
				(item) => AccessTools.Method(typeof(MPLobbyClassFilterVM), "OnSelectionChange").Invoke(__instance, new object[] { item });

			// Clear factions from original constructor and add all the factions we want
			__instance.Factions.Clear();
			foreach (string factionName in Common.Core.Utils.Factions.Instance.AvailableCultures.Keys)
			{
				__instance.Factions.Add(new MPLobbyClassFilterFactionItemVM(factionName, true, _onFactionFilterChanged, _onSelectionChanged));
			}

			// Refresh instance values
			__instance.ActiveClassGroups = new MBBindingList<MPLobbyClassFilterClassGroupItemVM>();
			__instance.Factions[0].IsActive = true;
			__instance.RefreshValues();
		}

		// Randomize idle action on armory for fun
		private static void Postfix_HeroPreviewSetCharacter(MPArmoryHeroPreviewVM __instance)
		{
			__instance.HeroVisual.IdleAction = _randomLobbyAnimation.GetRandomElement();
		}
	}
}