using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Objectives;
using System;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.GameModes.Story.ViewModels
{
	public class ObjectiveVM : ViewModel
	{
		private readonly Objective _objective;
		private readonly int _index;

		public int Index => _index;

		private string _name;
		private string _description;
		private string _progress;
		private bool _isCompleted;

		public ObjectiveVM(Objective objective, int index)
		{
			_objective = objective;
			_index = index;
			Name = objective.Name.LocalizedText;
			Description = objective.Description.LocalizedText;
		}

		public void UpdateFromSync(SyncObjectiveProgressMessage.ObjectiveData data)
		{
			IsCompleted = data.IsCompleted;
			Progress = FormatProgress(data);
		}

		private string FormatProgress(SyncObjectiveProgressMessage.ObjectiveData data)
		{
			if (data.Elements == null || data.Elements.Length == 0) return "";
			if (_objective.Progress == null || _objective.Progress.Count == 0) return "";

			var sb = new StringBuilder();
			for (int i = 0; i < data.Elements.Length && i < _objective.Progress.Count; i++)
			{
				ref var el = ref data.Elements[i];
				if (sb.Length > 0) sb.Append("  ");

				if (el.Type == 0) // TextElement
				{
					string template = _objective.Progress[i] is TextElement te
						? te.Template?.LocalizedText ?? "{0}"
						: "{0}";
					if (el.Values != null && el.Values.Length > 0)
						sb.AppendFormat(template, el.Values);
					else
						sb.Append(template);
				}
				else if (el.Type == 1) // BarElement
				{
					sb.Append($"{el.Text}: {(int)el.Current}/{(int)el.Max}");
				}
				else if (el.Type == 2) // TimerElement
				{
					float now = Mission.Current?.GetMissionTimeInSeconds() ?? 0f;
					float remaining = Math.Max(0, el.EndTime - now);
					sb.Append($"{el.Text}: {TimeSpan.FromSeconds(remaining):mm\\:ss}");
				}
			}
			return sb.ToString();
		}

		[DataSourceProperty]
		public string Name
		{
			get => _name;
			set { if (value != _name) { _name = value; OnPropertyChangedWithValue(value); } }
		}

		[DataSourceProperty]
		public string Description
		{
			get => _description;
			set { if (value != _description) { _description = value; OnPropertyChangedWithValue(value); } }
		}

		[DataSourceProperty]
		public string Progress
		{
			get => _progress;
			set { if (value != _progress) { _progress = value; OnPropertyChangedWithValue(value); } }
		}

		[DataSourceProperty]
		public bool IsCompleted
		{
			get => _isCompleted;
			set { if (value != _isCompleted) { _isCompleted = value; OnPropertyChangedWithValue(value); } }
		}
	}
}
