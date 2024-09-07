using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Get available data for different types of scenario data.
	/// </summary>
	public static class ScenarioData
	{
		public enum DataTypes
		{
			None,
			Map,
			Culture,
			BattleSide,
			Character,
			Item,
			GameMode
		}

		public static string[] AvailableMaps() => SceneList.Scenes.ConvertAll(s => s.Name).ToArray();
		public static string[] AvailableCultures() => Factions.Instance.AvailableCultures.Keys.ToArray();
		public static string[] AvailableSides() => new string[] { BattleSideEnum.Attacker.ToString(), BattleSideEnum.Defender.ToString() };
		public static string[] AvailableCharacters()
		{
			// Todo : move this to a more appropriate place. Editor doesn't load these by default
			//MBObjectManager.Instance.LoadXML("NPCCharacter");
			//MBObjectManager.Instance.LoadXML("MPCharacters", false);
			List<string> list = new List<string>();
			foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
			{
				if (character.IsPlayerCharacter)
				{
					list.Add(character.StringId);
				}
			}
			return list.ToArray();
		}
		public static string[] AvailableItems() => MBObjectManager.Instance.GetObjectTypeList<ItemObject>().ConvertAll(c => c.Name.ToString()).ToArray();
		public static string[] AvailableGameModes() => GetAvailableGameModes();

		private static string[] GetAvailableGameModes()
		{
			// Not usable when in Editor, game modes are loaded in Client or Server.
			// return Module.CurrentModule.GetMultiplayerGameTypes().Select(g => g.GameType).ToArray();

			// Return hardcoded list instead.
			return new string[] { "Lobby", "Scenario", "BattleRoyale", "PvC", "CvC", "CaptainX", "BattleX", "SiegeX", "Captain", "Battle", "Siege", "Skirmish" };
		}

		public static string[] GetData(DataTypes dataType)
		{
			return dataType switch
			{
				DataTypes.Map => AvailableMaps(),
				DataTypes.Culture => AvailableCultures(),
				DataTypes.BattleSide => AvailableSides(),
				DataTypes.Character => AvailableCharacters(),
				DataTypes.Item => AvailableItems(),
				DataTypes.GameMode => AvailableGameModes(),
				_ => new string[] { },
			};
		}
	}
}
