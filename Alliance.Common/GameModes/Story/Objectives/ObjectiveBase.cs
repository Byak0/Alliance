using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public abstract class ObjectiveBase
	{
		[ConfigProperty(label: "Owner", tooltip: "Which side must complete this objective")]
		public BattleSideEnum Side;

		[ConfigProperty(label: "Name", tooltip: "Name of the objective")]
		public LocalizedString Name = new LocalizedString("Objective name");

		[ConfigProperty(label: "Description", tooltip: "Description of the objective")]
		public LocalizedString Description = new LocalizedString("Objective description");

		[ConfigProperty(label: "Instant win", tooltip: "If true, completing this objective will instantly win the act")]
		public bool InstantActWin;

		[ConfigProperty(label: "Required for win", tooltip: "If true, this objective must be completed to win the act")]
		public bool RequiredForActWin;

		[ConfigProperty(isEditable: false)]
		public bool Active = true;

		public ObjectiveBase(BattleSideEnum side, LocalizedString name, LocalizedString description, bool instantWin, bool requiredForWin)
		{
			Side = side;
			Name = name;
			Description = description;
			InstantActWin = instantWin;
			RequiredForActWin = requiredForWin;
			Active = true;
		}

		public ObjectiveBase() { }

		public abstract string GetProgressAsString();

		public abstract void RegisterForUpdate();

		public abstract void UnregisterForUpdate();

		public abstract bool CheckObjective();

		public abstract void Reset();
	}
}
