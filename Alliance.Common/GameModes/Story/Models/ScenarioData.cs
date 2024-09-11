using Alliance.Common.Utilities;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Get available data for different types of scenario data.
	/// TODO: Find a way to load data from xml files instead of hardcoded lists.
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

		public static string[] AvailableCultures()
		{
			// Not usable when in Editor, xml not loaded...
			//return Factions.Instance.AvailableCultures.Keys.ToArray();
			return new string[] { "vlandia", "battania", "empire", "sturgia", "aserai", "khuzait", "rohan", "isengard", "autochtone", "explorator" };
		}

		public static string[] AvailableSides()
		{
			return new string[] { BattleSideEnum.Attacker.ToString(), BattleSideEnum.Defender.ToString() };
		}

		public static string[] AvailableCharacters()
		{
			// Not usable when in Editor, xml not loaded...
			//return MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>().ConvertAll(c => c.Name.ToString()).ToArray();
			return new string[] { "TODO" };
		}

		public static string[] AvailableItems()
		{
			// Not usable when in Editor, xml not loaded...
			//return MBObjectManager.Instance.GetObjectTypeList<ItemObject>().ConvertAll(c => c.Name.ToString()).ToArray();
			return new string[] { "TODO" };
		}

		public static string[] AvailableGameModes()
		{
			// Not usable when in Editor, game modes are loaded in Client or Server.
			// return Module.CurrentModule.GetMultiplayerGameTypes().Select(g => g.GameType).ToArray();
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
