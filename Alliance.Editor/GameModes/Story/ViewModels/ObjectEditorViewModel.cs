using Alliance.Common.Core.Configuration.Models;
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
		protected ScenarioEditorViewModel parentViewModel;
		public object Object { get; set; }
		public ObservableCollection<FieldViewModel> Fields { get; private set; }
		public ObservableCollection<FieldCategoryViewModel> FieldCategories { get; private set; }
		public string Title { get; set; }
		public string SelectedLanguage => parentViewModel?.SelectedLanguage ?? "English";
		public WeakGameEntity GameEntity { get; set; }

		public ObjectEditorViewModel(object obj, ScenarioEditorViewModel parentViewModel, string title, WeakGameEntity gameEntity)
		{
			InitVM(obj, parentViewModel, title, gameEntity);
		}

		public ObjectEditorViewModel(object obj, ScenarioEditorViewModel parentViewModel, string title)
		{
			InitVM(obj, parentViewModel, title, WeakGameEntity.Invalid);
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

				InitVM(obj, parentViewModel, title, WeakGameEntity.Invalid);
			}
		}

		private void InitVM(object obj, ScenarioEditorViewModel parentViewModel, string title, WeakGameEntity gameEntity)
		{
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
			this.parentViewModel = parentViewModel;
			Fields = new ObservableCollection<FieldViewModel>();
			FieldCategories = new ObservableCollection<FieldCategoryViewModel>();

			RefreshFields();
			Title = title + " > " + ScenarioEditorHelper.GetItemDisplayName(obj);

			if (parentViewModel != null)
			{
				parentViewModel.OnLanguageChange += UpdateAllFieldsLanguage;
			}
		}

		public void RefreshFields()
		{
			List<FieldInfo> editableFields = Object.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(fi =>
				{
					ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();
					return attr == null || attr.IsEditable;
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
					Fields.Add(new FieldViewModel(fi, fi.GetValue(Object), this, parentViewModel));
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

					categories[categoryName].Fields.Add(new FieldViewModel(fi, fi.GetValue(Object), this, parentViewModel));
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

			if (parentViewModel != null)
			{
				parentViewModel.OnLanguageChange -= UpdateAllFieldsLanguage;
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
