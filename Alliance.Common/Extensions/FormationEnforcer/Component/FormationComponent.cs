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
        public static FormationComponent Main => GameNetwork.MyPeer?.GetComponent<FormationComponent>();

        public event EventHandler OnStateChange;

        private FormationState _state = FormationState.None;

        public FormationState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (value != _state)
                {
                    _state = value;
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
                    case FormationState.Skirmish:
                        return Config.Instance.MeleeDebuffSkirm;
                    case FormationState.Rambo:
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
                    case FormationState.Skirmish:
                        return Config.Instance.DistDebuffSkirm;
                    case FormationState.Rambo:
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
                    case FormationState.Skirmish:
                        return Config.Instance.AccDebuffSkirm;
                    case FormationState.Rambo:
                        return Config.Instance.AccDebuffRambo;
                    default:
                        return Config.Instance.AccDebuffForm;
                }
            }
        }

        public FormationComponent()
        {
        }
    }

    public enum FormationState
    {
        None,
        Formation,
        Skirmish,
        Rambo
    }
}

