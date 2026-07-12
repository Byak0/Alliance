using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using TaleWorlds.Engine;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing an object in the scenario editor.
	/// </summary>
	public class ObjectEditorViewModel : INotifyPropertyChanged
	{
		protected ScenarioEditorViewModel ScenarioVM;
		protected FieldViewModel ParentVM;
		public object Object { get; set; }
		public ObservableCollection<FieldViewModel> Fields { get; private set; }
		public ObservableCollection<FieldCategoryViewModel> FieldCategories { get; private set; }
		public string Title { get; set; }
		public string SelectedLanguage => ScenarioVM?.SelectedLanguage ?? "English";
		public WeakGameEntity GameEntity { get; set; }

		public ObjectEditorViewModel(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title, WeakGameEntity gameEntity)
		{
			InitVM(obj, parentVM, scenarioVM, title, gameEntity);
		}

		public ObjectEditorViewModel(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title)
		{
			InitVM(obj, parentVM, scenarioVM, title, WeakGameEntity.Invalid);
		}

		public ObjectEditorViewModel()
		{
			// If in design mode, create a dummy object to display in the designer
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				AgentEnteredZoneCondition obj = new AgentEnteredZoneCondition()
				{
					Zone = new SerializableZone(new TaleWorlds.Library.Vec3(12f, 102.56f, 69442.1f), 124.41f)
				};
				obj.TargetCount = 5;
				string title = "Alliance - Scenario Editor";
				ScenarioEditorViewModel parentViewModel = new ScenarioEditorViewModel();

				InitVM(obj, null, parentViewModel, title, WeakGameEntity.Invalid);
			}
		}

		private void InitVM(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title, WeakGameEntity gameEntity)
		{
			ParentVM = parentVM;
			GameEntity = gameEntity;

			FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

			// If obj holds a ParentEntity reference, use it
			if (obj is ConditionalActionStruct conditionalActionStruct && conditionalActionStruct.ParentEntity != null)
			{
				GameEntity = conditionalActionStruct.ParentEntity;
			}
			// If there is only one non-abstract field of type object, directly open its UI
			// (example: if the object has only one field of type TWConfig, open the TWConfig directly)
			else if (fieldInfos.Length == 1 && !fieldInfos[0].FieldType.IsAbstract && fieldInfos[0].FieldType.IsClass && fieldInfos[0].FieldType != typeof(string))
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
			ScenarioVM = scenarioVM;
			Fields = new ObservableCollection<FieldViewModel>();
			FieldCategories = new ObservableCollection<FieldCategoryViewModel>();

			RefreshFields();
			Title = title + " > " + ScenarioEditorHelper.GetItemDisplayName(obj);

			if (ScenarioVM != null)
			{
				ScenarioVM.OnLanguageChange += UpdateAllFieldsLanguage;
			}
		}

		public void RefreshFields()
		{
			// Determine which field names are allowed
			HashSet<string> allowedFields = null;
			if(ParentVM?.ParentObject is GameModeSettings settings)
			{
				if (Object is TWConfig)
				{
					allowedFields = new HashSet<string>(settings.GetAvailableNativeOptions().Select(o => o.ToString()));
				}
				else if (Object is Config)
				{
					allowedFields = new HashSet<string>(settings.GetAvailableModOptions());
				}
			}

			List<FieldInfo> editableFields = Object.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(fi =>
				{
					ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();
					if (attr != null && !attr.IsEditable) return false;
					if (allowedFields != null && !allowedFields.Contains(fi.Name)) return false;
					return true;
				})
				.ToList();

			Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
			foreach (var category in FieldCategories)
			{
				expandedStates[category.Name] = category.IsExpanded;
			}

			Fields.Clear();
			FieldCategories.Clear();

			Dictionary<string, FieldCategoryViewModel> categories = new Dictionary<string, FieldCategoryViewModel>();

			foreach (FieldInfo fi in editableFields)
			{
				ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();
				// If the field has a dependency and the dependency is not satisfied, skip it
				if (attr != null && !attr.IsDependencySatisfied(Object))
				{
					continue;
				}
				// Add fields without a category to the Fields collection directly
				else if (attr == null || attr.Category == null)
				{
					Fields.Add(new FieldViewModel(fi, fi.GetValue(Object), this, ScenarioVM));
				}
				// Add fields with a category to the appropriate FieldCategoryViewModel
				else
				{
					string categoryName = attr.Category;
					if (!categories.ContainsKey(categoryName))
					{
						bool isExpanded = expandedStates.ContainsKey(categoryName)
							? expandedStates[categoryName]
							: (categoryName == "General");
						categories[categoryName] = new FieldCategoryViewModel(categoryName, isExpanded);
					}

					categories[categoryName].Fields.Add(new FieldViewModel(fi, fi.GetValue(Object), this, ScenarioVM));
				}
			}

			foreach (var category in categories.Values)
			{
				FieldCategories.Add(category);
			}
		}

		public void Close()
		{
			foreach (var field in Fields)
			{
				field.Close();
			}

			foreach (var category in FieldCategories)
			{
				foreach (var field in category.Fields)
				{
					field.Close();
				}
			}

			if (ScenarioVM != null)
			{
				ScenarioVM.OnLanguageChange -= UpdateAllFieldsLanguage;
			}
		}

		public void UpdateAllFieldsLanguage(object sender, EventArgs args)
		{
			foreach (var field in Fields)
			{
				if (field.FieldValue is LocalizedString)
				{
					field.OnPropertyChanged(nameof(field.LocalizedText));
				}
			}

			foreach (var category in FieldCategories)
			{
				foreach (var field in category.Fields)
				{
					if (field.FieldValue is LocalizedString)
					{
						field.OnPropertyChanged(nameof(field.LocalizedText));
					}
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
