using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
    public class ActCardVM : MapCardVM
    {
        public readonly Scenario Scenario;
        public readonly Act Act;

        public ActCardVM(string mapID, Scenario scenario, Act act, Action<MapCardVM> onSelect) : base(mapID, onSelect)
        {
            MapID = mapID;
            Scenario = scenario;
            Act = act;
            Name = Scenario.Name + " - " + Act.Name;
        }

        public override void RefreshValues()
        {
        }
    }
}