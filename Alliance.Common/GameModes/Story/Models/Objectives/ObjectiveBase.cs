using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models.Objectives
{
    public abstract class ObjectiveBase
    {
        public BattleSideEnum Side { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool InstantActWin { get; set; }
        public bool RequiredForActWin { get; set; }
        public bool Active { get; set; }

        public ObjectiveBase(BattleSideEnum side, string name, string description, bool instantWin, bool requiredForWin)
        {
            Side = side;
            Name = name;
            Description = description;
            InstantActWin = instantWin;
            RequiredForActWin = requiredForWin;
            Active = true;
        }

        public abstract string GetProgressAsString();

        public abstract void RegisterForUpdate();

        public abstract void UnregisterForUpdate();

        public abstract bool CheckObjective();

        public abstract void Reset();
    }
}
