using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Extensions.Cinematics.Models;
using System;
using System.Linq;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Plays a Cinematic, referenced by name (from the scenario) or inlined (for AL_TriggerAction map
	/// intros). Only the server override does anything: it broadcasts a PlayCinematicMessage and
	/// registers the authoritative server timeline; clients play from the message.
	/// </summary>
	[Serializable]
	[PhraseTemplate("Play cinematic")]
	[PhrasePreview("Play cinematic")]
	public class PlayCinematicAction : ActionBase
	{
		[ConfigProperty(label: "Cinematic", tooltip: "Cinematic defined on this scenario. Leave empty to use the inline cinematic below.")]
		[CinematicRef]
		public string CinematicName = "";

		[ConfigProperty(label: "Inline cinematic", tooltip: "Self-contained copy used when no cinematic name is set, so the action works without a scenario (e.g. AL_TriggerAction map intros).")]
		public Cinematic Cinematic;

		public PlayCinematicAction() { }

		/// <summary>Network addressing mode: inline cinematics are addressed by this action's
		/// (ScopeId, ActionId) registry entry, name-referenced ones by their name.</summary>
		public bool UseActionRef => Cinematic != null;

		/// <summary>Resolves the cinematic to play: inline copy first, otherwise by name. Null if neither.</summary>
		public Cinematic GetCinematic()
		{
			if (Cinematic != null) return Cinematic;

			if (!string.IsNullOrEmpty(CinematicName))
			{
				Scenario scenario = ScenarioManager.Instance?.CurrentScenario;
				if (scenario?.Cinematics != null)
				{
					return scenario.Cinematics.FirstOrDefault(c => c != null && c.Name == CinematicName);
				}
			}
			return null;
		}

		/// <summary>Nothing to do on clients - playback is driven by PlayCinematicMessage (see Server_PlayCinematicAction).</summary>
		public override ActionTask Execute(VariableStore context)
		{
			return ActionTask.CompletedTask;
		}
	}
}


