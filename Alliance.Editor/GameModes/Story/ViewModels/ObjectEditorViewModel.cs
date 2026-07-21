using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Attributes;
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
		/// <summary>The phrase lines rendered inline (one per declared template string); empty when the type has no template.</summary>
		public ObservableCollection<PhraseLineViewModel> PhraseLines { get; } = new ObservableCollection<PhraseLineViewModel>();
		public bool HasPhrase => PhraseLines.Count > 0;
		public string Title { get; set; }
		public string SelectedLanguage => ScenarioVM?.SelectedLanguage ?? "English";
		public WeakGameEntity GameEntity { get; set; }
		public bool CanPaste => _clipboardObject != null && _clipboardType == Object?.GetType();
		/// <summary>Field names to skip when rendering (e.g., "Position" shown inline by a parent template).</summary>
		public HashSet<string> HiddenFieldNames { get; set; }

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

			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				AgentCountCondition obj = new AgentCountCondition() { TargetCount = new LiteralValue<int>(5) };
				InitVM(obj, null, new ScenarioEditorViewModel(), "Alliance - Scenario Editor", WeakGameEntity.Invalid);
			}
		}

		private void InitVM(object obj, FieldViewModel parentVM, ScenarioEditorViewModel scenarioVM, string title, WeakGameEntity gameEntity)
		{
			ParentVM = parentVM;
			GameEntity = gameEntity;
			Object = obj;
			ScenarioVM = scenarioVM;
			Fields = new ObservableCollection<FieldViewModel>();
			FieldCategories = new ObservableCollection<FieldCategoryViewModel>();

			UnwrapSingleFieldObject(ref obj, ref title, ref gameEntity);
			Object = obj;

			RefreshFields();
			Title = title + " > " + ScenarioEditorHelper.GetItemDisplayName(obj);

			if (ScenarioVM != null)
			{
				ScenarioVM.OnLanguageChange += UpdateAllFieldsLanguage;
			}

			OnPropertyChanged(nameof(CanPaste));
		}

		/// <summary>
		/// If the object wraps a ScriptedEvent with a ParentEntity, use that as the game entity.
		/// If the object has a single non-abstract class field (not string, not Zone), unwrap to edit it directly.
		/// Zone is kept wrapped so its Value field is rendered via FieldTemplate → IsZone → ZoneTemplate (with "Edit Zone" button).
		/// </summary>
		private void UnwrapSingleFieldObject(ref object obj, ref string title, ref WeakGameEntity gameEntity)
		{
			FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

			if (obj is ScriptedEvent scriptedEvent && scriptedEvent.ParentEntity != null)
			{
				gameEntity = scriptedEvent.ParentEntity;
			}
			else if (fieldInfos.Length == 1 && !fieldInfos[0].FieldType.IsAbstract && fieldInfos[0].FieldType.IsClass
				&& fieldInfos[0].FieldType != typeof(string) && fieldInfos[0].FieldType != typeof(Zone))
			{
				var singleField = fieldInfos[0];
				var fieldValue = singleField.GetValue(obj);

				if (fieldValue == null)
				{
					fieldValue = Activator.CreateInstance(singleField.FieldType);
					singleField.SetValue(obj, fieldValue);
				}

				title += " > " + ScenarioEditorHelper.GetItemDisplayName(obj);
				obj = fieldValue;
			}
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
				Array clonedArray = Array.CreateInstance(type.GetElementType(), sourceArray.Length);
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
			HashSet<string> allowedFields = GetAllowedFieldNames();
			List<FieldInfo> editableFields = GetEditableFields(allowedFields);
			Dictionary<string, bool> expandedStates = SaveCategoryExpandedStates();

			CloseAllFieldViewModels();
			Fields.Clear();
			FieldCategories.Clear();

			ParseAndApplyPhrase(editableFields, out HashSet<string> consumedFieldNames);

			Dictionary<string, FieldCategoryViewModel> categories = BuildFieldViewModels(editableFields, consumedFieldNames, expandedStates);

			foreach (var category in categories.Values)
			{
				FieldCategories.Add(category);
			}
		}

		private HashSet<string> GetAllowedFieldNames()
		{
			if (ParentVM?.ParentObject is GameModeSettings settings)
			{
				if (Object is TWConfig)
				{
					return new HashSet<string>(settings.GetAvailableNativeOptions().Select(o => o.ToString()));
				}

				if (Object is Config)
				{
					return new HashSet<string>(settings.GetAvailableModOptions());
				}
			}

			return null;
		}

		private List<FieldInfo> GetEditableFields(HashSet<string> allowedFields)
		{
			return Object.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(fi =>
				{
					ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();
					if (attr != null && !attr.IsEditable) return false;
					if (allowedFields != null && !allowedFields.Contains(fi.Name)) return false;
					return true;
				})
				.ToList();
		}

		private Dictionary<string, bool> SaveCategoryExpandedStates()
		{
			var states = new Dictionary<string, bool>();
			foreach (var category in FieldCategories)
			{
				states[category.Name] = category.IsExpanded;
			}
			return states;
		}

		private void CloseAllFieldViewModels()
		{
			foreach (var field in AllFieldViewModels())
			{
				field.Close();
			}
		}

		private void ParseAndApplyPhrase(List<FieldInfo> editableFields, out HashSet<string> consumedFieldNames)
		{
			List<PhraseLineViewModel> phraseLines = ParsePhrase(editableFields, out consumedFieldNames);
			PhraseLines.Clear();
			foreach (var line in phraseLines)
			{
				PhraseLines.Add(line);
			}
			OnPropertyChanged(nameof(HasPhrase));
		}

		private Dictionary<string, FieldCategoryViewModel> BuildFieldViewModels(List<FieldInfo> editableFields, HashSet<string> consumedFieldNames, Dictionary<string, bool> expandedStates)
		{
			var categories = new Dictionary<string, FieldCategoryViewModel>();

			foreach (FieldInfo fi in editableFields)
			{
				if (!ShouldShowField(fi, consumedFieldNames)) continue;

				FieldViewModel fieldVM = new FieldViewModel(fi, fi.GetValue(Object), this, ScenarioVM);
				CategorizeField(fieldVM, fi, categories, expandedStates);
			}

			return categories;
		}

		private bool ShouldShowField(FieldInfo fi, HashSet<string> consumedFieldNames)
		{
			ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();
			if (attr != null && !attr.IsDependencySatisfied(Object)) return false;
			if (consumedFieldNames.Contains(fi.Name)) return false;
			if (HiddenFieldNames?.Contains(fi.Name) == true) return false;
			return true;
		}

		private void CategorizeField(FieldViewModel fieldVM, FieldInfo fi, Dictionary<string, FieldCategoryViewModel> categories, Dictionary<string, bool> expandedStates)
		{
			ConfigPropertyAttribute attr = fi.GetCustomAttribute<ConfigPropertyAttribute>();

			if (attr == null || attr.Category == null)
			{
				if (HasPhrase)
				{
					GetOrCreateCategory(categories, expandedStates, "Advanced", false).Fields.Add(fieldVM);
				}
				else
				{
					Fields.Add(fieldVM);
				}
			}
			else
			{
				GetOrCreateCategory(categories, expandedStates, attr.Category, attr.Category == "General").Fields.Add(fieldVM);
			}
		}

		private static FieldCategoryViewModel GetOrCreateCategory(Dictionary<string, FieldCategoryViewModel> categories, Dictionary<string, bool> expandedStates, string name, bool defaultExpanded)
		{
			if (!categories.ContainsKey(name))
			{
				bool isExpanded = expandedStates.ContainsKey(name) ? expandedStates[name] : defaultExpanded;
				categories[name] = new FieldCategoryViewModel(name, isExpanded);
			}
			return categories[name];
		}

		private List<PhraseLineViewModel> ParsePhrase(List<FieldInfo> editableFields, out HashSet<string> consumedFieldNames)
		{
			consumedFieldNames = new HashSet<string>();
			List<PhraseLineViewModel> lines = new List<PhraseLineViewModel>();

			PhraseTemplateAttribute phraseAttr = Object?.GetType().GetCustomAttribute<PhraseTemplateAttribute>();
			if (phraseAttr?.Templates == null) return lines;

			foreach (string template in phraseAttr.Templates)
			{
				if (string.IsNullOrWhiteSpace(template)) continue;

				PhraseLineViewModel line = new PhraseLineViewModel();
				ParseTemplateLine(template, editableFields, consumedFieldNames, line.Segments);
				if (line.Segments.Count > 0) lines.Add(line);
			}

			return lines;
		}

		private void ParseTemplateLine(string template, List<FieldInfo> editableFields, HashSet<string> consumedFieldNames, ObservableCollection<PhraseSegment> segments)
		{
			ParseSegments(template, editableFields, consumedFieldNames, segments);
		}

		private void ParseSegments(string text, List<FieldInfo> editableFields, HashSet<string> consumedFieldNames, ObservableCollection<PhraseSegment> segments)
		{
			int i = 0;
			while (i < text.Length)
			{
				int open = text.IndexOf('{', i);

				if (open < 0)
				{
					string tail = text.Substring(i);
					if (!string.IsNullOrEmpty(tail)) segments.Add(new PhraseTextSegment { Text = tail });
					break;
				}

				if (open > i)
				{
					string lit = text.Substring(i, open - i);
					if (!string.IsNullOrEmpty(lit)) segments.Add(new PhraseTextSegment { Text = lit });
				}

				int close = PhraseTextRenderer.FindMatchingBrace(text, open);
				if (close < 0)
				{
					segments.Add(new PhraseTextSegment { Text = text.Substring(open) });
					break;
				}

				string block = text.Substring(open + 1, close - open - 1);
				i = close + 1;

				if (block.StartsWith("?"))
				{
					ParseConditionalSegment(block, editableFields, consumedFieldNames, segments);
					continue;
				}

				ParseFieldSlot(block, editableFields, consumedFieldNames, segments);
			}
		}

		private void ParseConditionalSegment(string block, List<FieldInfo> editableFields, HashSet<string> consumedFieldNames, ObservableCollection<PhraseSegment> segments)
		{
			string inner = block.Substring(1);
			int sep = inner.IndexOf(':');
			string condition = sep >= 0 ? inner.Substring(0, sep).Trim() : inner.Trim();
			string content = sep >= 0 ? inner.Substring(sep + 1) : string.Empty;

			if (PhraseTextRenderer.IsConditionSatisfied(condition, Object))
			{
				ParseSegments(content, editableFields, consumedFieldNames, segments);
			}
		}

		private void ParseFieldSlot(string block, List<FieldInfo> editableFields, HashSet<string> consumedFieldNames, ObservableCollection<PhraseSegment> segments)
		{
			int bar = block.IndexOf('|');
			string fieldName = (bar >= 0 ? block.Substring(0, bar) : block).Trim();
			FieldInfo fi = editableFields.FirstOrDefault(f => f.Name == fieldName);
			ConfigPropertyAttribute attr = fi?.GetCustomAttribute<ConfigPropertyAttribute>();

			if (fi != null && (attr == null || attr.IsDependencySatisfied(Object)))
			{
				consumedFieldNames.Add(fieldName);
				FieldViewModel fieldVM = new FieldViewModel(fi, fi.GetValue(Object), this, ScenarioVM);

				if (bar >= 0)
				{
					string[] choices = block.Substring(bar + 1).Split('|');
					if (choices.Length > 0) fieldVM.ChoiceLabels = choices;
				}

				segments.Add(new PhraseFieldSegment { Field = fieldVM });
			}
			else
			{
				segments.Add(new PhraseTextSegment { Text = "{" + block + "}" });
			}
		}

		private IEnumerable<FieldViewModel> AllFieldViewModels()
		{
			foreach (var field in Fields) yield return field;
			foreach (var category in FieldCategories)
			{
				foreach (var field in category.Fields) yield return field;
			}
			foreach (var line in PhraseLines)
			{
				foreach (var segment in line.Segments)
				{
					if (segment is PhraseFieldSegment fieldSegment) yield return fieldSegment.Field;
				}
			}
		}

		public ScriptedEvent FindEnclosingScriptedEvent()
		{
			return FindEnclosing<ScriptedEvent>();
		}

		public Scenario FindEnclosingScenario()
		{
			return FindEnclosing<Scenario>();
		}

		public Act FindEnclosingAct()
		{
			return FindEnclosing<Act>();
		}

		private T FindEnclosing<T>() where T : class
		{
			for (ObjectEditorViewModel vm = this; vm != null; vm = vm.ParentVM?.parentViewModel)
			{
				if (vm.Object is T result) return result;
			}
			return null;
		}

		internal ObjectEditorViewModel ParentEditor => ParentVM?.parentViewModel;

		internal void RefreshValueSourcePreviews()
		{
			foreach (FieldViewModel field in AllFieldViewModels())
			{
				field.RefreshValueSourceDisplay();
			}
		}

		public void Close()
		{
			foreach (var field in AllFieldViewModels())
			{
				field.Close();
			}

			if (ScenarioVM != null)
			{
				ScenarioVM.OnLanguageChange -= UpdateAllFieldsLanguage;
			}
		}

		public void UpdateAllFieldsLanguage(object sender, EventArgs args)
		{
			foreach (var field in AllFieldViewModels())
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
