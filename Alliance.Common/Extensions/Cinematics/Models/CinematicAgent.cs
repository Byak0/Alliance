using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Who (or what) an AgentActionTrack stages. Exclusive modes:
	/// - AgentsFromScene: existing agents resolved from agent variables (server-side, replicated natively).
	/// - NewFakeAgents: per-machine FakeAgents spawned from a formation definition - visuals only,
	///   never replicated; every client spawns its own from this authored data and runs commands
	///   deterministically from the shared cinematic clock.
	/// Also owns the target manipulation methods (play/move/teleport/visibility) so executors never
	/// branch on the mode themselves.
	/// </summary>
	[Serializable]
	public class CinematicAgent
	{
		[ConfigProperty(label: "Target mode", tooltip: "AgentsFromScene: stage existing agents resolved from variables. NewFakeAgents: spawn per-client fake agents (visuals only, never replicated) from the formation definition below.")]
		public CinematicAgentMode Mode = CinematicAgentMode.AgentsFromScene;

		[ConfigProperty(label: "Agents", tooltip: "Variables or functions holding the Agents to stage. Resolved by the server when the cinematic starts; commands apply to every resolved agent.", dependency: "?Mode=AgentsFromScene")]
		[SyncToClient]
		public List<ValueSource<Agent>> Agents = new List<ValueSource<Agent>>();

		[ConfigProperty(label: "Stand-in character", tooltip: "Character used by the kit preview stand-ins for these true agents (full fidelity requires characters to be loaded). Empty = generic human.", dataType: AllianceData.DataTypes.Character, dependency: "?Mode=AgentsFromScene")]
		public string PreviewCharacterId = "";

		[ConfigProperty(label: "Character", tooltip: "Character the fake agents are dressed as.", dataType: AllianceData.DataTypes.Character, dependency: "?Mode=NewFakeAgents")]
		public string CharacterId = "";

		[ConfigProperty(label: "Culture", tooltip: "Culture providing the clothing colors (team side simulation).", dataType: AllianceData.DataTypes.Culture, dependency: "?Mode=NewFakeAgents")]
		public string CultureId = "";

		[ConfigProperty(label: "Count", minValue: 1, maxValue: 64, dependency: "?Mode=NewFakeAgents")]
		public int Count = 1;

		[ConfigProperty(label: "Layout", tooltip: "Grid: rows x columns. Blob: tight cluster around the origin. Chaotic: scattered around it. (Rows = 1 gives a single line.)", dependency: "?Mode=NewFakeAgents")]
		public CinematicFormationLayout Layout = CinematicFormationLayout.Grid;

		[ConfigProperty(label: "Rows", tooltip: "Lines used by the Grid layout.", minValue: 1, maxValue: 16, dependency: "?Mode=NewFakeAgents")]
		public int Rows = 1;

		[ConfigProperty(label: "Spacing (m)", minValue: 0.5f, maxValue: 10f, dependency: "?Mode=NewFakeAgents")]
		public float Spacing = 1.5f;

		[ConfigProperty(label: "Weapons drawn", tooltip: "Fake agents wield their weapons in hand (holsters worn empty) instead of carrying them sheathed.", dependency: "?Mode=NewFakeAgents")]
		public bool WeaponsDrawn;

		[ConfigProperty(label: "Origin", tooltip: "Where the formation spawns (facing = line orientation).", dependency: "?Mode=NewFakeAgents")]
		public FrameValue Origin = new FrameValue();

		[ConfigProperty(label: "Persist after cinematic", tooltip: "Keep the fake agents alive after the cinematic ends. Persistent groups are synced to late joiners.", dependency: "?Mode=NewFakeAgents")]
		public bool Persist;

		/// <summary>Fake agents currently spawned for this target (per-machine, never serialized).</summary>
		[XmlIgnore]
		internal readonly List<FakeAgent> SpawnedFakes = new List<FakeAgent>();

		[XmlIgnore]
		public bool IsTrueMode => Mode == CinematicAgentMode.AgentsFromScene;

		[XmlIgnore]
		public bool IsStagedMode => Mode == CinematicAgentMode.NewFakeAgents;

		/// <summary>Short label for the timeline lane hyperlink.</summary>
		[XmlIgnore]
		public string EditorLabel
		{
			get
			{
				if (IsStagedMode)
					return $"Fake agents: {(string.IsNullOrEmpty(CharacterId) ? "default" : CharacterId)} x{Math.Max(1, Count)}";

				List<string> variables = Agents?
					.OfType<VariableValue<Agent>>()
					.Where(v => !string.IsNullOrEmpty(v.VariableName))
					.Select(v => v.VariableName).ToList();
				return variables != null && variables.Count > 0
					? "Variables: " + string.Join(", ", variables)
					: "Agents from scene (unresolved)";
			}
		}

		/// <summary>AgentsFromScene mode: resolves the variable slots. Server passes its context store;
		/// tools pass null (globals / authored defaults).</summary>
		public List<Agent> ResolveAgents(VariableStore context)
		{
			List<Agent> result = new List<Agent>();
			foreach (ValueSource<Agent> slot in Agents ?? Enumerable.Empty<ValueSource<Agent>>())
			{
				Agent agent = slot?.Resolve(context);
				if (agent != null) result.Add(agent);
			}
			return result;
		}

		/// <summary>NewFakeAgents mode: spawns Count FakeAgents in the configured layout at the authored origin (per-machine).</summary>
		public List<FakeAgent> SpawnFakes(Scene scene)
		{
			DespawnFakes();
			List<FakeAgent> fakes = SpawnFormation(scene, null, CharacterId, CultureId, Count, Layout, Rows, Spacing, WeaponsDrawn, Origin.ToFrame());
			SpawnedFakes.AddRange(fakes);
			return fakes;
		}

		/// <summary>Spawns a staged-extras formation from raw definition data (used by both the model
		/// and the late-join sync, which has no CinematicAgent instance on the joining client).</summary>
		public static List<FakeAgent> SpawnFormation(Scene scene, string key, string characterId, string cultureId, int count, CinematicFormationLayout layout, int rows, float spacing, bool weaponsDrawn, MatrixFrame originFrame)
		{
			List<FakeAgent> fakes = new List<FakeAgent>();
			NativeMpData.CharacterDefinition character = NativeMpData.Instance.GetCharacter(characterId);
			int total = Math.Max(1, count);
			for (int i = 0; i < total; i++)
			{
				MatrixFrame frame = originFrame;
				frame.origin += GetFormationOffset(originFrame, layout, rows, spacing, count, i);
				// Staged extras are crowds: always the cheap raw path (AgentVisuals are reserved
				// for the per-track stand-in previews). Seed = member index: everyone picks the
				// same roster mix and late joiners dress alike.
				fakes.Add(new FakeAgent(scene, frame, character, cultureId: cultureId, name: $"{(string.IsNullOrEmpty(characterId) ? "extra" : characterId)} {i + 1}", useFullAgentVisuals: false, weaponsDrawn: weaponsDrawn, equipmentSeed: i));
			}
			if (!string.IsNullOrEmpty(key)) FakeAgentStore.Track(key, fakes);
			return fakes;
		}

		/// <summary>Deterministic formation placement, centered on the origin frame (index-derived -
		/// every machine computes the exact same layout, late joiners included).</summary>
		public static Vec3 GetFormationOffset(MatrixFrame originFrame, CinematicFormationLayout layout, int rows, float spacing, int count, int index)
		{
			rows = Math.Max(1, rows);
			float side, forward;
			switch (layout)
			{
				case CinematicFormationLayout.Blob:
				case CinematicFormationLayout.Chaotic:
				{
					Vec2 local = ScatteredOffset(layout, spacing, index);
					side = local.x;
					forward = local.y;
					break;
				}
				default: // Grid
				{
					// Rows = members per line; lines stack forward. Centered on the origin.
					int perLine = Math.Min(rows, Math.Max(1, count));
					int line = index / perLine;
					int column = index % perLine;
					int lineCount = (count + perLine - 1) / perLine;
					side = (column - (perLine - 1) * 0.5f) * spacing;
					forward = (line - (lineCount - 1) * 0.5f) * spacing;
					break;
				}
			}
			return originFrame.rotation.f * forward + originFrame.rotation.s * side;
		}

		/// <summary>Raw index-derived scatter for the loose layouts, relaxed so members keep some
		/// distance from each other when space permits: every member is pushed at least
		/// 0.8 * spacing away from the members before it (bounded passes - the area stays compact).
		/// Fully deterministic: offsets 0..index are recomputed identically on every machine.</summary>
		private static Vec2 ScatteredOffset(CinematicFormationLayout layout, float spacing, int index)
		{
			Vec2[] offsets = new Vec2[index + 1];
			float spread = layout == CinematicFormationLayout.Blob ? 2f : 4f;
			float minDistance = spacing * 0.8f;
			for (int i = 0; i <= index; i++)
			{
				Vec2 raw = new Vec2(
					(Hash01(i * 3 + 1) - 0.5f) * spacing * spread,
					(Hash01(i * 3 + 2) - 0.5f) * spacing * spread);
				if (i == 0)
				{
					offsets[0] = raw;
					continue;
				}
				Vec2 placed = raw;
				for (int pass = 0; pass < 4; pass++)
				{
					for (int j = 0; j < i; j++)
					{
						Vec2 delta = placed - offsets[j];
						float distance = delta.Length;
						if (distance >= minDistance) continue;
						if (distance > 0.0001f) placed = offsets[j] + delta / distance * minDistance;
						else placed += new Vec2(minDistance, minDistance * 0.25f); // exact overlap: deterministic kick
					}
				}
				offsets[i] = placed;
			}
			return offsets[index];
		}

		/// <summary>The frame a specific formation member moves to when the group destination is
		/// <paramref name="anchorFrame"/> - keeps the formation intact while traveling.</summary>
		public MatrixFrame GetMemberFrame(MatrixFrame anchorFrame, int index)
		{
			MatrixFrame frame = anchorFrame;
			frame.origin += GetFormationOffset(anchorFrame, Layout, Rows, Spacing, Count, index);
			return frame;
		}

		/// <summary>Deterministic per-index scatter in [0..1) - no per-machine randomness (late joiners must agree).</summary>
		private static float Hash01(int i)
		{
			float value = Math.Abs((float)Math.Sin(i * 12.9898f) * 43758.5453f);
			return value - (float)Math.Floor(value);
		}

		public void DespawnFakes()
		{
			foreach (FakeAgent fake in SpawnedFakes) fake?.Despawn();
			SpawnedFakes.Clear();
		}
	}

	public enum CinematicAgentMode
	{
		AgentsFromScene,
		NewFakeAgents
	}

	public enum CinematicFormationLayout
	{
		Grid,
		Blob,
		Chaotic
	}
}
