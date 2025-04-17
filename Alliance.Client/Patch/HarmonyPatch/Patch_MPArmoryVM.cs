using Alliance.Client.Patch.Behaviors;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
	/// <summary>
	/// Patches the MPArmoryVM to call our custom AllianceLobbyClassFilterVM instead.
	/// This is needed to allow previewing custom factions in the Lobby.
	/// Thanks to DarthKiller454 for the code.
	/// </summary>
	class Patch_MPArmoryVM
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MPArmoryVM));
		private static bool _patched;

		public static bool Patch()
		{
			try
			{
				if (_patched) return false;
				_patched = true;

				Harmony.Patch(
					AccessTools.Constructor(typeof(MPArmoryVM), new Type[]
					{
						typeof(Action<BasicCharacterObject>),
						typeof(Action<MPArmoryCosmeticItemBaseVM>),
						typeof(Func<string>)
					}),
					postfix: new HarmonyMethod(typeof(Patch_MPArmoryVM).GetMethod(
						nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic))
				);
			}
			catch (Exception ex)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MPArmoryVM)}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		private static void Postfix(MPArmoryVM __instance)
		{
			// Wrap the OnSelectedClassChanged method to fit the Action<MPLobbyClassFilterClassItemVM, bool> delegate
			var onSelectionChanged = GetOnSelectedChangeDelegate(__instance);

			// Pass the wrapped delegate to the custom filter
			__instance.ClassFilter = CustomLobbyClassFilterVM.CreateCustomFilter(__instance);
		}

		private static Action<MPLobbyClassFilterClassItemVM, bool> GetOnSelectedChangeDelegate(object instance)
		{
			var method = instance.GetType().GetMethod("OnSelectedClassChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method == null)
				throw new MissingMethodException("Could not find OnSelectedClassChanged on MPArmoryVM");

			return (item, selected) => method.Invoke(instance, new object[] { item, selected });
		}
	}
}