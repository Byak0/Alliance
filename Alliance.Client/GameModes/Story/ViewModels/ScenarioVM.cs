using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.GameModes.Story.ViewModels
{
	public class ScenarioVM : ViewModel
	{
		private bool _showBoard;
		private bool _showIntro;
		private bool _showResult;
		private string _actTitle;
		private string _actDescription;
		private string _resultTitle;
		private string _resultDescription;
		private Color _resultColor;
		private MBBindingList<ObjectiveVM> _objectives;
		private float _lastRefresh;

		[DataSourceProperty]
		public bool ShowBoard
		{
			get
			{
				return _showBoard;
			}
			set
			{
				if (value != _showBoard)
				{
					_showBoard = value;
					OnPropertyChangedWithValue(value, "ShowBoard");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowIntro
		{
			get
			{
				return _showIntro;
			}
			set
			{
				if (value != _showIntro)
				{
					_showIntro = value;
					OnPropertyChangedWithValue(value, "ShowIntro");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowResult
		{
			get
			{
				return _showResult;
			}
			set
			{
				if (value != _showResult)
				{
					_showResult = value;
					OnPropertyChangedWithValue(value, "ShowResult");
				}
			}
		}

		[DataSourceProperty]
		public string ActTitle
		{
			get
			{
				return _actTitle;
			}
			set
			{
				if (value != _actTitle)
				{
					_actTitle = value;
					OnPropertyChangedWithValue(value, "ActTitle");
				}
			}
		}

		[DataSourceProperty]
		public string ActDescription
		{
			get
			{
				return _actDescription;
			}
			set
			{
				if (value != _actDescription)
				{
					_actDescription = value;
					OnPropertyChangedWithValue(value, "ActDescription");
				}
			}
		}

		[DataSourceProperty]
		public string ResultTitle
		{
			get
			{
				return _resultTitle;
			}
			set
			{
				if (value != _resultTitle)
				{
					_resultTitle = value;
					OnPropertyChangedWithValue(value, "ResultTitle");
				}
			}
		}

		[DataSourceProperty]
		public string ResultDescription
		{
			get
			{
				return _resultDescription;
			}
			set
			{
				if (value != _resultDescription)
				{
					_resultDescription = value;
					OnPropertyChangedWithValue(value, "ResultDescription");
				}
			}
		}

		[DataSourceProperty]
		public Color ResultColor
		{
			get
			{
				return _resultColor;
			}
			set
			{
				if (value != _resultColor)
				{
					_resultColor = value;
				}
				OnPropertyChangedWithValue(value, "ResultColor");
			}
		}

		[DataSourceProperty]
		public MBBindingList<ObjectiveVM> Objectives
		{
			get
			{
				return _objectives;
			}
			set
			{
				if (value != _objectives)
				{
					_objectives = value;
					OnPropertyChangedWithValue(value, "Objectives");
				}
			}
		}

		public ScenarioVM()
		{
		}

		public void SetAct(Act act)
		{
			Log("Setting Act in VM : " + act.Name.LocalizedText, LogLevel.Debug);
			ActTitle = act?.Name.LocalizedText;
			ActDescription = act?.Description.LocalizedText;
			if (Mission.Current.PlayerTeam != null) SetObjectives(act, Mission.Current.PlayerTeam.Side);
		}

		public void SetObjectives(Act act, BattleSideEnum side = BattleSideEnum.None)
		{
			Log("Setting Objectives in VM : ", LogLevel.Debug);
			Objectives = new MBBindingList<ObjectiveVM>();
			foreach (ObjectiveBase objective in act?.Objectives.FindAll(obj => obj.Side == side))
			{
				Log(objective.Name + " - " + objective.Description, LogLevel.Debug);
				Objectives.Add(new ObjectiveVM(objective));
			}
		}

		public void RefreshProgress(float dt)
		{
			_lastRefresh += dt;
			if (_lastRefresh < 1f) return;
			_lastRefresh = 0;
			if (_objectives != null)
			{
				foreach (ObjectiveVM objective in _objectives)
				{
					objective.RefreshProgress();
				}
			}
		}

		internal void SetMouseState(bool isMouseVisible)
		{
		}
	}
}