using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors
{
	public abstract class AL_AgentBehaviorGroup
	{
		public Agent OwnerAgent
		{
			get
			{
				return Navigator.OwnerAgent;
			}
		}

		public AL_AgentBehavior ScriptedBehavior { get; private set; }

		public bool IsActive
		{
			get
			{
				return _isActive;
			}
			set
			{
				if (_isActive != value)
				{
					_isActive = value;
					if (_isActive)
					{
						OnActivate();
						return;
					}
					OnDeactivate();
				}
			}
		}

		public Mission Mission { get; private set; }

		protected AL_AgentBehaviorGroup(AL_AgentNavigator navigator, Mission mission)
		{
			Mission = mission;
			Behaviors = new List<AL_AgentBehavior>();
			Navigator = navigator;
			_isActive = false;
			ScriptedBehavior = null;
		}

		public T AddBehavior<T>() where T : AL_AgentBehavior
		{
			T t = Activator.CreateInstance(typeof(T), new object[] { this }) as T;
			if (t != null)
			{
				foreach (AL_AgentBehavior agentBehavior in Behaviors)
				{
					if (agentBehavior.GetType() == t.GetType())
					{
						return agentBehavior as T;
					}
				}
				Behaviors.Add(t);
				return t;
			}
			return t;
		}

		public T GetBehavior<T>() where T : AL_AgentBehavior
		{
			foreach (AL_AgentBehavior agentBehavior in Behaviors)
			{
				if (agentBehavior is T)
				{
					return (T)((object)agentBehavior);
				}
			}
			return default(T);
		}

		public bool HasBehavior<T>() where T : AL_AgentBehavior
		{
			using (List<AL_AgentBehavior>.Enumerator enumerator = Behaviors.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current is T)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void RemoveBehavior<T>() where T : AL_AgentBehavior
		{
			for (int i = 0; i < Behaviors.Count; i++)
			{
				if (Behaviors[i] is T)
				{
					bool isActive = Behaviors[i].IsActive;
					Behaviors[i].IsActive = false;
					if (ScriptedBehavior == Behaviors[i])
					{
						ScriptedBehavior = null;
					}
					Behaviors.RemoveAt(i);
					if (isActive)
					{
						ForceThink(0f);
					}
				}
			}
		}

		public void SetScriptedBehavior<T>() where T : AL_AgentBehavior
		{
			foreach (AL_AgentBehavior agentBehavior in Behaviors)
			{
				if (agentBehavior is T)
				{
					ScriptedBehavior = agentBehavior;
					ForceThink(0f);
					break;
				}
			}
			foreach (AL_AgentBehavior agentBehavior2 in Behaviors)
			{
				if (agentBehavior2 != ScriptedBehavior)
				{
					agentBehavior2.IsActive = false;
				}
			}
		}

		public void DisableScriptedBehavior()
		{
			if (ScriptedBehavior != null)
			{
				ScriptedBehavior.IsActive = false;
				ScriptedBehavior = null;
				ForceThink(0f);
			}
		}

		public void DisableAllBehaviors()
		{
			foreach (AL_AgentBehavior agentBehavior in Behaviors)
			{
				agentBehavior.IsActive = false;
			}
		}

		public AL_AgentBehavior GetActiveBehavior()
		{
			foreach (AL_AgentBehavior agentBehavior in Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					return agentBehavior;
				}
			}
			return null;
		}

		public virtual void Tick(float dt, bool isSimulation)
		{
		}

		public virtual void ConversationTick()
		{
		}

		public virtual void OnAgentRemoved(Agent agent)
		{
		}

		protected virtual void OnActivate()
		{
		}

		protected virtual void OnDeactivate()
		{
			foreach (AL_AgentBehavior agentBehavior in Behaviors)
			{
				agentBehavior.IsActive = false;
			}
		}

		public virtual float GetScore(bool isSimulation)
		{
			return 0f;
		}

		public virtual void ForceThink(float inSeconds)
		{
		}

		public AL_AgentNavigator Navigator;

		public List<AL_AgentBehavior> Behaviors;

		protected float CheckBehaviorTime = 5f;

		protected Timer CheckBehaviorTimer;

		private bool _isActive;
	}
}
