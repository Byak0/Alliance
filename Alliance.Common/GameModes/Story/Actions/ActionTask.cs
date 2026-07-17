using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Actions
{
	public abstract class ActionTask
	{
		public bool IsCompleted { get; protected set; }
		public event Action OnTaskCompleted;
		public abstract void Tick(float dt);

		public static readonly ActionTask CompletedTask = new CompletedTask();

		protected void OnCompleted()
		{
			IsCompleted = true;
			OnTaskCompleted?.Invoke();
		}
	}

	public sealed class CompletedTask : ActionTask
	{
		public CompletedTask()
		{
			IsCompleted = true;
		}

		public override void Tick(float dt) { }
	}

	public sealed class WaitTask : ActionTask
	{
		private float _elapsed;
		public float Duration { get; }

		public WaitTask(float duration)
		{
			Duration = duration;
			_elapsed = 0f;
		}

		public override void Tick(float dt)
		{
			if (IsCompleted) return;
			_elapsed += dt;
			if (_elapsed >= Duration)
			{
				OnCompleted();
			}
		}
	}

	public sealed class SequenceTask : ActionTask
	{
		private readonly List<ActionBase> _actions;
		private int _index;
		private ActionTask _currentTask;

		public SequenceTask(List<ActionBase> actions)
		{
			_actions = actions;
			_index = 0;
			if (_actions.Count == 0)
			{
				IsCompleted = true;
			}
		}

		public override void Tick(float dt)
		{
			if (IsCompleted) return;

			if (_index >= _actions.Count)
			{
				OnCompleted();
				return;
			}

			if (_currentTask == null)
			{
				_currentTask = _actions[_index].Execute();
			}

			_currentTask.Tick(dt);

			if (_currentTask.IsCompleted)
			{
				_index++;
				_currentTask = null;
			}
		}
	}
}
