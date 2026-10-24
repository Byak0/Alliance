using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Target(s) of an AgentActionTrack. Exclusive modes:
	/// - AgentsFromScene: existing agents resolved from agent variables (server-side, synched natively).
	/// - NewFakeAgents: per-machine FakeAgents spawned from a formation definition - visuals only,
	///   every client spawns its own from this authored data and runs commands deterministically from the shared cinematic clock.
	/// </summary>
	[Serializable]
	public class CinematicAgentGroup
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

		[ConfigProperty(label: "Swap colors", tooltip: "Swap the culture's color1/color2 (native defender-side colors when both teams share a culture).", dependency: "?Mode=NewFakeAgents")]
		public bool SwapColors = false;

		[ConfigProperty(label: "Count", minValue: 1, maxValue: 2000, dependency: "?Mode=NewFakeAgents")]
		public int Count = 1;

		[ConfigProperty(label: "Layout", tooltip: "Grid: Rows lines of agents (agent spacing = Spacing, line spacing = Spacing x2). Circle: Rows concentric rings around a center member (agent and ring spacing = Spacing), each ring rotated half a slot. Loose: Grid base with organic jitter.", dependency: "?Mode=NewFakeAgents")]
		public CinematicFormationLayout Layout = CinematicFormationLayout.Grid;

		[ConfigProperty(label: "Rows", tooltip: "Grid: number of lines. Circle: number of concentric rings (the innermost one is a single center member).", minValue: 1, maxValue: 32, dependency: "?Mode=NewFakeAgents")]
		public int Rows = 1;

		[ConfigProperty(label: "Spacing (m)", tooltip: "Distance between agents (Grid line spacing = Spacing x2; Circle ring spacing = Spacing).", minValue: 0.5f, maxValue: 10f, dependency: "?Mode=NewFakeAgents")]
		public float Spacing = 1.5f;

		[ConfigProperty(label: "Weapons drawn", tooltip: "Fake agents wield their weapons in hand (holsters worn empty) instead of carrying them sheathed.", dependency: "?Mode=NewFakeAgents")]
		public bool WeaponsDrawn;

		[ConfigProperty(label: "Origin", tooltip: "Where the formation spawns (facing = line orientation).", dependency: "?Mode=NewFakeAgents")]
		public FrameValue Origin = new FrameValue();

		[ConfigProperty(label: "Persist after cinematic", tooltip: "Keep the fake agents alive after the cinematic ends. Persistent groups are synced to late joiners.", dependency: "?Mode=NewFakeAgents")]
		public bool Persist;

		/// <summary>The crowd spawn job of this target's last <see cref="StartCrowdSpawn"/> -
		/// despawned by <see cref="DespawnFakes"/> so relaunches never stack crowds.</summary>
		internal CrowdSpawnJob ActiveJob { get; set; }

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

		/// <summary>AgentsFromScene mode: resolves the variable slots. Server passes its context store; tools pass null (globals / authored defaults).</summary>
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

		/// <summary>NewFakeAgents mode: starts spawning this target's crowd and returns its job.
		/// The fake agents are created over several frames (see <see cref="CrowdSpawnJob.SpawnBatch"/>),
		/// so a large crowd never freezes a frame.</summary>
		public CrowdSpawnJob StartCrowdSpawn(Scene scene)
		{
			DespawnFakes();
			ActiveJob = CreateCrowdSpawnJob(scene, null, CharacterId, CultureId, Count, Layout, Rows, Spacing, WeaponsDrawn, Origin.ToFrame(), SwapColors);
			return ActiveJob;
		}

		/// <summary>Creates a crowd spawn job from raw definition data; a non-empty key also registers
		/// the crowd in <see cref="FakeAgentStore"/> (late-join sync).</summary>
		private static CrowdSpawnJob CreateCrowdSpawnJob(Scene scene, string key, string characterId, string cultureId, int count, CinematicFormationLayout layout, int rows, float spacing, bool weaponsDrawn, MatrixFrame originFrame, bool swapColors = false)
		{
			CrowdSpawnJob job = new CrowdSpawnJob(scene, characterId, cultureId, count, layout, rows, spacing, weaponsDrawn, originFrame, swapColors);
			if (!string.IsNullOrEmpty(key)) FakeAgentStore.Track(key, job.SpawnedFakeAgents);
			return job;
		}

		/// <summary>Spawns a whole crowd at once from raw definition data (used by the late-join
		/// sync, which has no CinematicAgent instance on the joining client).</summary>
		public static List<FakeAgent> SpawnFormation(Scene scene, string key, string characterId, string cultureId, int count, CinematicFormationLayout layout, int rows, float spacing, bool weaponsDrawn, MatrixFrame originFrame, bool swapColors = false)
		{
			CrowdSpawnJob job = CreateCrowdSpawnJob(scene, key, characterId, cultureId, count, layout, rows, spacing, weaponsDrawn, originFrame, swapColors);
			job.SpawnBatch(int.MaxValue);
			return job.SpawnedFakeAgents;
		}

		/// <summary>Incremental spawner for one staged crowd: <see cref="SpawnBatch"/> creates at
		/// most maxCount fake agents per call (optionally bounded by a millisecond budget), so a
		/// large crowd spawns over several frames instead of stalling one frame for seconds.
		/// Placement and equipment are index-derived, so every machine - late joiners included -
		/// spawns the exact same crowd in the same order.</summary>
		public class CrowdSpawnJob
		{
			private readonly Scene _scene;
			private readonly string _characterId;
			private readonly string _cultureId;
			private readonly int _count;
			private readonly CinematicFormationLayout _layout;
			private readonly int _rows;
			private readonly float _spacing;
			private readonly bool _weaponsDrawn;
			private readonly bool _swapColors;
			private readonly MatrixFrame _originFrame;
			private readonly AllianceData.CharacterDefinition _character;
			private int _next;

			/// <summary>Fake agents spawned so far (stable list - safe to track before completion).</summary>
			public List<FakeAgent> SpawnedFakeAgents { get; } = new List<FakeAgent>();

			/// <summary>Total fake agents this job will have spawned when complete (at least 1).</summary>
			public int TotalCount => Math.Max(1, _count);

			public bool IsComplete => _next >= Math.Max(1, _count);

			internal CrowdSpawnJob(Scene scene, string characterId, string cultureId, int count, CinematicFormationLayout layout, int rows, float spacing, bool weaponsDrawn, MatrixFrame originFrame, bool swapColors)
			{
				_scene = scene;
				_characterId = characterId;
				_cultureId = cultureId;
				_count = count;
				_layout = layout;
				_rows = rows;
				_spacing = spacing;
				_weaponsDrawn = weaponsDrawn;
				_swapColors = swapColors;
				_originFrame = originFrame;
				_character = AllianceData.Instance.GetCharacter(characterId);
			}

			/// <summary>Spawns the next up-to-maxCount fake agents of the crowd (all of them with
			/// int.MaxValue). With a budget, stops early once it is exceeded.</summary>
			public void SpawnBatch(int maxCount, double budgetMs = -1)
			{
				System.Diagnostics.Stopwatch budgetWatch = budgetMs > 0 ? System.Diagnostics.Stopwatch.StartNew() : null;
				while (!IsComplete && maxCount-- > 0)
				{
					int i = _next++;
					MatrixFrame frame = _originFrame;
					frame.origin += GetFormationOffset(_originFrame, _layout, _rows, _spacing, _count, i);
					SpawnedFakeAgents.Add(new FakeAgent(_scene, frame, _character, cultureId: _cultureId, name: $"{(string.IsNullOrEmpty(_characterId) ? "extra" : _characterId)} {i + 1}", useFullAgentVisuals: false, weaponsDrawn: _weaponsDrawn, equipmentSeed: i, swapColors: _swapColors));
					// Budget checked every 16 fakes: a heavy fake (first-time resource loads) can
					// overshoot a frame, but the job stays bounded overall.
					if (budgetWatch != null && (maxCount & 15) == 0 && budgetWatch.Elapsed.TotalMilliseconds > budgetMs) break;
				}
			}
		}

		/// <summary>Deterministic formation placement, centered on the origin frame (index-derived -
		/// every machine computes the exact same layout, late joiners included).</summary>
		public static Vec3 GetFormationOffset(MatrixFrame originFrame, CinematicFormationLayout layout, int rows, float spacing, int count, int index)
		{
			rows = Math.Max(1, rows);
			count = Math.Max(1, count);
			float side, forward;
			switch (layout)
			{
				case CinematicFormationLayout.Circle:
				{
					// Concentric rings: Rows = ring count (ring 0 = single center member, only when
					// more than one ring is used). Ring r sits at radius r*spacing with ~6r members,
					// every ring rotated by half a slot. The outer ring absorbs the remainder, its
					// radius stretching so the agent spacing is kept.
					int ringCount = Math.Max(1, rows);
					bool hasCenter = ringCount > 1;
					int remaining = index - (hasCenter ? 1 : 0);
					int ring = 1;
					int consumedInRings = 0;
					int capacity;
					while (true)
					{
						bool lastRing = ring >= ringCount;
						capacity = lastRing
							? Math.Max(1, count - (hasCenter ? 1 : 0) - consumedInRings)
							: Math.Max(1, (int)(float)Math.Round(2f * (float)Math.PI * ring));
						if (remaining < capacity || lastRing) break;
						remaining -= capacity;
						consumedInRings += capacity;
						ring++;
					}
					float radius = spacing * ring;
					float slotAngle = 2f * (float)Math.PI / capacity;
					float angle = ring * slotAngle * 0.5f + remaining * slotAngle;
					side = (float)Math.Sin(angle) * radius;
					forward = (float)Math.Cos(angle) * radius;
					break;
				}
				case CinematicFormationLayout.Loose:
				{
					// Grid base placement with deterministic jitter for an organic crowd look.
					GridPlacement(rows, spacing, count, index, out side, out forward);
					side += (Hash01(index * 3 + 1) - 0.5f) * spacing;
					forward += (Hash01(index * 3 + 2) - 0.5f) * spacing * 1.2f;
					break;
				}
				default: // Grid: Rows lines of agents; agent spacing = Spacing, line spacing = Spacing x2.
				{
					GridPlacement(rows, spacing, count, index, out side, out forward);
					break;
				}
			}
			return originFrame.rotation.f * forward + originFrame.rotation.s * side;
		}

		/// <summary>Rows lines of members; Spacing between members, Spacing x2 between lines,
		/// centered on the origin (partial last line centered too).</summary>
		private static void GridPlacement(int rows, float spacing, int count, int index, out float side, out float forward)
		{
			int lineCount = Math.Max(1, rows);
			int perLine = Math.Max(1, (count + lineCount - 1) / lineCount);
			int line = index / perLine;
			int column = index % perLine;
			int membersInLine = Math.Min(perLine, count - line * perLine);
			side = (column - (membersInLine - 1) * 0.5f) * spacing;
			forward = (line - (lineCount - 1) * 0.5f) * spacing * 2f;
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

		/// <summary>Despawns the fake agents of the current spawn job, if any - relaunching a
		/// cinematic must never stack a new crowd on top of the previous one.</summary>
		private void DespawnFakes()
		{
			if (ActiveJob != null)
			{
				foreach (FakeAgent fake in ActiveJob.SpawnedFakeAgents) fake?.Despawn();
				ActiveJob = null;
			}
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
		Circle,
		Loose
	}

	/// <summary>Spreads crowd spawn jobs across frames (shared by the in-game cinematic view and
	/// the editor preview): every <see cref="Tick"/> gives each pending job a slice of the frame
	/// budget, so several large crowds stream in together without freezing a frame.</summary>
	public class CrowdSpawnScheduler
	{
		private readonly List<(AgentActionTrack Track, CinematicAgentGroup.CrowdSpawnJob Job)> _jobs = new List<(AgentActionTrack, CinematicAgentGroup.CrowdSpawnJob)>();
		private readonly System.Diagnostics.Stopwatch _budgetWatch = new System.Diagnostics.Stopwatch();
		private readonly System.Diagnostics.Stopwatch _groupWatch = new System.Diagnostics.Stopwatch();
		private int _groupPlanned;

		/// <summary>Crowds still spawning.</summary>
		public int PendingCount => _jobs.Count;

		/// <summary>Total fake agents planned by the current group of jobs (valid once every job
		/// has been added - resets when the queue drains).</summary>
		public int TotalPlanned => _groupPlanned;

		/// <summary>Ms elapsed since the current group started spawning (for completion reports).</summary>
		public long ElapsedMs => _groupWatch.ElapsedMilliseconds;

		/// <summary>Registers a spawn job. The first job of a group resets the group counters.</summary>
		public void Add(AgentActionTrack track, CinematicAgentGroup.CrowdSpawnJob job)
		{
			if (_jobs.Count == 0)
			{
				_groupPlanned = 0;
				_groupWatch.Restart();
			}
			_jobs.Add((track, job));
			_groupPlanned += job.TotalCount;
		}

		/// <summary>Advances every pending job within the frame budget. Returns true when all
		/// jobs just completed (read <see cref="TotalPlanned"/> / <see cref="ElapsedMs"/> for the
		/// completion report - they reset with the next group).</summary>
		public bool Tick(double perJobBudgetMs, double frameBudgetMs)
		{
			if (_jobs.Count == 0) return false;
			_budgetWatch.Restart();
			int index = 0;
			while (index < _jobs.Count)
			{
				CinematicAgentGroup.CrowdSpawnJob job = _jobs[index].Job;
				job.SpawnBatch(int.MaxValue, perJobBudgetMs);
				if (job.IsComplete) _jobs.RemoveAt(index);
				else index++;
				if (_jobs.Count > 0 && _budgetWatch.ElapsedMilliseconds > frameBudgetMs) break;
			}
			return _jobs.Count == 0;
		}

		/// <summary>Cancels matching jobs: despawns their partial crowd and drops them from the
		/// queue (left in, they would keep spawning an obsolete crowd on top of its replacement).</summary>
		public void Cancel(Func<AgentActionTrack, bool> cancelIf)
		{
			for (int i = _jobs.Count - 1; i >= 0; i--)
			{
				if (!cancelIf(_jobs[i].Track)) continue;
				foreach (FakeAgent fake in _jobs[i].Job.SpawnedFakeAgents) fake?.Despawn();
				_jobs.RemoveAt(i);
			}
		}

		/// <summary>Drops every job without despawning (the caller already disposed of the fakes).</summary>
		public void Clear() => _jobs.Clear();
	}
}
