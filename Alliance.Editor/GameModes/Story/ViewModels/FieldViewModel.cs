using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Conditions;
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
using System.Windows.Data;
using System.Windows.Input;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;
using static Alliance.Common.Utilities.Logger;

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

		public FieldInfo FieldInfo { get; private set; }
		public string FieldName { get; private set; }
		public Type FieldType { get; private set; }
		public string Label { get; private set; }
		public string Tooltip { get; private set; }
		public bool ShowTooltip { get; private set; }
		public string[] PossibleValues { get; private set; }
		public bool IsMultiChoiceString => PossibleValues != null && PossibleValues.Length > 0;
		/// <summary>When true, the dropdown only allows picking from PossibleValues (no free-text entry).</summary>
		public bool IsChoiceLocked { get; set; }
		public bool IsLocalizedString => typeof(LocalizedString).IsAssignableFrom(FieldType);
		public bool IsComplexType => !FieldType.IsEnum && !IsLocalizedString && !IsCollection && !FieldType.IsPrimitive && FieldType != typeof(string) && FieldType != typeof(bool);
		public bool IsCollection => typeof(IEnumerable).IsAssignableFrom(FieldType) && FieldType != typeof(string);
		public bool IsZone => FieldType == typeof(SerializableZone);
		public bool IsPlayerSpawnMenu => FieldType == typeof(PlayerSpawnMenu);
		public Type[] ConcreteTypes { get; private set; } = Array.Empty<Type>();
		/// <summary>True for abstract-typed fields (e.g. AgentSource): rendered inline with a subtype picker.</summary>
		public bool IsPolymorphicField => FieldType != null && FieldType.IsAbstract && ConcreteTypes.Length > 0;
		public ObservableCollection<ItemViewModel> Items { get; }
		public ZoneViewModel ZoneVM { get; }

		public object ParentObject => parentViewModel?.Object;

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

					if (FieldType == typeof(bool) && ConfigPropertyAttribute.HasDependents(FieldInfo.Name, parentViewModel.Object))
					{
						parentViewModel.RefreshFields();
					}
					else if (parentViewModel.HasPhrase && (FieldType == typeof(bool) || FieldType.IsEnum))
					{
						parentViewModel.RefreshFields();
					}
					else if (parentViewModel?.Object is ScenarioVariable && FieldName == nameof(ScenarioVariable.EnumTypeName))
					{
						parentViewModel.RefreshFields();
					}
					else if (parentViewModel?.Object != null && parentViewModel.Object.GetType()
						.GetFields(BindingFlags.Public | BindingFlags.Instance)
						.Any(f => f.GetCustomAttribute<DependsOnVariableAttribute>()?.SourceField == FieldName))
					{
						parentViewModel.RefreshFields();
					}
				}
			}
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

		/// <summary>
		/// When set, this field renders inline as a dropdown whose items are these labels (phrase-only).
		/// The selected index maps to the underlying value: bool → 0 (false) / 1 (true),
		/// enum → value declaration order.
		/// </summary>
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

		/// <summary>
		/// Gathers the names of available variables matching <paramref name="variableType"/> from all sources:
		/// 1. Trigger variables produced by [VariableOutput] fields on sibling conditions of the enclosing ScriptedEvent.
		/// 2. Global variables declared on the enclosing Scenario (via its Variables list, populated in Phase 2).
		/// Matching follows the exact type or List&lt;type&gt; convention.
		/// </summary>
		private string[] CollectAvailableVariables(Type variableType)
		{
			List<string> names = new List<string>();

			// Source 1: trigger variables from sibling conditions
			ScriptedEvent scriptedEvent = parentViewModel?.FindEnclosingScriptedEvent();
			if (scriptedEvent != null)
			{
				foreach (Condition condition in scriptedEvent.Conditions)
				{
					if (condition == null) continue;
					foreach (FieldInfo f in condition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
					{
						VariableOutputAttribute outAttr = f.GetCustomAttribute<VariableOutputAttribute>();
						if (outAttr?.VariableType == null) continue;

						Type produced = outAttr.VariableType;
						bool typeMatch = variableType.IsAssignableFrom(produced)
							|| (produced.IsGenericType && produced.GetGenericTypeDefinition() == typeof(List<>) && variableType.IsAssignableFrom(produced.GetGenericArguments()[0]));
						if (!typeMatch) continue;

						string name = f.GetValue(condition) as string;
						if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
						{
							names.Add(name);
						}
					}
				}
			}

			// Source 2: global variables from the enclosing Scenario
			Scenario parentScenario = parentViewModel?.FindEnclosingScenario();
			if (parentScenario?.Variables != null)
			{
				foreach (var scVar in parentScenario.Variables)
				{
					if (scVar == null || string.IsNullOrWhiteSpace(scVar.Name)) continue;
					if (!TypeMatchesFilter(variableType, scVar.Type)) continue;
					if (!names.Contains(scVar.Name))
					{
						names.Add(scVar.Name);
					}
				}
			}

			return names.ToArray();
		}

		/// <summary>
		/// Maps a VariableType enum to its closest System.Type for type matching.
		/// </summary>
		private static Type VariableTypeToSystemType(VariableType vt)
		{
			switch (vt)
			{
				case VariableType.Int: return typeof(int);
				case VariableType.Float: return typeof(float);
				case VariableType.Bool: return typeof(bool);
				case VariableType.String: return typeof(string);
				case VariableType.Enum: return typeof(string);
				default: return typeof(object);
			}
		}

		/// <summary>
		/// Checks whether a scenario variable's type matches a given filter type.
		/// </summary>
		private static bool TypeMatchesFilter(Type filterType, VariableType varType)
		{
			if (filterType == typeof(object)) return true;
			Type varSysType = VariableTypeToSystemType(varType);
			if (filterType == varSysType) return true;
			if (filterType == typeof(float) && varSysType == typeof(int)) return true;
			return false;
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

		public ICommand EditCommand { get; }
		public ICommand DeleteCommand { get; }
		public ICommand AddCommand { get; }
		public ICommand EditPlayerSpawnMenuCommand { get; }

		/// <summary>
		/// Command used by the compact inline (phrase) widget to edit complex fields
		/// (zones, nested objects, spawn menus). Resolves to the most appropriate editor for the field type.
		/// </summary>
		public ICommand InlineEditCommand => IsZone ? ZoneVM?.EditZoneCommand : (IsPlayerSpawnMenu ? EditPlayerSpawnMenuCommand : EditCommand);

		public FieldViewModel(FieldInfo fieldInfo, object fieldValue, ObjectEditorViewModel parentViewModel, ScenarioEditorViewModel scenarioEditorViewModel)
		{
			FieldInfo = fieldInfo;
			this.parentViewModel = parentViewModel;
			this.scenarioEditorViewModel = scenarioEditorViewModel;
			FieldName = fieldInfo.Name;
			FieldType = fieldInfo.FieldType;
			_fieldValue = fieldValue;

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
				catch(Exception ex)
				{
					Log($"Error retrieving possible values for field '{FieldName}': {ex.Message}", LogLevel.Error);
					PossibleValues = new string[0];
				}
			}
			else
			{
				Label = FieldName;
			}

			// [VariableRef]: render as a dropdown of available variables from trigger conditions and scenario globals.
			VariableRefAttribute variableRef = fieldInfo.GetCustomAttribute<VariableRefAttribute>();
			if (variableRef != null)
			{
				PossibleValues = CollectAvailableVariables(variableRef.VariableType);
			}

			// ScenarioVariable.EnumTypeName: populate + lock dropdown from discovered enum types.
			if (parentViewModel?.Object is ScenarioVariable sv && FieldName == nameof(ScenarioVariable.EnumTypeName))
			{
				PossibleValues = ScenarioData.AvailableEnumTypes().Select(t => t.Name).ToArray();
				IsChoiceLocked = true;
			}
			// ScenarioVariable.DefaultValue: show enum value dropdown when Type == Enum.
			if (parentViewModel?.Object is ScenarioVariable sv2 && FieldName == nameof(ScenarioVariable.DefaultValue) && sv2.Type == VariableType.Enum && !string.IsNullOrEmpty(sv2.EnumTypeName))
			{
				Type enumType = ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == sv2.EnumTypeName);
				if (enumType != null)
				{
					PossibleValues = Enum.GetNames(enumType);
					IsChoiceLocked = true;
				}
			}
			// ScenarioVariable.DefaultValue: dynamic editor type based on VariableType.
			if (parentViewModel?.Object is ScenarioVariable svForType && FieldName == nameof(ScenarioVariable.DefaultValue))
			{
				switch (svForType.Type)
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
					default:
						break;
				}
			}
			// Fields marked with [DependsOnVariable]: dynamic editor based on the variable referenced by another field.
			DependsOnVariableAttribute depAttr = fieldInfo.GetCustomAttribute<DependsOnVariableAttribute>();
			if (depAttr != null && !string.IsNullOrEmpty(depAttr.SourceField))
			{
				FieldInfo srcField = parentViewModel?.Object?.GetType().GetField(depAttr.SourceField, BindingFlags.Public | BindingFlags.Instance);
				if (srcField != null)
				{
					string varName = srcField.GetValue(parentViewModel.Object) as string;
					if (!string.IsNullOrEmpty(varName))
					{
						Scenario scenario = parentViewModel.FindEnclosingScenario();
						ScenarioVariable matchedVar = scenario?.Variables?.FirstOrDefault(v => v.Name == varName);
						if (matchedVar != null)
						{
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
								default:
									break;
							}
						}
					}
				}
			}

			// Polymorphic (abstract-typed) fields: discover concrete subtypes for the inline type picker.
			if (FieldType != null && FieldType.IsAbstract)
			{
				ConcreteTypes = DiscoverConcreteTypes(FieldType);
			}

			EditCommand = new RelayCommand(_ => EditObjectFromFieldInfo(FieldInfo), _ => IsComplexType);
			DeleteCommand = new RelayCommand(DeleteItem);
			AddCommand = new RelayCommand(_ => AddItem());
			EditPlayerSpawnMenuCommand = new RelayCommand(_ => OpenPlayerSpawnMenu(), _ => IsPlayerSpawnMenu);

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

			if (IsZone)
			{
				ZoneVM = new ZoneViewModel((SerializableZone)FieldValue, FieldInfo, this);
			}
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
			if (FieldValue is IList list)
			{
				var baseType = FieldType.GetGenericArguments()[0];
				var typeToCreate = baseType;

				// Check if the base type is abstract or an interface
				if (baseType.IsAbstract || baseType.IsInterface)
				{
					// Ask the user to select a concrete type
					typeToCreate = OpenTypeSelection(baseType);
				}

				if (typeToCreate == null) return;

				var newItem = Activator.CreateInstance(typeToCreate);
				list.Add(newItem);
				Items.Add(new ItemViewModel(newItem, this));
				OnPropertyChanged(nameof(FieldValue));
			}
		}

		public void EditObject(object obj, ItemViewModel itemViewModel = null)
		{
			var editorWindow = new ObjectEditorWindow(obj,parentViewModel.GameEntity, this, scenarioEditorViewModel, parentViewModel.Title);
			editorWindow.Show();

			// Update DisplayName when the editor window is closed
			editorWindow.Closing += (sender, args) =>
			{
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

				// Check if the base type is abstract or an interface
				if (FieldType.IsAbstract || FieldType.IsInterface)
				{
					// Ask the user to select a concrete type
					typeToCreate = OpenTypeSelection(FieldType);
				}

				if (typeToCreate == null) return;

				obj = Activator.CreateInstance(typeToCreate);
				fieldInfo.SetValue(parentViewModel.Object, obj);
			}

			EditObject(obj);
		}

		/// <summary>
		/// Open a dialog to select among the list of concrete types that inherit from the base type.
		/// </summary>
		public Type OpenTypeSelection(Type baseType)
		{
			// Retrieve all types that inherit from the base type
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

					// Add successfully loaded types only
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

				// Show the dialog
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

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private ObjectEditorViewModel _nestedVM;

		/// <summary>Inline editor for a polymorphic (abstract-typed) field: renders the chosen instance's fields directly.</summary>
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

		/// <summary>The concrete type of the current value; setting it swaps the instance (changes the source).</summary>
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

		private static Type[] DiscoverConcreteTypes(Type baseType)
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

			// Return the list of enum values
			return Enum.GetValues(enumType).Cast<Enum>().ToList();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
