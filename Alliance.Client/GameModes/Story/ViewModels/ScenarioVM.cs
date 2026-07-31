using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
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
		private bool _showLives;
		private string _livesLabel;
		private string _livesValue;
		private MBBindingList<ObjectiveVM> _objectives;
		private float _lastRefresh;
		private SyncObjectiveProgressMessage.ObjectiveData[] _syncedProgress;

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
		public bool ShowLives
		{
			get
			{
				return _showLives;
			}
			set
			{
				if (value != _showLives)
				{
					_showLives = value;
					OnPropertyChangedWithValue(value, "ShowLives");
				}
			}
		}

		[DataSourceProperty]
		public string LivesLabel
		{
			get
			{
				return _livesLabel;
			}
			set
			{
				if (value != _livesLabel)
				{
					_livesLabel = value;
					OnPropertyChangedWithValue(value, "LivesLabel");
				}
			}
		}

		[DataSourceProperty]
		public string LivesValue
		{
			get
			{
				return _livesValue;
			}
			set
			{
				if (value != _livesValue)
				{
					_livesValue = value;
					OnPropertyChangedWithValue(value, "LivesValue");
				}
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
			if (act?.Objectives == null) return;
			for (int i = 0; i < act.Objectives.Count; i++)
			{
				var obj = act.Objectives[i];
				if (obj.Side == side)
				{
					Log(obj.Name.LocalizedText + " - " + obj.Description.LocalizedText, LogLevel.Debug);
					Objectives.Add(new ObjectiveVM(obj, i));
				}
			}
		}

		public void SetLives(RespawnStrategy strategy, int teamRemainingLives, int playerRemainingLives)
		{
			switch (strategy)
			{
				case RespawnStrategy.MaxLivesPerTeam:
					if(teamRemainingLives > 1)
					{
						LivesLabel = teamRemainingLives.ToString() + " lives left for team.";
					}
					else if (teamRemainingLives == 1)
					{
						LivesLabel = " 1 life left for team.";
					}
					else
					{
						LivesLabel = "No lives left for team.";
					}
					break;
				case RespawnStrategy.MaxLivesPerPlayer:
					if (playerRemainingLives > 1)
					{
						LivesLabel = "You have " + playerRemainingLives.ToString() + " lives left.";
					}
					else if (playerRemainingLives == 1)
					{
						LivesLabel = "You have 1 life left.";
					}
					else
					{
						LivesLabel = "You don't have any life left.";
					}
					break;
				default:
					LivesLabel = string.Empty;
					LivesValue = string.Empty;
					break;
			}
		}

		public void RefreshProgress(float dt)
		{
			_lastRefresh += dt;
			if (_lastRefresh < 0.5f) return;
			_lastRefresh = 0;
			if (_objectives == null || _syncedProgress == null) return;
			foreach (ObjectiveVM vm in _objectives)
			{
				if (vm.Index < _syncedProgress.Length)
					vm.UpdateFromSync(_syncedProgress[vm.Index]);
			}
		}

		public void SetSyncedProgress(SyncObjectiveProgressMessage.ObjectiveData[] data)
		{
			_syncedProgress = data;
		}

		internal void SetMouseState(bool isMouseVisible)
		{
		}
	}
}