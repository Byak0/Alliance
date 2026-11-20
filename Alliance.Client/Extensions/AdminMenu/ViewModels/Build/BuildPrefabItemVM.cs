using System;
using System.IO;
using Alliance.Common.Extensions.BuildSystem.Configuration.Models;
using JetBrains.Annotations;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels.Build
{
	public class BuildPrefabItemVM : ViewModel
	{
		private readonly Action _onStateChanged;

		private string _id;
		private string _module;
		private string _sourcePath;
		private string _sourceFile;
		private bool _isEnabled;
		private bool _isVisible = true;

		public BuildPrefabItemVM(BuildPrefabDefinition definition, bool isEnabled, Action onStateChanged)
		{
			_onStateChanged = onStateChanged;

			Id = definition?.Id ?? string.Empty;
			Module = definition?.Module ?? string.Empty;
			SourcePath = definition?.SourcePath ?? string.Empty;
			SourceFile = string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetFileName(SourcePath);
			IsEnabled = isEnabled;
		}

		[DataSourceProperty]
		public string Id
		{
			get => _id;
			set
			{
				if (value != _id)
				{
					_id = value;
					OnPropertyChangedWithValue(value, nameof(Id));
				}
			}
		}

		[DataSourceProperty]
		public string Module
		{
			get => _module;
			set
			{
				if (value != _module)
				{
					_module = value;
					OnPropertyChangedWithValue(value, nameof(Module));
				}
			}
		}

		[DataSourceProperty]
		public string SourcePath
		{
			get => _sourcePath;
			set
			{
				if (value != _sourcePath)
				{
					_sourcePath = value;
					OnPropertyChangedWithValue(value, nameof(SourcePath));
				}
			}
		}

		[DataSourceProperty]
		public string SourceFile
		{
			get => _sourceFile;
			set
			{
				if (value != _sourceFile)
				{
					_sourceFile = value;
					OnPropertyChangedWithValue(value, nameof(SourceFile));
				}
			}
		}

		[DataSourceProperty]
		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;
					OnPropertyChangedWithValue(value, nameof(IsEnabled));
					_onStateChanged?.Invoke();
				}
			}
		}

		[DataSourceProperty]
		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				if (value != _isVisible)
				{
					_isVisible = value;
					OnPropertyChangedWithValue(value, nameof(IsVisible));
				}
			}
		}

		[UsedImplicitly]
		public void ToggleEnabled()
		{
			IsEnabled = !IsEnabled;
		}

		public bool MatchesFilter(string filterText, string selectedModule, string selectedFile, bool showOnlyEnabled)
		{
			if (showOnlyEnabled && !IsEnabled)
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(selectedModule)
				&& !string.Equals(selectedModule, "All", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(Module, selectedModule, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(selectedFile)
				&& !string.Equals(selectedFile, "All", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(SourceFile, selectedFile, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(filterText))
			{
				return true;
			}

			string filter = filterText.Trim();

			return ContainsIgnoreCase(Id, filter)
				|| ContainsIgnoreCase(Module, filter)
				|| ContainsIgnoreCase(SourceFile, filter)
				|| ContainsIgnoreCase(SourcePath, filter);
		}

		private static bool ContainsIgnoreCase(string source, string filter)
		{
			return !string.IsNullOrWhiteSpace(source)
				&& source.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}