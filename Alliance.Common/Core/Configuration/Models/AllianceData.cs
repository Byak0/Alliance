using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Utilities;
using System;
using TaleWorlds.Core;

namespace Alliance.Common.Core.Configuration.Models
{
	/// <summary>
	/// Data for Alliance : available maps, cultures, battlesides, characters, etc.
	/// Used in editor and in-game to display options.
	/// </summary>
	public static class AllianceData
	{
		public enum DataTypes
		{
			None,
			Map,
			Culture,
			BattleSide,
			Character,
			Item,
			GameMode,
			Sounds,
			Difficulty
		}

		public enum Difficulty
		{
			Easy = 0,
			Normal = 1,
			Hard = 2,
			VeryHard = 3,
			Bannerlord = 4
		}

		public static string[] AvailableMaps() => SceneList.Scenes.ConvertAll(s => s.Name).ToArray();

		public static string[] AvailableCultures() => Factions.Instance.OrderedCultureKeys.ToArray();

		public static string[] AvailableCharacters() => Characters.Instance.CharacterStubs.ConvertAll(c => c.Name.ToString()).ToArray();

		public static string[] AvailableItems()
		{
			// Not usable when in Editor, xml not loaded...
			//return MBObjectManager.Instance.GetObjectTypeList<ItemObject>().ConvertAll(c => c.Name.ToString()).ToArray();
			return new string[] { "TODO" };
		}

		public static string[] AvailableSounds() => AudioPlayer.Instance.GetAvailableSounds();

		public static readonly string[] AvailableSides = new string[] { BattleSideEnum.Defender.ToString(), BattleSideEnum.Attacker.ToString() };

		public static readonly string[] AvailableGameModes = new string[] { "Lobby", "Scenario", "BattleRoyale", "PvC", "CvC", "CaptainX", "BattleX", "SiegeX", "Captain", "Battle", "Siege", "Skirmish" };

		public static readonly string[] AvailableDifficulties = new string[]
		{
			nameof(Difficulty.Easy),
			nameof(Difficulty.Normal),
			nameof(Difficulty.Hard),
			nameof(Difficulty.VeryHard),
			nameof(Difficulty.Bannerlord)
		};

		public static string[] GetData(DataTypes dataType)
		{
			return dataType switch
			{
				DataTypes.Map => AvailableMaps(),
				DataTypes.Culture => AvailableCultures(),
				DataTypes.BattleSide => AvailableSides,
				DataTypes.Character => AvailableCharacters(),
				DataTypes.Item => AvailableItems(),
				DataTypes.GameMode => AvailableGameModes,
				DataTypes.Sounds => AvailableSounds(),
				DataTypes.Difficulty => AvailableDifficulties,
				_ => Array.Empty<string>(),
			};
		}
	}
}
