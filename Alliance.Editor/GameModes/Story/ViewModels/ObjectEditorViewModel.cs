using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using TaleWorlds.Engine;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing an object in the scenario editor.
	/// </summary>
	public class ObjectEditorViewModel : INotifyPropertyChanged
	{
		private static object _clipboardObject;
		private static Type _clipboardType;

		protected ScenarioEditorViewModel ScenarioVM;
		protected FieldViewModel ParentVM;
		public object Object { get; set; }
		public ObservableCollection<FieldViewModel> Fields { get; private set; }
		public ObservableCollection<FieldCategoryViewModel> FieldCategories { get; private set; }
		public string Title { get; set; }
		public string SelectedLanguage => ScenarioVM?.SelectedLanguage ?? "English";
		public WeakGameEntity GameEntity { get; set; }
		public bool CanPaste => _clipboardObject != null && _clipboardType == Object?.GetType();

		public ICommand CopyCommand { get; }
		public ICommand PasteCommand { get; }

		public ObjectEditorViewModel(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title, WeakGameEntity gameEntity)
		{
			CopyCommand = new RelayCommand(_ => CopyObject());
			PasteCommand = new RelayCommand(_ => PasteObject(), _ => CanPaste);
			InitVM(obj, parentVM, scenarioVM, title, gameEntity);
		}

		public ObjectEditorViewModel(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title)
		{
			CopyCommand = new RelayCommand(_ => CopyObject());
			PasteCommand = new RelayCommand(_ => PasteObject(), _ => CanPaste);
			InitVM(obj, parentVM, scenarioVM, title, WeakGameEntity.Invalid);
		}

		public ObjectEditorViewModel()
		{
			CopyCommand = new RelayCommand(_ => CopyObject());
			PasteCommand = new RelayCommand(_ => PasteObject(), _ => CanPaste);

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

			OnPropertyChanged(nameof(CanPaste));
		}

		private void CopyObject()
		{
			if (Object == null) return;

			_clipboardType = Object.GetType();
			_clipboardObject = DeepCloneObject(Object);
			OnPropertyChanged(nameof(CanPaste));
		}

		private void PasteObject()
		{
			if (!CanPaste || _clipboardObject == null || Object == null) return;

			if (_clipboardType != Object.GetType())
			{
				MessageBox.Show($"Cannot paste values from '{_clipboardType?.Name}' into '{Object.GetType().Name}'.", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			CopyObjectState(_clipboardObject, Object);
			RefreshFields();
			OnPropertyChanged(nameof(CanPaste));
		}

		private static object DeepCloneObject(object source)
		{
			if (source == null) return null;

			Type type = source.GetType();

			if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
			{
				return source;
			}

			if (type.IsArray)
			{
				Array sourceArray = (Array)source;
				Type elementType = type.GetElementType();
				Array clonedArray = Array.CreateInstance(elementType, sourceArray.Length);
				for (int i = 0; i < sourceArray.Length; i++)
				{
					clonedArray.SetValue(DeepCloneObject(sourceArray.GetValue(i)), i);
				}
				return clonedArray;
			}

			if (typeof(IDictionary).IsAssignableFrom(type))
			{
				IDictionary sourceDict = (IDictionary)source;
				IDictionary clonedDict = CreateDictionaryInstance(type);
				foreach (DictionaryEntry entry in sourceDict)
				{
					clonedDict.Add(DeepCloneObject(entry.Key), DeepCloneObject(entry.Value));
				}
				return clonedDict;
			}

			if (typeof(IList).IsAssignableFrom(type))
			{
				IList sourceList = (IList)source;
				IList clonedList = CreateListInstance(type);
				foreach (object item in sourceList)
				{
					clonedList.Add(DeepCloneObject(item));
				}
				return clonedList;
			}

			object clone;
			try
			{
				clone = Activator.CreateInstance(type);
			}
			catch
			{
				// Fallback: if no parameterless ctor, keep source reference instead of crashing.
				return source;
			}

			CopyObjectState(source, clone);
			return clone;
		}

		private static IList CreateListInstance(Type listType)
		{
			try
			{
				if (!listType.IsInterface && !listType.IsAbstract)
				{
					return (IList)Activator.CreateInstance(listType);
				}
			}
			catch
			{
			}

			if (listType.IsGenericType)
			{
				Type itemType = listType.GetGenericArguments()[0];
				Type concreteType = typeof(List<>).MakeGenericType(itemType);
				return (IList)Activator.CreateInstance(concreteType);
			}

			return new ArrayList();
		}

		private static IDictionary CreateDictionaryInstance(Type dictionaryType)
		{
			try
			{
				if (!dictionaryType.IsInterface && !dictionaryType.IsAbstract)
				{
					return (IDictionary)Activator.CreateInstance(dictionaryType);
				}
			}
			catch
			{
			}

			if (dictionaryType.IsGenericType)
			{
				Type[] args = dictionaryType.GetGenericArguments();
				Type concreteType = typeof(Dictionary<,>).MakeGenericType(args[0], args[1]);
				return (IDictionary)Activator.CreateInstance(concreteType);
			}

			return new Hashtable();
		}

		private static void CopyObjectState(object source, object target)
		{
			if (source == null || target == null || source.GetType() != target.GetType()) return;

			Type type = source.GetType();

			foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				object sourceValue = field.GetValue(source);
				field.SetValue(target, DeepCloneObject(sourceValue));
			}

			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
				{
					continue;
				}

				object sourceValue = property.GetValue(source);
				object clonedValue = DeepCloneObject(sourceValue);

				if (clonedValue != null && !property.PropertyType.IsAssignableFrom(clonedValue.GetType()))
				{
					continue;
				}

				property.SetValue(target, clonedValue);
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
