using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using Alliance.Common.GameModes.Story.Utilities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Alliance.Editor.Extensions.Story.ViewModels
{
	public class ScenarioEditorViewModel : INotifyPropertyChanged
	{
		public ObjectEditorViewModel ObjectEditorVM { get; private set; }
		public Scenario Scenario { get; private set; }

		private List<string> _availableLanguages;
		private string _selectedLanguage;
		private string _currentFilePath;

		public List<string> AvailableLanguages
		{
			get => _availableLanguages;
			set
			{
				_availableLanguages = value;
				OnPropertyChanged(nameof(AvailableLanguages));
			}
		}

		public string SelectedLanguage
		{
			get => _selectedLanguage;
			set
			{
				if (_selectedLanguage != value)
				{
					_selectedLanguage = value;
					OnPropertyChanged(nameof(SelectedLanguage));
					ObjectEditorVM?.UpdateAllFieldsLanguage();
				}
			}
		}

		public ICommand NewCommand { get; }
		public ICommand LoadCommand { get; }
		public ICommand SaveCommand { get; }
		public ICommand SaveAsCommand { get; }

		public ScenarioEditorViewModel(Scenario scenario)
		{
			Scenario = scenario;
			ObjectEditorVM = new ObjectEditorViewModel(scenario, this, "Alliance - Scenario Editor");
			NewCommand = new RelayCommand(NewScenario);
			LoadCommand = new RelayCommand(LoadScenario);
			SaveCommand = new RelayCommand(SaveScenario);
			SaveAsCommand = new RelayCommand(SaveAsScenario);
			AvailableLanguages = LocalizationHelper.GetAvailableLanguages();
			SelectedLanguage = "English";
		}

		public ScenarioEditorViewModel()
		{
			Scenario = ExampleScenarios.BFHD();
			ObjectEditorVM = new ObjectEditorViewModel(Scenario, this, "Alliance - Scenario Editor");
			NewCommand = new RelayCommand(NewScenario);
			LoadCommand = new RelayCommand(LoadScenario);
			SaveCommand = new RelayCommand(SaveScenario);
			SaveAsCommand = new RelayCommand(SaveAsScenario);
			AvailableLanguages = LocalizationHelper.GetAvailableLanguages();
			SelectedLanguage = "English";
		}

		private void NewScenario(object obj)
		{
			if (ConfirmUnsavedChanges())
			{
				Scenario = new Scenario(new LocalizedString("New scenario"), new LocalizedString());
				ObjectEditorVM = new ObjectEditorViewModel(Scenario, this, "Alliance - Scenario Editor");
				_currentFilePath = null;  // New scenario, no file path yet
				OnPropertyChanged(nameof(ObjectEditorVM));
			}
		}

		private void SaveAsScenario(object obj)
		{
			// Clean the scenario name to make it filename-safe (remove illegal characters)
			string safeScenarioName = string.Join("_", Scenario.Name.GetText().Split(Path.GetInvalidFileNameChars()));

			// Merge the scenario name with the ID to ensure uniqueness
			string fileName = $"{safeScenarioName}_{Scenario.Id}.xml";

			var saveFileDialog = new SaveFileDialog
			{
				Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
				Title = "Save Scenario As",
				FileName = fileName
			};

			if (saveFileDialog.ShowDialog() == true)
			{
				_currentFilePath = saveFileDialog.FileName;
				SaveScenarioToFile(_currentFilePath);
			}
		}

		private void SaveScenario(object obj)
		{
			if (string.IsNullOrEmpty(_currentFilePath))
			{
				SaveAsScenario(obj);  // If no file path exists, fall back to Save As
			}
			else
			{
				SaveScenarioToFile(_currentFilePath);
			}
		}

		private void LoadScenario(object obj)
		{
			if (ConfirmUnsavedChanges())
			{
				var openFileDialog = new OpenFileDialog
				{
					Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
					Title = "Open Scenario"
				};

				if (openFileDialog.ShowDialog() == true)
				{
					_currentFilePath = openFileDialog.FileName;

					// Deserialize the scenario from the selected file
					Scenario = ScenarioSerializer.DeserializeScenarioFromPath(_currentFilePath);
					ObjectEditorVM = new ObjectEditorViewModel(Scenario, this, "Alliance - Scenario Editor");
					OnPropertyChanged(nameof(ObjectEditorVM));
				}
			}
		}

		public bool ConfirmUnsavedChanges()
		{
			MessageBoxResult result = MessageBox.Show(
				"You have unsaved changes. Do you want to save them before continuing?",
				"Unsaved Changes",
				MessageBoxButton.YesNoCancel);

			if (result == MessageBoxResult.Yes)
			{
				SaveScenario(Scenario);
				return true;
			}
			return result != MessageBoxResult.Cancel;
		}

		private void SaveScenarioToFile(string filePath)
		{
			try
			{
				Scenario.Version++;
				Scenario.LastEditedAt = DateTime.Now;
				Scenario.LastEditedBy = Environment.UserName;
				ScenarioSerializer.SerializeScenarioToXML(Scenario, filePath);
			}
			catch (IOException ex)
			{
				MessageBox.Show($"Failed to save scenario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
