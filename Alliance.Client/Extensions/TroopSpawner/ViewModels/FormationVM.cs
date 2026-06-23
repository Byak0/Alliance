using Alliance.Common.Extensions.TroopSpawner.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
	/// <summary>
	/// View model for a formation.
	/// </summary>
	public class FormationVM : ViewModel
	{
		private Action<FormationVM> _onFormationSelected;
		private bool _isSelected;
		private bool _hasCommander;
		private string _commanderName;
		private OrderTroopItemVM _orderTroopVM;

		public Formation Formation { get; }

		public bool Selected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					OrderTroopVM.IsSelected = _isSelected;
				}
			}
		}

		[DataSourceProperty]
		public bool HasCommander
		{
			get
			{
				return _hasCommander;
			}
			set
			{
				if (_hasCommander != value)
				{
					_hasCommander = value;
					OnPropertyChangedWithValue(value, "HasCommander");
				}
			}
		}

		[DataSourceProperty]
		public string CommanderName
		{
			get
			{
				return _commanderName;
			}
			set
			{
				if (_commanderName != value)
				{
					_commanderName = value;
					OnPropertyChangedWithValue(value, "CommanderName");
				}
			}
		}

		[DataSourceProperty]
		public OrderTroopItemVM OrderTroopVM
		{
			get
			{
				return _orderTroopVM;
			}
			set
			{
				if (_orderTroopVM != value)
				{
					_orderTroopVM = value;
					OnPropertyChangedWithValue(value, "OrderTroopVM");
				}
			}
		}

		public FormationVM(Formation formation, Action<FormationVM> selectFormation)
		{
			Formation = formation;
			OrderTroopVM = new OrderTroopItemVM(formation, null, new Func<Formation, int>(GetFormationMorale));
			OrderTroopVM.IsSelectable = true;
			Formation.OnUnitCountChanged += RefreshFormationInfos;
			_onFormationSelected = selectFormation;
			SetCommanderInfos(FormationControlModel.Instance.GetControllerOfFormation(Formation));
		}

		public override void OnFinalize()
		{
			Formation.OnUnitCountChanged -= RefreshFormationInfos;
		}

		private void RefreshFormationInfos(Formation formation)
		{
			OrderTroopVM.SetFormationClassFromFormation(formation);
		}

		public void RefreshCommanderVisual(Agent agent)
		{
			if (OrderTroopVM == null) return;

			if (agent != null) OrderTroopVM.CaptainImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(agent.Character));
			else OrderTroopVM.CaptainImageIdentifier = null;
		}

		public void SetCommanderInfos(MissionPeer commander)
		{
			HasCommander = commander != null;
			CommanderName = commander?.Name;
			RefreshCommanderVisual(commander?.ControlledAgent);
		}

		private int GetFormationMorale(Formation formation)
		{
			return (int)MissionGameModels.Current.BattleMoraleModel.GetAverageMorale(formation);
		}

		public void SelectFormation()
		{
			_onFormationSelected?.Invoke(this);
			Log("Select formation " + Formation.Index.ToString(), LogLevel.Debug);
		}
	}
}