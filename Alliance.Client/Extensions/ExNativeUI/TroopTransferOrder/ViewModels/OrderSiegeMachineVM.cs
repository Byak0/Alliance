using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects.Siege;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class OrderSiegeMachineVM : OrderSubjectVM
    {
        public Type MachineType;

        public Action<OrderSiegeMachineVM> SetSelected;

        private string _machineClass = "";

        private double _currentHP;

        private bool _isInside;

        private Vec2 _position;

        public DeploymentPoint DeploymentPoint { get; private set; }

        public SiegeWeapon SiegeWeapon
        {
            get
            {
                if (DeploymentPoint != null)
                {
                    return DeploymentPoint.DeployedWeapon as SiegeWeapon;
                }

                return null;
            }
        }

        public bool IsPrimarySiegeMachine => SiegeWeapon is IPrimarySiegeWeapon;

        [DataSourceProperty]
        public string MachineClass
        {
            get
            {
                return _machineClass;
            }
            set
            {
                _machineClass = value;
                OnPropertyChangedWithValue(value, "MachineClass");
            }
        }

        [DataSourceProperty]
        public double CurrentHP
        {
            get
            {
                return _currentHP;
            }
            set
            {
                if (value != _currentHP)
                {
                    _currentHP = value;
                    OnPropertyChangedWithValue(value, "CurrentHP");
                }
            }
        }

        public bool IsInside
        {
            get
            {
                return _isInside;
            }
            set
            {
                if (value != _isInside)
                {
                    _isInside = value;
                    OnPropertyChangedWithValue(value, "IsInside");
                }
            }
        }

        public Vec2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChangedWithValue(value, "Position");
                }
            }
        }

        private void ExecuteAction()
        {
            if (SiegeWeapon != null)
            {
                SetSelected(this);
            }
        }

        public OrderSiegeMachineVM(DeploymentPoint deploymentPoint, Action<OrderSiegeMachineVM> setSelected, int keyIndex)
        {
            DeploymentPoint = deploymentPoint;
            SetSelected = setSelected;
            ShortcutText = keyIndex.ToString();
        }

        public void RefreshSiegeWeapon()
        {
            if (SiegeWeapon == null)
            {
                MachineType = null;
                MachineClass = "none";
                CurrentHP = 1.0;
                IsSelectable = false;
                IsSelected = false;
                return;
            }

            IsSelectable = !SiegeWeapon.IsDestroyed && !SiegeWeapon.IsDeactivated;
            MachineType = SiegeWeapon.GetType();
            MachineClass = SiegeWeapon.GetSiegeEngineType().StringId;
            if (SiegeWeapon.DestructionComponent != null)
            {
                CurrentHP = SiegeWeapon.DestructionComponent.HitPoint / SiegeWeapon.DestructionComponent.MaxHitPoint;
            }
        }

        public static SiegeEngineType GetSiegeType(Type t, BattleSideEnum side)
        {
            if (t == typeof(SiegeLadder))
            {
                return DefaultSiegeEngineTypes.Ladder;
            }

            if (t == typeof(Ballista))
            {
                return DefaultSiegeEngineTypes.Ballista;
            }

            if (t == typeof(FireBallista))
            {
                return DefaultSiegeEngineTypes.FireBallista;
            }

            if (t == typeof(BatteringRam))
            {
                return DefaultSiegeEngineTypes.Ram;
            }

            if (t == typeof(SiegeTower))
            {
                return DefaultSiegeEngineTypes.SiegeTower;
            }

            if (t == typeof(Mangonel))
            {
                if (side != BattleSideEnum.Attacker)
                {
                    return DefaultSiegeEngineTypes.Catapult;
                }

                return DefaultSiegeEngineTypes.Onager;
            }

            if (t == typeof(FireMangonel))
            {
                if (side != BattleSideEnum.Attacker)
                {
                    return DefaultSiegeEngineTypes.FireCatapult;
                }

                return DefaultSiegeEngineTypes.FireOnager;
            }

            if (t == typeof(Trebuchet))
            {
                return DefaultSiegeEngineTypes.Trebuchet;
            }

            if (t == typeof(FireTrebuchet))
            {
                return DefaultSiegeEngineTypes.FireTrebuchet;
            }

            Debug.FailedAssert("Invalid siege weapon", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\OrderSiegeMachineVM.cs", "GetSiegeType", 163);
            return DefaultSiegeEngineTypes.Ladder;
        }
    }
}
