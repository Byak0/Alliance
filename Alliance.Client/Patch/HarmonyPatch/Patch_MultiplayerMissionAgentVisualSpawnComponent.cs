using Alliance.Client.Patch.Behaviors;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
	/// <summary>
	/// Patches the MultiplayerMissionAgentVisualSpawnComponent to call our custom AllianceAgentVisualSpawnComponent instead.
	/// This is needed to allow previewing agents of custom races.
	/// The MultiplayerMissionAgentVisualSpawnComponent is called everywhere in the native code so replacing the component "cleanly" is not possible.
	/// </summary>
	class Patch_AllianceAgentVisualSpawnComponent
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_AllianceAgentVisualSpawnComponent));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(MultiplayerMissionAgentVisualSpawnComponent).GetMethod(nameof(MultiplayerMissionAgentVisualSpawnComponent.SpawnAgentVisualsForPeer),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_AllianceAgentVisualSpawnComponent).GetMethod(
						nameof(Prefix_SpawnAgentVisualsForPeer), BindingFlags.Static | BindingFlags.Public)));
				Harmony.Patch(
					typeof(MultiplayerMissionAgentVisualSpawnComponent).GetMethod(nameof(MultiplayerMissionAgentVisualSpawnComponent.RemoveAgentVisuals),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_AllianceAgentVisualSpawnComponent).GetMethod(
						nameof(Prefix_RemoveAgentVisuals), BindingFlags.Static | BindingFlags.Public)));
				Harmony.Patch(
					typeof(MultiplayerMissionAgentVisualSpawnComponent).GetMethod(nameof(MultiplayerMissionAgentVisualSpawnComponent.OnMyAgentSpawned),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_AllianceAgentVisualSpawnComponent).GetMethod(
						nameof(Prefix_OnMyAgentSpawned), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log("Alliance - ERROR in " + nameof(Patch_AllianceAgentVisualSpawnComponent), LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		public static bool Prefix_SpawnAgentVisualsForPeer(MissionPeer missionPeer, AgentBuildData buildData, int selectedEquipmentSetIndex = -1, bool isBot = false, int totalTroopCount = 0)
		{
			Mission.Current.GetMissionBehavior<AllianceAgentVisualSpawnComponent>().SpawnAgentVisualsForPeer(missionPeer, buildData, selectedEquipmentSetIndex, isBot, totalTroopCount);

			return false; // Skip original method
		}

		public static bool Prefix_RemoveAgentVisuals(MissionPeer missionPeer, bool sync = false)
		{
			Mission.Current.GetMissionBehavior<AllianceAgentVisualSpawnComponent>().RemoveAgentVisuals(missionPeer, sync);

			return false; // Skip original method
		}

		public static bool Prefix_OnMyAgentSpawned()
		{
			Mission.Current.GetMissionBehavior<AllianceAgentVisualSpawnComponent>().OnMyAgentSpawned();

			return false; // Skip original method
		}
	}
}

