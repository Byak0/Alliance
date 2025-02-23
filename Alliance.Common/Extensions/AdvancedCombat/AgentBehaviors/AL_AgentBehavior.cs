using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors
{
	public abstract class AL_AgentBehavior
	{
		public AL_AgentNavigator Navigator
		{
			get
			{
				return BehaviorGroup.Navigator;
			}
		}

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

		public Agent OwnerAgent
		{
			get
			{
				return Navigator.OwnerAgent;
			}
		}

		public Mission Mission { get; private set; }

		protected AL_AgentBehavior(AL_AgentBehaviorGroup behaviorGroup)
		{
			Mission = behaviorGroup.Mission;
			CheckTime = 40f + MBRandom.RandomFloat * 20f;
			BehaviorGroup = behaviorGroup;
			_isActive = false;
		}

		public virtual float GetAvailability(bool isSimulation)
		{
			return 0f;
		}

		public virtual void Tick(float dt, bool isSimulation)
		{
		}

		public virtual void ConversationTick()
		{
		}

		protected virtual void OnActivate()
		{
		}

		protected virtual void OnDeactivate()
		{
		}

		public virtual bool CheckStartWithBehavior()
		{
			return false;
		}

		public virtual void OnSpecialTargetChanged()
		{
		}

		public virtual void SetCustomWanderTarget(UsableMachine customUsableMachine)
		{
		}

		public virtual void OnAgentRemoved(Agent agent)
		{
		}

		public abstract string GetDebugInfo();

		public float CheckTime = 15f;

		protected readonly AL_AgentBehaviorGroup BehaviorGroup;

		private bool _isActive;
	}
}
