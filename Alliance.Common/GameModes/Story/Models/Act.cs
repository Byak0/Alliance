using Alliance.Common.GameModes.Story.Models.Objectives;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Models
{
    public class Act
    {
        public string Name { get; }
        public string Description { get; }
        public bool LoadMap { get; }
        public string MapID { get; }
        public GameModeSettings ActSettings { get; set; }
        public VictoryLogic VictoryLogic { get; }
        public SpawnLogic SpawnLogic { get; }
        public List<ObjectiveBase> Objectives;

        public Act(string name, string desc, bool loadMap, string mapId, GameModeSettings actSettings, SpawnLogic spawnLogic, VictoryLogic victoryLogic)
        {
            Name = name;
            Description = desc;
            LoadMap = loadMap;
            MapID = mapId;
            ActSettings = actSettings;
            SpawnLogic = spawnLogic;
            VictoryLogic = victoryLogic;
            Objectives = new List<ObjectiveBase>();
        }

        public void RegisterObjectives()
        {
            foreach (ObjectiveBase objective in Objectives)
            {
                objective.Reset();
                objective.RegisterForUpdate();
            }
        }

        public void UnregisterObjectives()
        {
            foreach (ObjectiveBase objective in Objectives)
            {
                objective.UnregisterForUpdate();
            }
        }
    }
}
