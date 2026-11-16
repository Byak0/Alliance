using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Models
{
	[Serializable]
	public class VictoryLogic
	{
		[ConfigProperty(label: "Actions on victory", tooltip: "Actions executed sequentially when objectives are completed.", category: "Victory conditions")]
		public List<ActionBase> ActionsOnVictory;

		private int _actionIndex;
		private ActionTask _currentTask;
		private bool _isExecuting;
		private VariableStore _localContext;

		[XmlIgnore]
		public bool IsCompleted { get; private set; }

		public VictoryLogic() { }

		public VictoryLogic(List<ActionBase> actionsOnVictory)
		{
			ActionsOnVictory = actionsOnVictory;
		}

		public void Execute()
		{
			_actionIndex = 0;
			_isExecuting = true;
			IsCompleted = false;
			_currentTask = null;
			_localContext = new VariableStore();

			if (ActionsOnVictory != null && ActionsOnVictory.Count > 0)
			{
				_currentTask = ActionsOnVictory[0].Execute(_localContext);
				_currentTask?.Tick(0f, _localContext);
				if (_currentTask != null && _currentTask.IsCompleted)
					AdvancePipeline();
			}
			else
			{
				IsCompleted = true;
				_isExecuting = false;
			}
		}

		public void Tick(float dt)
		{
			if (!_isExecuting || ActionsOnVictory == null) return;

			_currentTask?.Tick(dt, _localContext);
			if (_currentTask != null && _currentTask.IsCompleted)
				AdvancePipeline();
		}

		private void AdvancePipeline()
		{
			_actionIndex++;
			if (_actionIndex < ActionsOnVictory.Count)
			{
				_currentTask = ActionsOnVictory[_actionIndex].Execute(_localContext);
			}
			else
			{
				IsCompleted = true;
				_isExecuting = false;
				_currentTask = null;
			}
		}
	}
}
