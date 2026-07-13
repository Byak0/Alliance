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

		[ConfigProperty(label: "Optional", tooltip: "Set this objective as optional (=not required to complete the act). Do not set all your objectives as optional or the scenario will be won instantly.")]
		public bool Optional = false;

		[ConfigProperty(label: "Instant win", tooltip: "If enabled, completing this objective will instantly win the act, bypassing all other objectives.")]
		public bool InstantActWin = false;

		[ConfigProperty(isEditable: false)]
		public bool Active = true;

		public ObjectiveBase(BattleSideEnum side, LocalizedString name, LocalizedString description, bool instantWin, bool optional)
		{
			Side = side;
			Name = name;
			Description = description;
			InstantActWin = instantWin;
			Optional = optional;
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
