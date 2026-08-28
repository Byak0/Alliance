using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Linq;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Plays a <see cref="Cinematic"/>. Triggered from any action host (act conditional actions, victory logic, 
	/// or an <c>AL_TriggerAction</c> scene entity for map intros). 
	/// The cinematic may be referenced by Id (scenario-scoped, resolved from <see cref="Scenario.Cinematics"/>) or inlined.
	/// </summary>
	[Serializable]
	[PhraseTemplate("Play cinematic")]
	[PhrasePreview("Play cinematic")]
	public class PlayCinematicAction : ActionBase
	{
		[ConfigProperty(label: "Cinematic (by Id)", tooltip: "Id of a cinematic defined on this scenario. Leave empty to use the inline cinematic below.", category: "Reference")]
		public CinematicRef CinematicRef = new CinematicRef();

		[ConfigProperty(label: "Inline cinematic", tooltip: "Used when no Cinematic Id is set. Inlined here so the action is self-contained (e.g. for AL_TriggerAction map intros).", category: "Reference")]
		public Cinematic Cinematic;

		public PlayCinematicAction() { }

		/// <summary>
		/// Resolves the target cinematic: inline first, otherwise by Id from the running scenario.
		/// </summary>
		public Cinematic GetCinematic(VariableStore context)
		{
			if (Cinematic != null) return Cinematic;

			if (CinematicRef != null && !CinematicRef.IsEmpty)
			{
				Scenario scenario = ScenarioManager.Instance?.CurrentScenario;
				if (scenario?.Cinematics != null)
				{
					return scenario.Cinematics.FirstOrDefault(c => c != null && c.Id == CinematicRef.CinematicId);
				}
			}
			return null;
		}

		/// <summary>Nothing to do here - playback is server-driven (see Server_PlayCinematicAction).</summary>
		public override ActionTask Execute(VariableStore context)
		{
			return ActionTask.CompletedTask;
		}
	}
}
