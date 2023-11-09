using Alliance.Common.Core.Configuration.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.FormationEnforcer.Component
{
    /// <summary>
    /// Component keeping track of formation state for a player.
    /// </summary>
    public class FormationComponent : PeerComponent
    {
        public enum States
        {
            None,
            Formation,
            Skirmish,
            Rambo
        }

        public static FormationComponent Main => GameNetwork.MyPeer?.GetComponent<FormationComponent>();

        public event EventHandler OnStateChange;

        public States State
        {
            get
            {
                return state;
            }
            set
            {
                if (value != state)
                {
                    state = value;
                    OnStateChange?.Invoke(this, EventArgs.Empty);
                    this.GetNetworkPeer().ControlledAgent?.UpdateAgentStats();
                }
            }
        }

        public float MeleeDebuffMultiplier
        {
            get
            {
                switch (State)
                {
                    case States.Skirmish:
                        return Config.Instance.MeleeDebuffSkirm;
                    case States.Rambo:
                        return Config.Instance.MeleeDebuffRambo;
                    default:
                        return Config.Instance.MeleeDebuffForm;
                }
            }
        }

        public float DistanceDebuffMultiplier
        {
            get
            {
                switch (State)
                {
                    case States.Skirmish:
                        return Config.Instance.DistDebuffSkirm;
                    case States.Rambo:
                        return Config.Instance.DistDebuffRambo;
                    default:
                        return Config.Instance.DistDebuffForm;
                }
            }
        }

        public float AccuracyDebuffMultiplier
        {
            get
            {
                switch (State)
                {
                    case States.Skirmish:
                        return Config.Instance.AccDebuffSkirm;
                    case States.Rambo:
                        return Config.Instance.AccDebuffRambo;
                    default:
                        return Config.Instance.AccDebuffForm;
                }
            }
        }

        private States state = States.None;

        public FormationComponent()
        {
        }
    }
}

