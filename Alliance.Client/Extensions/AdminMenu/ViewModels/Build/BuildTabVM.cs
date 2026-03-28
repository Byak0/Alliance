using Alliance.Common.Extensions.BuildSystem.Configuration;
using Alliance.Common.Extensions.BuildSystem.Configuration.Models;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels.Build
{
	public class BuildTabVM : ViewModel
	{
		private const string AllValue = "All";

		private MBBindingList<BuildPrefabItemVM> _prefabs;
		private string _filterText;
		private string _statusText;
		private bool _showOnlyEnabled;
		private SelectorVM<SelectorItemVM> _moduleSelector;
		private SelectorVM<SelectorItemVM> _fileSelector;

		private BuildPrefabPreset _preset;
		private bool _isInitialized;
		private string _selectedModule = AllValue;
		private string _selectedFile = AllValue;

		public BuildTabVM()
		{
			Prefabs = new MBBindingList<BuildPrefabItemVM>();
			ModuleSelector = new SelectorVM<SelectorItemVM>(0, OnModuleSelectionChanged);
			FileSelector = new SelectorVM<SelectorItemVM>(0, OnFileSelectionChanged);
		}

		[DataSourceProperty]
		public MBBindingList<BuildPrefabItemVM> Prefabs
		{
			get => _prefabs;
			set
			{
				if (value != _prefabs)
				{
					_prefabs = value;
					OnPropertyChangedWithValue(value, nameof(Prefabs));
				}
			}
		}

		[DataSourceProperty]
		public string FilterText
		{
			get => _filterText;
			set
			{
				if (value != _filterText)
				{
					_filterText = value;
					OnPropertyChangedWithValue(value, nameof(FilterText));
					ApplyFilter();
				}
			}
		}

		[DataSourceProperty]
		public string StatusText
		{
			get => _statusText;
			set
			{
				if (value != _statusText)
				{
					_statusText = value;
					OnPropertyChangedWithValue(value, nameof(StatusText));
				}
			}
		}

		[DataSourceProperty]
		public bool ShowOnlyEnabled
		{
			get => _showOnlyEnabled;
			set
			{
				if (value != _showOnlyEnabled)
				{
					_showOnlyEnabled = value;
					OnPropertyChangedWithValue(value, nameof(ShowOnlyEnabled));
					ApplyFilter();
				}
			}
		}

		[DataSourceProperty]
		public SelectorVM<SelectorItemVM> ModuleSelector
		{
			get => _moduleSelector;
			set
			{
				if (value != _moduleSelector)
				{
					_moduleSelector = value;
					OnPropertyChangedWithValue(value, nameof(ModuleSelector));
				}
			}
		}

		[DataSourceProperty]
		public SelectorVM<SelectorItemVM> FileSelector
		{
			get => _fileSelector;
			set
			{
				if (value != _fileSelector)
				{
					_fileSelector = value;
					OnPropertyChangedWithValue(value, nameof(FileSelector));
				}
			}
		}

		[UsedImplicitly]
		public void Reload()
		{
			_preset = BuildPrefabCatalogManager.LoadOrCreatePreset();

			HashSet<string> enabledIds = new HashSet<string>(
				(_preset.AllowedPrefabs ?? new List<BuildPrefabReference>())
					.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
					.Select(x => x.Id),
				StringComparer.OrdinalIgnoreCase);

			MBBindingList<BuildPrefabItemVM> items = new MBBindingList<BuildPrefabItemVM>();
			foreach (BuildPrefabDefinition prefab in BuildPrefabCatalogManager.Catalog.Prefabs
				.OrderBy(x => x.Module)
				.ThenBy(x => x.SourcePath)
				.ThenBy(x => x.Id))
			{
				items.Add(new BuildPrefabItemVM(prefab, enabledIds.Contains(prefab.Id), OnPrefabStateChanged));
			}

			Prefabs = items;

			RefreshModuleSelector();
			RefreshFileSelector();
			ApplyFilter();
			UpdateStatusText("Build prefab preset loaded.");
			_isInitialized = true;
		}

		[UsedImplicitly]
		public void Save()
		{
			BuildPrefabPreset preset = new BuildPrefabPreset
			{
				Name = _preset?.Name ?? "Default",
				AllowedPrefabs = Prefabs
					.Where(x => x.IsEnabled)
					.Select(x => new BuildPrefabReference { Id = x.Id })
					.ToList()
			};

			if (BuildPrefabCatalogManager.SavePreset(preset))
			{
				_preset = preset;
				UpdateStatusText("Build prefab preset saved.");
			}
			else
			{
				UpdateStatusText("Failed to save build prefab preset.");
			}
		}

		[UsedImplicitly]
		public void EnableAllVisible()
		{
			foreach (BuildPrefabItemVM prefab in Prefabs.Where(x => x.IsVisible))
			{
				prefab.IsEnabled = true;
			}

			UpdateStatusText("All visible prefabs enabled.");
		}

		[UsedImplicitly]
		public void DisableAllVisible()
		{
			foreach (BuildPrefabItemVM prefab in Prefabs.Where(x => x.IsVisible))
			{
				prefab.IsEnabled = false;
			}

			UpdateStatusText("All visible prefabs disabled.");
		}

		[UsedImplicitly]
		public void ToggleShowOnlyEnabled()
		{
			ShowOnlyEnabled = !ShowOnlyEnabled;
		}

		[UsedImplicitly]
		public void EnsureInitialized()
		{
			if (!_isInitialized)
			{
				Reload();
			}
		}

		private void RefreshModuleSelector()
		{
			List<string> modules = new List<string> { AllValue };
			modules.AddRange(Prefabs
				.Select(x => x.Module)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

			if (!modules.Any(x => string.Equals(x, _selectedModule, StringComparison.OrdinalIgnoreCase)))
			{
				_selectedModule = AllValue;
			}

			int selectedIndex = modules.FindIndex(x => string.Equals(x, _selectedModule, StringComparison.OrdinalIgnoreCase));
			ModuleSelector.Refresh(modules, selectedIndex < 0 ? 0 : selectedIndex, OnModuleSelectionChanged);
		}

		private void RefreshFileSelector()
		{
			IEnumerable<BuildPrefabItemVM> filtered = Prefabs;

			if (!string.Equals(_selectedModule, AllValue, StringComparison.OrdinalIgnoreCase))
			{
				filtered = filtered.Where(x => string.Equals(x.Module, _selectedModule, StringComparison.OrdinalIgnoreCase));
			}

			List<string> files = new List<string> { AllValue };
			files.AddRange(filtered
				.Select(x => x.SourceFile)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

			if (!files.Any(x => string.Equals(x, _selectedFile, StringComparison.OrdinalIgnoreCase)))
			{
				_selectedFile = AllValue;
			}

			int selectedIndex = files.FindIndex(x => string.Equals(x, _selectedFile, StringComparison.OrdinalIgnoreCase));
			FileSelector.Refresh(files, selectedIndex < 0 ? 0 : selectedIndex, OnFileSelectionChanged);
		}

		private void OnModuleSelectionChanged(SelectorVM<SelectorItemVM> selector)
		{
			string selectedModule = GetSelectedString(selector);
			if (selectedModule == null)
			{
				return;
			}

			_selectedModule = selectedModule;
			_selectedFile = AllValue;

			RefreshFileSelector();
			ApplyFilter();
		}

		private void OnFileSelectionChanged(SelectorVM<SelectorItemVM> selector)
		{
			string selectedFile = GetSelectedString(selector);
			if (selectedFile == null)
			{
				return;
			}

			_selectedFile = selectedFile;
			ApplyFilter();
		}

		private void OnPrefabStateChanged()
		{
			ApplyFilter();
		}

		private void ApplyFilter()
		{
			foreach (BuildPrefabItemVM prefab in Prefabs)
			{
				prefab.IsVisible = prefab.MatchesFilter(FilterText, _selectedModule, _selectedFile, ShowOnlyEnabled);
			}

			UpdateStatusText();
		}

		private void UpdateStatusText(string prefix = null)
		{
			int enabledCount = Prefabs.Count(x => x.IsEnabled);
			int visibleCount = Prefabs.Count(x => x.IsVisible);
			int totalCount = Prefabs.Count;

			StatusText = string.IsNullOrWhiteSpace(prefix)
				? $"Enabled: {enabledCount}/{totalCount} - Visible: {visibleCount}"
				: $"{prefix} Enabled: {enabledCount}/{totalCount} - Visible: {visibleCount}";
		}

		private static string GetSelectedString(SelectorVM<SelectorItemVM> selector)
		{
			return selector?.SelectedItem?.StringItem;
		}
	}
}