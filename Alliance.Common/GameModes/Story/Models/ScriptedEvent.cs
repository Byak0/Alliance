using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A scripted event is a set of conditions and actions that are triggered when the conditions are met.
	/// </summary>
	[PhrasePreview("{Name} — {#Conditions} condition(s) → {#Actions} action(s)")]
	public class ScriptedEvent
	{
		public string Name = "Scripted Event";
		[ConfigProperty(label: "Conditions", tooltip: "If multiple conditions are set, they must all be true to trigger the actions.")]
		public List<Condition> Conditions = new List<Condition>();
		[ConfigProperty(label: "Actions", tooltip: "Actions triggered when conditions are met.")]
		public List<ActionBase> Actions = new List<ActionBase>();
		[ConfigProperty(label: "Enabled", tooltip: "Enable or disable the scripted event.")]
		public bool Enabled = true;
		[ConfigProperty(label: "One Time Only", tooltip: "If true, the scripted event will only trigger once.")]
		public bool OneTimeOnly = false;
		[ConfigProperty(label: "Refresh Delay", tooltip: "Delay between condition checks in seconds. Longer delays are preferable for performance.")]
		public float RefreshDelay = 1f;

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		internal float _refreshTimer = 0f;
		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		internal bool _enabled = false;

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		public WeakGameEntity ParentEntity = WeakGameEntity.Invalid;

		private enum PipelineState { Idle, Running, Done }
		private PipelineState _pipelineState = PipelineState.Idle;
		private int _actionIndex;
		private ActionTask _currentTask;
		private VariableStore _localContext;

		public ScriptedEvent() { }

		public void Register(WeakGameEntity entity)
		{
			_enabled = Enabled;
			_localContext = new VariableStore();
			_pipelineState = PipelineState.Idle;
			_currentTask = null;

			if (entity != null)
			{
				ParentEntity = entity;
			}
			foreach (Condition condition in Conditions)
			{
				condition.Register(entity);
			}
			foreach (ActionBase action in Actions)
			{
				action.Register(entity);
			}
		}

		/// <summary>
		/// Check if the conditions are met and execute the actions if they are.
		/// Uses a cooperative async pipeline so that actions with duration (e.g. WaitAction)
		/// are awaited over multiple ticks. The pipeline is anti-reentrant: while running,
		/// condition re-evaluation is blocked.
		/// </summary>
		public void Tick(float dt)
		{
			if (!_enabled) return;

			// --- Pipeline running: tick the current action ---
			if (_pipelineState == PipelineState.Running)
			{
				_currentTask?.Tick(dt, _localContext);

				if (_currentTask != null && _currentTask.IsCompleted)
				{
					_actionIndex++;
					if (_actionIndex < Actions.Count)
					{
						_currentTask = Actions[_actionIndex].Execute(_localContext);
					}
					else
					{
						_pipelineState = PipelineState.Done;
						_currentTask = null;
					}
				}
				return;
			}

			// --- Pipeline done: handle completion logic ---
			if (_pipelineState == PipelineState.Done)
			{
				if (OneTimeOnly)
				{
					_enabled = false;
				}
				_pipelineState = PipelineState.Idle;
				_localContext = new VariableStore();
				return;
			}

			// --- Idle: check conditions (throttled by RefreshDelay) ---
			_refreshTimer += dt;
			if (_refreshTimer < RefreshDelay) return;
			_refreshTimer = 0f;

			bool conditionsMet = true;
			foreach (Condition condition in Conditions)
			{
				if (!condition.Evaluate(_localContext))
				{
					conditionsMet = false;
					break;
				}
			}

			if (conditionsMet)
			{
				// Start the async pipeline
				_pipelineState = PipelineState.Running;
				_actionIndex = 0;
				_currentTask = Actions.Count > 0 ? Actions[0].Execute(_localContext) : ActionTask.CompletedTask;

				// Tick the first task immediately
				_currentTask?.Tick(0f, _localContext);
				if (_currentTask != null && _currentTask.IsCompleted)
				{
					_actionIndex++;
					if (_actionIndex < Actions.Count)
					{
						_currentTask = Actions[_actionIndex].Execute(_localContext);
					}
					else
					{
						_pipelineState = PipelineState.Done;
						_currentTask = null;
						if (OneTimeOnly) _enabled = false;
					}
				}
			}
		}
	}
}
