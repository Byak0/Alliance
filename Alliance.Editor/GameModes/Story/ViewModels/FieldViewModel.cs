using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Editor.GameModes.Story.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;
using static Alliance.Common.Utilities.Logger;
using Condition = Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing a single field in a scenario or any object.
	/// </summary>
	public class FieldViewModel : INotifyPropertyChanged
	{
		internal readonly ObjectEditorViewModel parentViewModel;
		internal readonly ScenarioEditorViewModel scenarioEditorViewModel;
		private object _fieldValue;
		private Func<object, object> _valueToEffective;
		private Func<object, object> _valueFromEffective;
		private ValueSourceChipViewModel _valueSourceChip;

		public FieldInfo FieldInfo { get; private set; }
		public string FieldName { get; private set; }
		public Type FieldType { get; private set; }
		public string Label { get; private set; }
		public string Tooltip { get; private set; }
		public bool ShowTooltip { get; private set; }
		public string[] PossibleValues { get; private set; }
		public bool IsMultiChoiceString => PossibleValues != null && PossibleValues.Length > 0;
		public bool IsChoiceLocked { get; set; }
		public bool IsLocalizedString => typeof(LocalizedString).IsAssignableFrom(FieldType);
		public bool IsValueSource => ValueSourceTypeSupport.IsValueSourceType(FieldType);
		public bool IsComplexType => !IsValueSource && !FieldType.IsEnum && !IsLocalizedString && !IsCollection && !FieldType.IsPrimitive && FieldType != typeof(string) && FieldType != typeof(bool);
		public bool IsCollection => typeof(IEnumerable).IsAssignableFrom(FieldType) && FieldType != typeof(string);
		public bool IsZone => FieldType == typeof(Zone);
		public bool IsPlayerSpawnMenu => FieldType == typeof(PlayerSpawnMenu);
		public Type[] ConcreteTypes { get; private set; } = Array.Empty<Type>();
		public bool IsPolymorphicField => FieldType != null && FieldType.IsAbstract && ConcreteTypes.Length > 0;
		public ObservableCollection<ItemViewModel> Items { get; }
		public ZoneViewModel ZoneVM { get; private set; }
		public ValueSourceChipViewModel ValueSourceChip => _valueSourceChip ??= new ValueSourceChipViewModel(this);

		public object ParentObject => parentViewModel?.Object;

		private bool _isPopupOpen;

		public bool IsPopupOpen
		{
			get => _isPopupOpen;
			set
			{
				if (_isPopupOpen != value)
				{
					_isPopupOpen = value;
					OnPropertyChanged(nameof(IsPopupOpen));
				}
			}
		}

		public object FieldValue
		{
			get
			{
				if (_valueToEffective != null)
					return _valueToEffective(_fieldValue);
				return _fieldValue;
			}
			set
			{
				object storageValue = _valueFromEffective != null ? _valueFromEffective(value) : value;
				if (!object.Equals(_fieldValue, storageValue))
				{
					_fieldValue = storageValue;
					FieldInfo.SetValue(parentViewModel.Object, _fieldValue);
					OnPropertyChanged(nameof(FieldValue));
					_valueSourceChip?.Refresh();

					for (ObjectEditorViewModel vm = parentViewModel; vm != null; vm = vm.ParentEditor)
					{
						vm.RefreshValueSourcePreviews();
					}

					if (ShouldRefreshParentOnChange())
					{
						parentViewModel.RefreshFields();
					}
				}
			}
		}

		private bool ShouldRefreshParentOnChange()
		{
			if (FieldType == typeof(bool) && ConfigPropertyAttribute.HasDependents(FieldInfo.Name, parentViewModel.Object))
				return true;

			if (parentViewModel.HasPhrase && (FieldType == typeof(bool) || FieldType.IsEnum))
				return true;

			if (parentViewModel?.Object is ScenarioVariable && FieldName == nameof(ScenarioVariable.EnumTypeName))
				return true;

			if (parentViewModel?.Object != null && parentViewModel.Object.GetType()
				.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Any(f => f.GetCustomAttribute<DependsOnVariableAttribute>()?.SourceField == FieldName))
				return true;

			return false;
		}

		public string LocalizedText
		{
			get => (FieldValue as LocalizedString)?.GetText(parentViewModel.SelectedLanguage);
			set
			{
				if (FieldInfo.FieldType != typeof(LocalizedString)) return;

				FieldValue ??= new LocalizedString(value);
				((LocalizedString)FieldValue).SetText(parentViewModel.SelectedLanguage, value);
				OnPropertyChanged(nameof(LocalizedText));
			}
		}

		private string[] _choiceLabels;
		private object[] _enumOrderedValues;

		public string[] ChoiceLabels
		{
			get => _choiceLabels;
			set
			{
				_choiceLabels = value;
				OnPropertyChanged(nameof(IsChoice));
				OnPropertyChanged(nameof(SelectedChoiceIndex));
			}
		}

		public bool IsChoice => _choiceLabels != null && _choiceLabels.Length > 0;

		public int SelectedChoiceIndex
		{
			get
			{
				if (_choiceLabels == null || FieldValue == null) return -1;
				if (FieldType == typeof(bool)) return (bool)FieldValue ? 1 : 0;
				if (FieldType.IsEnum) return Array.IndexOf(GetEnumOrderedValues(), FieldValue);
				return -1;
			}
			set
			{
				if (_choiceLabels == null) return;
				object newValue = null;
				if (FieldType == typeof(bool)) newValue = (object)(value == 1);
				else if (FieldType.IsEnum)
				{
					object[] ordered = GetEnumOrderedValues();
					if (value >= 0 && value < ordered.Length) newValue = ordered[value];
				}
				if (newValue != null)
				{
					FieldValue = newValue;
					OnPropertyChanged(nameof(SelectedChoiceIndex));
				}
			}
		}

		public ICommand EditCommand { get; }
		public ICommand DeleteCommand { get; }
		public ICommand AddCommand { get; }
		public ICommand EditPlayerSpawnMenuCommand { get; }
		public ICommand InlineEditCommand => IsPlayerSpawnMenu ? EditPlayerSpawnMenuCommand : EditCommand;

		public FieldViewModel(FieldInfo fieldInfo, object fieldValue, ObjectEditorViewModel parentViewModel, ScenarioEditorViewModel scenarioEditorViewModel)
		{
			// Core field info
			FieldInfo = fieldInfo;
			this.parentViewModel = parentViewModel;
			this.scenarioEditorViewModel = scenarioEditorViewModel;
			FieldName = fieldInfo.Name;
			FieldType = fieldInfo.FieldType;
			_fieldValue = fieldValue;

			ApplyConfigPropertyAttribute(fieldInfo);
			ApplyVariableReferenceDropdowns(fieldInfo);
			ApplyScenarioVariableFields();
			ApplyDependsOnVariableAttribute(fieldInfo);
			DiscoverPolymorphicTypes();
			FilterFunctionCallTypes();

			// Commands
			EditCommand = new RelayCommand(_ => EditObjectFromFieldInfo(FieldInfo), _ => IsComplexType);
			DeleteCommand = new RelayCommand(DeleteItem);
			AddCommand = new RelayCommand(_ => AddItem());
			EditPlayerSpawnMenuCommand = new RelayCommand(_ => OpenPlayerSpawnMenu(), _ => IsPlayerSpawnMenu);

			// Collection items
			if (IsCollection && FieldValue is IEnumerable enumerable)
			{
				Items = new ObservableCollection<ItemViewModel>(
					enumerable.Cast<object>().Select(item => new ItemViewModel(item, this))
				);
			}
			else
			{
				Items = new ObservableCollection<ItemViewModel>();
			}

			InitializeZoneViewModel();
		}

		private void ApplyConfigPropertyAttribute(FieldInfo fieldInfo)
		{
			var attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();

			if (attribute != null)
			{
				Label = attribute.Label ?? FieldName;
				Tooltip = attribute.Tooltip;
				ShowTooltip = !string.IsNullOrEmpty(Tooltip);
				try
				{
					PossibleValues = attribute.PossibleValues;
				}
				catch (Exception ex)
				{
					Log($"Error retrieving possible values for field '{FieldName}': {ex.Message}", LogLevel.Error);
					PossibleValues = new string[0];
				}
			}
			else
			{
				Label = FieldName;
			}
		}

		private void ApplyVariableReferenceDropdowns(FieldInfo fieldInfo)
		{
			VariableRefAttribute variableRef = fieldInfo.GetCustomAttribute<VariableRefAttribute>();
			if (variableRef != null)
			{
				PossibleValues = CollectAvailableVariables(variableRef.VariableType);
				IsChoiceLocked = true;
			}

			if (parentViewModel?.Object != null
				&& parentViewModel.Object.GetType().IsGenericType
				&& parentViewModel.Object.GetType().GetGenericTypeDefinition() == typeof(VariableValue<>)
				&& FieldName == nameof(VariableValue<int>.VariableName))
			{
				Type valueType = parentViewModel.Object.GetType().GetGenericArguments()[0];
				PossibleValues = CollectAvailableVariables(valueType);
				IsChoiceLocked = true;
			}
		}

		private void ApplyScenarioVariableFields()
		{
			if (!(parentViewModel?.Object is ScenarioVariable sv)) return;

			if (FieldName == nameof(ScenarioVariable.EnumTypeName))
			{
				PossibleValues = ScenarioData.AvailableEnumTypes().Select(t => t.Name).ToArray();
				IsChoiceLocked = true;
			}

			if (FieldName == nameof(ScenarioVariable.DefaultValue))
			{
				ApplyScenarioVariableDefaultValue(sv);
			}
		}

		private void ApplyScenarioVariableDefaultValue(ScenarioVariable sv)
		{
			if (sv.Type == VariableType.Enum && !string.IsNullOrEmpty(sv.EnumTypeName))
			{
				Type enumType = ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == sv.EnumTypeName);
				if (enumType != null)
				{
					PossibleValues = Enum.GetNames(enumType);
					IsChoiceLocked = true;
				}
			}

			switch (sv.Type)
			{
				case VariableType.Int:
					FieldType = typeof(int);
					_valueToEffective = v => int.TryParse(v?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : 0;
					_valueFromEffective = v => ((int)v).ToString(CultureInfo.InvariantCulture);
					break;
				case VariableType.Float:
					FieldType = typeof(float);
					_valueToEffective = v => float.TryParse(v?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;
					_valueFromEffective = v => ((float)v).ToString(CultureInfo.InvariantCulture);
					break;
				case VariableType.Bool:
					FieldType = typeof(bool);
					_valueToEffective = v => bool.TryParse(v?.ToString(), out bool b) && b;
					_valueFromEffective = v => ((bool)v).ToString().ToLower();
					break;
			}
		}

		private void ApplyDependsOnVariableAttribute(FieldInfo fieldInfo)
		{
			DependsOnVariableAttribute depAttr = fieldInfo.GetCustomAttribute<DependsOnVariableAttribute>();
			if (depAttr == null || string.IsNullOrEmpty(depAttr.SourceField)) return;

			FieldInfo srcField = parentViewModel?.Object?.GetType().GetField(depAttr.SourceField, BindingFlags.Public | BindingFlags.Instance);
			if (srcField == null) return;

			string varName = srcField.GetValue(parentViewModel.Object) as string;
			if (string.IsNullOrEmpty(varName)) return;

			Scenario scenario = parentViewModel.FindEnclosingScenario();
			ScenarioVariable matchedVar = scenario?.Variables?.FirstOrDefault(v => v.Name == varName);
			if (matchedVar == null) return;

			switch (matchedVar.Type)
			{
				case VariableType.Int:
					FieldType = typeof(int);
					_valueToEffective = v => int.TryParse(v?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : 0;
					_valueFromEffective = v => ((int)v).ToString(CultureInfo.InvariantCulture);
					break;
				case VariableType.Float:
					FieldType = typeof(float);
					_valueToEffective = v => float.TryParse(v?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;
					_valueFromEffective = v => ((float)v).ToString(CultureInfo.InvariantCulture);
					break;
				case VariableType.Bool:
					FieldType = typeof(bool);
					_valueToEffective = v => bool.TryParse(v?.ToString(), out bool b) && b;
					_valueFromEffective = v => ((bool)v).ToString().ToLower();
					break;
				case VariableType.Enum:
					if (!string.IsNullOrEmpty(matchedVar.EnumTypeName))
					{
						Type enumType = ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == matchedVar.EnumTypeName);
						if (enumType != null)
						{
							PossibleValues = Enum.GetNames(enumType);
							IsChoiceLocked = true;
						}
					}
					break;
			}
		}

		private void DiscoverPolymorphicTypes()
		{
			if (FieldType != null && FieldType.IsAbstract && !IsValueSource)
			{
				ConcreteTypes = DiscoverConcreteTypes(FieldType);
			}
		}

		private void FilterFunctionCallTypes()
		{
			if (FieldType != typeof(Function)
				|| parentViewModel?.Object == null
				|| !parentViewModel.Object.GetType().IsGenericType
				|| parentViewModel.Object.GetType().GetGenericTypeDefinition() != typeof(FunctionCall<>))
			{
				return;
			}

			Type resultType = parentViewModel.Object.GetType().GetGenericArguments()[0];
			ConcreteTypes = ConcreteTypes
				.Where(type => FunctionReturns(type, resultType))
				.OrderBy(type => type.Name)
				.ToArray();
		}

		private void InitializeZoneViewModel()
		{
			if (IsZone)
			{
				ZoneVM = new ZoneViewModel((Zone)FieldValue, FieldInfo, this);
			}
		}

		internal string[] CollectAvailableVariables(Type variableType)
		{
			List<string> names = new List<string>();

			if (variableType == typeof(Zone))
			{
				CollectZoneNames(names);
				return names.ToArray();
			}

			CollectTriggerVariableNames(variableType, names);
			CollectGlobalVariableNames(variableType, names);

			return names.ToArray();
		}

		private void CollectZoneNames(List<string> names)
		{
			Act parentAct = parentViewModel?.FindEnclosingAct();
			if (parentAct?.Zones != null)
			{
				foreach (var zone in parentAct.Zones)
				{
					if (zone == null || string.IsNullOrWhiteSpace(zone.Name)) continue;
					if (!names.Contains(zone.Name)) names.Add(zone.Name);
				}
			}
		}

		private void CollectTriggerVariableNames(Type variableType, List<string> names)
		{
			ScriptedEvent scriptedEvent = parentViewModel?.FindEnclosingScriptedEvent();
			if (scriptedEvent == null) return;

			foreach (Condition condition in scriptedEvent.Conditions)
			{
				if (condition == null) continue;

				foreach (FieldInfo f in condition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					VariableOutputAttribute outAttr = f.GetCustomAttribute<VariableOutputAttribute>();
					if (outAttr?.VariableType == null) continue;
					if (!variableType.IsAssignableFrom(outAttr.VariableType)) continue;

					string name = f.GetValue(condition) as string;
					if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
					{
						names.Add(name);
					}
				}
			}
		}

		private void CollectGlobalVariableNames(Type variableType, List<string> names)
		{
			Scenario parentScenario = parentViewModel?.FindEnclosingScenario();
			if (parentScenario?.Variables == null) return;

			foreach (var scVar in parentScenario.Variables)
			{
				if (scVar == null || string.IsNullOrWhiteSpace(scVar.Name)) continue;
				if (!TypeMatchesFilter(variableType, scVar.Type)) continue;
				if (!names.Contains(scVar.Name)) names.Add(scVar.Name);
			}
		}

		private static bool TypeMatchesFilter(Type filterType, VariableType varType)
		{
			return varType switch
			{
				VariableType.Int => filterType == typeof(int) || filterType == typeof(float),
				VariableType.Float => filterType == typeof(float),
				VariableType.Bool => filterType == typeof(bool),
				VariableType.String => filterType == typeof(string),
				VariableType.Enum => filterType == typeof(string),
				_ => filterType == typeof(object),
			};
		}

		private object[] GetEnumOrderedValues()
		{
			if (_enumOrderedValues == null && FieldType != null && FieldType.IsEnum)
			{
				_enumOrderedValues = FieldType.GetFields(BindingFlags.Public | BindingFlags.Static)
					.Select(f => f.GetValue(null))
					.ToArray();
			}
			return _enumOrderedValues;
		}

		public void DeleteItem(object item)
		{
			if (FieldValue is IList list && item is ItemViewModel viewModel)
			{
				list.Remove(viewModel.Item);
				Items.Remove(viewModel);
				OnPropertyChanged(nameof(FieldValue));
			}
		}

		public void AddItem()
		{
			if (IsCollection && FieldValue == null)
			{
				FieldValue = Activator.CreateInstance(FieldType);
			}

			if (!(FieldValue is IList list)) return;

			var baseType = FieldType.GetGenericArguments()[0];
			var typeToCreate = baseType;

			if (baseType.IsAbstract || baseType.IsInterface)
			{
				if (ValueSourceTypeSupport.IsValueSourceType(baseType))
				{
					var candidates = ValueSourceTypeSupport.GetConcreteTypes(baseType)
						.Where(t => !t.IsAbstract)
						.ToList();

					if (candidates.Count > 0)
					{
						var vm = new TypeSelectionViewModel(candidates);
						var form = new TypeSelectionForm { DataContext = vm };
						if (form.ShowDialog() == true && vm.SelectedType != null)
							typeToCreate = vm.SelectedType;
						else
							return;
					}
				}
				else
				{
					typeToCreate = OpenTypeSelection(baseType);
				}
			}

			if (typeToCreate == null) return;

			var newItem = Activator.CreateInstance(typeToCreate);
			list.Add(newItem);
			Items.Add(new ItemViewModel(newItem, this));
			OnPropertyChanged(nameof(FieldValue));
		}

		public void EditObject(object obj, ItemViewModel itemViewModel = null)
		{
			var editorWindow = new ObjectEditorWindow(obj, parentViewModel.GameEntity, this, scenarioEditorViewModel, parentViewModel.Title);

			if (itemViewModel != null)
			{
				itemViewModel.IsPopupOpen = true;
			}
			else
			{
				IsPopupOpen = true;
			}

			editorWindow.Show();

			editorWindow.Closing += (sender, args) =>
			{
				if (itemViewModel != null)
				{
					itemViewModel.IsPopupOpen = false;
				}
				else
				{
					IsPopupOpen = false;
				}
				itemViewModel?.OnClose();
				OnPropertyChanged(nameof(FieldValue));
			};
		}

		public void EditObjectFromFieldInfo(FieldInfo fieldInfo)
		{
			var obj = fieldInfo.GetValue(parentViewModel.Object);

			if (obj == null)
			{
				var typeToCreate = FieldType;

				if (FieldType.IsAbstract || FieldType.IsInterface)
				{
					typeToCreate = OpenTypeSelection(FieldType);
				}

				if (typeToCreate == null) return;

				obj = Activator.CreateInstance(typeToCreate);
				fieldInfo.SetValue(parentViewModel.Object, obj);
			}

			EditObject(obj);
		}

		public Type OpenTypeSelection(Type baseType)
		{
			var concreteTypes = new List<Type>();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					var types = assembly.GetTypes()
						.Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);

					concreteTypes.AddRange(types);
				}
				catch (ReflectionTypeLoadException ex)
				{
					foreach (var loaderException in ex.LoaderExceptions)
					{
						Log(loaderException.Message, LogLevel.Error);
					}

					var validTypes = ex.Types.Where(t => t != null && baseType.IsAssignableFrom(t) && !t.IsAbstract);
					concreteTypes.AddRange(validTypes);
				}
			}

			if (concreteTypes.Count > 0)
			{
				var typeSelectionViewModel = new TypeSelectionViewModel(concreteTypes);
				var typeSelectionForm = new TypeSelectionForm
				{
					DataContext = typeSelectionViewModel
				};

				bool? result = typeSelectionForm.ShowDialog();
				if (result == true && typeSelectionViewModel.SelectedType != null)
				{
					return typeSelectionViewModel.SelectedType;
				}
			}

			return null;
		}

		public void OpenPlayerSpawnMenu()
		{
			if (FieldValue is PlayerSpawnMenu menu)
			{
				EditorToolsManager.OpenPlayerSpawnMenu(menu, OnPlayerMenuClosed);
			}
			else
			{
				Log("Field value is not a valid PlayerSpawnMenu instance.", LogLevel.Error);
			}
		}

		private void OnPlayerMenuClosed(PlayerSpawnMenu menu)
		{
			if (menu != null)
			{
				FieldValue = menu;
				OnPropertyChanged(nameof(FieldValue));
			}
			else
			{
				Log("PlayerSpawnMenu was closed without changes.", LogLevel.Warning);
			}
		}

		private ObjectEditorViewModel _nestedVM;

		public ObjectEditorViewModel NestedVM
		{
			get
			{
				if (_nestedVM == null && IsPolymorphicField && FieldValue != null)
				{
					_nestedVM = new ObjectEditorViewModel(FieldValue, this, scenarioEditorViewModel, "", parentViewModel.GameEntity);
				}
				return _nestedVM;
			}
		}

		public Type SelectedConcreteType
		{
			get => FieldValue?.GetType();
			set
			{
				if (value == null) return;
				if (FieldValue != null && FieldValue.GetType() == value) return;
				FieldValue = Activator.CreateInstance(value);
				_nestedVM?.Close();
				_nestedVM = null;
				OnPropertyChanged(nameof(SelectedConcreteType));
				OnPropertyChanged(nameof(NestedVM));
			}
		}

		internal void OpenValueSourceEditor()
		{
			if (!IsValueSource) return;
			ValueSourceEditorPopup popup = new ValueSourceEditorPopup(this)
			{
				Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
					?? Application.Current?.MainWindow
			};
			popup.Closed += (_, _) => RefreshValueSourceDisplay();
			popup.Show();
		}

		internal void RefreshValueSourceDisplay()
		{
			_valueSourceChip?.Refresh();
			OnPropertyChanged(nameof(FieldValue));
		}

		internal static bool FunctionReturns(Type functionType, Type resultType)
		{
			if (functionType == null || resultType == null || functionType.IsAbstract) return false;
			try
			{
				if (Activator.CreateInstance(functionType) is Function function)
				{
					return resultType.IsAssignableFrom(function.ReturnType);
				}
			}
			catch
			{
			}
			return false;
		}

		internal static Type[] DiscoverConcreteTypes(Type baseType)
		{
			List<Type> types = new List<Type>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (var t in assembly.GetTypes())
					{
						if (baseType.IsAssignableFrom(t) && !t.IsAbstract) types.Add(t);
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (ex.Types != null)
					{
						foreach (var t in ex.Types)
						{
							if (t != null && baseType.IsAssignableFrom(t) && !t.IsAbstract) types.Add(t);
						}
					}
				}
				catch { }
			}
			return types.ToArray();
		}

		public void Close()
		{
			ZoneVM?.Close();
			_nestedVM?.Close();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class RelayCommand : ICommand
	{
		private readonly Action<object> _execute;
		private readonly Func<object, bool> _canExecute;

		public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

		public void Execute(object parameter) => _execute(parameter);

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

	public class EnumToListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return null;

			Type enumType = value.GetType();
			if (!enumType.IsEnum) throw new InvalidOperationException("Value must be an enum type");

			return Enum.GetValues(enumType).Cast<Enum>().ToList();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
