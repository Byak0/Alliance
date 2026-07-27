using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a game.
	/// </summary>
	[Serializable]
	public class StartGameAction : ActionBase
	{
		[ConfigProperty(label: "Map ID", tooltip: "ID of the map to load", dataType: AllianceData.DataTypes.Map)]
		public string MapID;

		[ConfigProperty(label: "Settings", tooltip: "Define native and mod settings.")]
		public GameModeSettings Settings;

		public StartGameAction(string mapID, GameModeSettings settings)
		{
			MapID = mapID;
			Settings = settings;
		}

		public StartGameAction() { }

		public override ActionTask Execute(VariableStore context) => ActionTask.CompletedTask;
	}
}