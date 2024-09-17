using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing an object in the scenario editor.
	/// </summary>
	public class ObjectEditorViewModel : INotifyPropertyChanged
	{
		private ScenarioEditorViewModel _parentViewModel;
		public object Object { get; set; }
		public ObservableCollection<FieldViewModel> Fields { get; private set; }
		public string Title { get; set; }
		public string SelectedLanguage => _parentViewModel?.SelectedLanguage ?? "English";

		public ObjectEditorViewModel(object obj, ScenarioEditorViewModel parentViewModel, string title)
		{
			InitVM(obj, parentViewModel, title);
		}

		public ObjectEditorViewModel()
		{
			// If in design mode, create a dummy object to display in the designer
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				object obj = new KillAllObjective();
				string title = "Alliance - Scenario Editor";
				ScenarioEditorViewModel parentViewModel = new ScenarioEditorViewModel();

				InitVM(obj, parentViewModel, title);
			}
		}

		private void InitVM(object obj, ScenarioEditorViewModel parentViewModel, string title)
		{
			FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

			// If there is only one non-abstract field of type object, directly open its UI
			// (example: if the object has only one field of type TWConfig, open the TWConfig directly)
			if (fieldInfos.Length == 1 && !fieldInfos[0].FieldType.IsAbstract && fieldInfos[0].FieldType.IsClass)
			{
				var singleField = fieldInfos[0];
				var fieldValue = singleField.GetValue(obj);

				// If the field value is null, instantiate it
				if (fieldValue == null)
				{
					fieldValue = Activator.CreateInstance(singleField.FieldType);
					singleField.SetValue(obj, fieldValue);
				}

				title += " > " + ScenarioEditorHelper.GetItemDisplayName(obj);
				obj = fieldValue;
			}

			Object = obj;
			_parentViewModel = parentViewModel;
			Fields = new ObservableCollection<FieldViewModel>();

			// Iterate over all public fields
			foreach (FieldInfo fi in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				// Skip fields that are marked as not editable
				ScenarioEditorAttribute attribute = fi.GetCustomAttribute<ScenarioEditorAttribute>();
				if (attribute == null || attribute.IsEditable)
				{
					// Create a view model to display the field content
					FieldViewModel fvm = new FieldViewModel(fi, fi.GetValue(obj), this, _parentViewModel);
					Fields.Add(fvm);
				}
			}
			Title = title + " > " + ScenarioEditorHelper.GetItemDisplayName(obj);
		}

		public void UpdateAllFieldsLanguage()
		{
			foreach (var field in Fields)
			{
				if (field.FieldValue is LocalizedString)
				{
					field.OnPropertyChanged(nameof(field.LocalizedText));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}