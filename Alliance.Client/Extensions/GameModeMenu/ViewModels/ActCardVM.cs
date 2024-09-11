using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.Utilities.SceneList;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
	public class ActCardVM : MapCardVM
	{
		public readonly Scenario Scenario;
		public readonly Act Act;

		public ActCardVM(SceneInfo mapInfo, Scenario scenario, Act act, Action<MapCardVM> onSelect) : base(mapInfo, onSelect)
		{
			MapInfo = mapInfo;
			Scenario = scenario;
			Act = act;
			Name = Scenario.Name.LocalizedText + " - " + Act.Name.LocalizedText;
		}
	}
}