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
using ValueSource = Alliance.Common.GameModes.Story.Models.ValueSource;

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
		private object _redirectParent;
		private Func<object, object> _valueToEffective;
		private Func<object, object> _valueFromEffective;
		private ValueSourceChipViewModel _valueSourceChip;
		private ObjectEditorWindow _activeEditorWindow;
		private readonly Dictionary<ItemViewModel, ObjectEditorWindow> _activeItemEditors = new();
		private Window _activeValueSourcePopup;
		private readonly Dictionary<ItemViewModel, ValueSourceEditorPopup> _activeValueSourcePopups = new();

		public FieldInfo FieldInfo { get; private set; }
		public string FieldName { get; private set; }
		public Type FieldType { get; private set; }
		public string Label { get; private set; }
		public string Tooltip { get; private set; }
		public bool ShowTooltip { get; private set; }
		private string[] _possibleValues;
		public string[] PossibleValues
		{
			get => _possibleValues;
			set
			{
				if (_possibleValues != value)
				{
					_possibleValues = value;
					OnPropertyChanged(nameof(PossibleValues));
					OnPropertyChanged(nameof(IsMultiChoiceString));
				}
			}
		}
		public bool IsMultiChoiceString => PossibleValues != null && PossibleValues.Length > 0;
		public bool IsChoiceLocked { get; set; }
		public bool IsLocalizedString => typeof(LocalizedString).IsAssignableFrom(FieldType);
		public bool IsValueSource => ValueSourceHelper.IsValueSourceType(FieldType);
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

		public void SetRedirectParent(object redirectParent)
		{
			_redirectParent = redirectParent;
		}

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
					object target = _redirectParent ?? parentViewModel.Object;
					FieldInfo.SetValue(target, _fieldValue);
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
			if (ConfigPropertyAttribute.HasDependents(FieldInfo.Name, parentViewModel.Object))
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

			// Read and apply config attribute (Label, Tooltip, PossibleValues)
			ApplyConfigPropertyAttribute(fieldInfo);

			// Collect possible values for variable reference dropdowns, if applicable
			ApplyVariableReferenceDropdowns(fieldInfo);

			// Special handling for ScenarioVariable fields
			ApplyScenarioVariableFields();

			// Special handling for fields that depend on another variable's type
			ApplyDependsOnVariableAttribute(fieldInfo);

			// Discover concrete types for polymorphic fields (abstract/interface)
			DiscoverPolymorphicTypes();

			// Filter concrete types for FunctionCall<T> fields to only those that return the correct type
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
			Scenario scenario = parentViewModel?.FindEnclosingScenario();
			Act act = parentViewModel?.FindEnclosingAct();
			object localObject = (object)parentViewModel?.FindEnclosingScriptedEvent() ?? parentViewModel?.FindEnclosingAct();

			VariableRefAttribute variableRef = fieldInfo.GetCustomAttribute<VariableRefAttribute>();
			if (variableRef != null)
			{
				PossibleValues = ValueSourceHelper.CollectAvailableVariables(variableRef.VariableType, scenario, act, localObject);
				IsChoiceLocked = true;
			}

			if (parentViewModel?.Object != null
				&& parentViewModel.Object.GetType().IsGenericType
				&& parentViewModel.Object.GetType().GetGenericTypeDefinition() == typeof(VariableValue<>)
				&& FieldName == nameof(VariableValue<int>.VariableName))
			{
				Type valueType = parentViewModel.Object.GetType().GetGenericArguments()[0];
				object objectToIgnoreVarFrom = parentViewModel?.ParentEditor?.Object;
				PossibleValues = ValueSourceHelper.CollectAvailableVariables(valueType, scenario, act, localObject, objectToIgnoreVarFrom);
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

			// If target field is a ValueSource, we'll need to set it to ValueSource<T> where T is the type of the matched variable.
			bool isValueSource = ValueSourceHelper.IsValueSourceType(FieldType);			
			if(isValueSource)
			{
				// Get CLR type from variable definition (resolve actual enum type, not typeof(Enum))
				Type clrType = matchedVar.Type == VariableType.Enum
					? (ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == matchedVar.EnumTypeName) ?? typeof(string))
					: ScenarioData.GetVariableType(matchedVar.Type);
				Type abstractValueSource = typeof(ValueSource<>).MakeGenericType(clrType);

				// Only create new instance if existing value is null or wrong type
				if (FieldValue == null || FieldValue.GetType().GetGenericTypeDefinition() != typeof(ValueSource<>)
					&& !abstractValueSource.IsAssignableFrom(FieldValue.GetType()))
				{
					// Use the helper to get initial concrete type (literal/variable/function)
					Type targetConcrete = ValueSourceHelper.GetValueSourceTypeToCreate(
					clrType, scenario,
					parentViewModel.FindEnclosingAct(),
					parentViewModel.FindEnclosingScriptedEvent(),
					parentViewModel?.ParentEditor?.Object);
					var newItem = Activator.CreateInstance(targetConcrete);
					if (targetConcrete.GetGenericTypeDefinition() == typeof(LiteralValue<>))
					{
						Type valueType = targetConcrete.GetGenericArguments()[0];
						object defaultValue = ValueSourceHelper.CreateDefaultLiteralValue(valueType);
						if (defaultValue != null)
							targetConcrete.GetField(nameof(LiteralValue<int>.Value))?.SetValue(newItem, defaultValue);
					}
					FieldValue = newItem;
				}

				FieldType = abstractValueSource;

				// If enum, set PossibleValues for the literal sub-editor
				if (matchedVar.Type == VariableType.Enum)
				{
					Type enumType = !string.IsNullOrEmpty(matchedVar.EnumTypeName) ?
									ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == matchedVar.EnumTypeName) : null;
					if(enumType != null) PossibleValues = Enum.GetNames(enumType);
				}

				return;
			}

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
				ConcreteTypes = ValueSourceHelper.DiscoverConcreteTypes(FieldType);
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
				.Where(type => ValueSourceHelper.FunctionReturns(type, resultType))
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
				RefreshItemDisplayNames();
				OnPropertyChanged(nameof(FieldValue));
			}
		}

		public void MoveItem(int fromIndex, int toIndex)
		{
			if (fromIndex == toIndex) return;
			if (!(FieldValue is IList list)) return;
			if (fromIndex < 0 || fromIndex >= Items.Count || toIndex < 0 || toIndex > Items.Count) return;

			var item = Items[fromIndex].Item;

			list.RemoveAt(fromIndex);
			int insertIndex = toIndex > fromIndex ? toIndex - 1 : toIndex;
			list.Insert(insertIndex, item);

			Items.Move(fromIndex, toIndex > fromIndex ? toIndex - 1 : toIndex);

			RefreshItemDisplayNames();
			OnPropertyChanged(nameof(FieldValue));
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
				if (ValueSourceHelper.IsValueSourceType(baseType))
				{
					Scenario scenario = parentViewModel?.FindEnclosingScenario();
					Act act = parentViewModel?.FindEnclosingAct();
					object localObject = (object) parentViewModel?.FindEnclosingScriptedEvent() ?? act;
					object objectToIgnore = parentViewModel?.ParentEditor?.Object;
					typeToCreate = ValueSourceHelper.GetValueSourceTypeToCreate(baseType, scenario, act, localObject, objectToIgnore);
				}
				else
				{
					typeToCreate = OpenTypeSelection(baseType);
				}
			}

			if (typeToCreate == null) return;

			var newItem = Activator.CreateInstance(typeToCreate);

			if (typeToCreate.IsGenericType && typeToCreate.GetGenericTypeDefinition() == typeof(LiteralValue<>))
			{
				Type valueType = typeToCreate.GetGenericArguments()[0];
				object defaultValue = ValueSourceHelper.CreateDefaultLiteralValue(valueType);
				if (defaultValue != null)
				{
					typeToCreate.GetField(nameof(LiteralValue<int>.Value))?.SetValue(newItem, defaultValue);
				}
			}

			list.Add(newItem);
			Items.Add(new ItemViewModel(newItem, this));
			RefreshItemDisplayNames();
			OnPropertyChanged(nameof(FieldValue));
		}

		private void RefreshItemDisplayNames()
		{
			foreach (var item in Items)
				item.UpdateDisplayName();
		}

		public void EditObject(object obj, ItemViewModel itemViewModel = null)
		{
			if (itemViewModel != null)
			{
				if (_activeItemEditors.TryGetValue(itemViewModel, out var existingWindow) && existingWindow.IsLoaded)
				{
					existingWindow.Focus();
					return;
				}
			}
			else
			{
				if (_activeEditorWindow != null && _activeEditorWindow.IsLoaded)
				{
					_activeEditorWindow.Focus();
					return;
				}
			}

			if (IsCollection && itemViewModel != null && obj is ValueSource)
			{
				OpenValueSourceEditorForListItem(itemViewModel);
				return;
			}

			if (IsCollection && itemViewModel != null)
			{
				var vsField = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(f => f.GetCustomAttribute<InlineContentAttribute>() != null
									  && ValueSourceHelper.IsValueSourceType(f.FieldType));
				if (vsField != null)
				{
					OpenValueSourceEditorForWrappedItem(itemViewModel, obj, vsField);
					return;
				}
			}

			var editorWindow = new ObjectEditorWindow(obj, parentViewModel.GameEntity, this, scenarioEditorViewModel, parentViewModel.Title);

			if (itemViewModel != null)
			{
				_activeItemEditors[itemViewModel] = editorWindow;
				itemViewModel.IsPopupOpen = true;
			}
			else
			{
				_activeEditorWindow = editorWindow;
				IsPopupOpen = true;
			}

			editorWindow.Show();

			editorWindow.Closing += (sender, args) =>
			{
				if (itemViewModel != null)
				{
					_activeItemEditors.Remove(itemViewModel);
					itemViewModel.IsPopupOpen = false;
				}
				else
				{
					_activeEditorWindow = null;
					IsPopupOpen = false;
				}
				itemViewModel?.OnClose();
				OnPropertyChanged(nameof(FieldValue));
			};
		}

		private void OpenValueSourceEditorForListItem(ItemViewModel itemVM)
		{
			if (_activeValueSourcePopups.TryGetValue(itemVM, out var existing) && existing.IsLoaded)
			{
				existing.Focus();
				return;
			}

			Type valueSourceType = FieldType.GetGenericArguments()[0];
			IList list = FieldValue as IList;

			Func<object> getter = () => itemVM.Item;
			Action<object> setter = newValue =>
			{
				if (list == null) return;
				int index = list.IndexOf(itemVM.Item);
				if (index >= 0)
				{
					list[index] = newValue;
					itemVM.ReplaceItem(newValue);
					OnPropertyChanged(nameof(FieldValue));
				}
			};

			var viewModel = new ValueSourceEditorViewModel(
				valueSourceType, getter, setter, itemVM.DisplayName ?? Label, this);

			var popup = new ValueSourceEditorPopup(viewModel, this)
			{
				Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
					?? Application.Current?.MainWindow
			};

			_activeValueSourcePopups[itemVM] = popup;
			itemVM.IsPopupOpen = true;

			popup.Closed += (_, _) =>
			{
				_activeValueSourcePopups.Remove(itemVM);
				itemVM.IsPopupOpen = false;
				itemVM.OnClose();
				OnPropertyChanged(nameof(FieldValue));
			};

			popup.Show();
		}

		private void OpenValueSourceEditorForWrappedItem(ItemViewModel itemVM, object wrapper, FieldInfo vsField)
		{
			if (_activeValueSourcePopups.TryGetValue(itemVM, out var existing) && existing.IsLoaded)
			{
				existing.Focus();
				return;
			}

			Type valueSourceType = vsField.FieldType;

			Func<object> getter = () => vsField.GetValue(wrapper);
			Action<object> setter = newValue =>
			{
				vsField.SetValue(wrapper, newValue);
				OnPropertyChanged(nameof(FieldValue));
			};

			var viewModel = new ValueSourceEditorViewModel(
				valueSourceType, getter, setter, itemVM.DisplayName ?? Label, this);

			var popup = new ValueSourceEditorPopup(viewModel, this)
			{
				Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
					?? Application.Current?.MainWindow
			};

			_activeValueSourcePopups[itemVM] = popup;
			itemVM.IsPopupOpen = true;

			popup.Closed += (_, _) =>
			{
				_activeValueSourcePopups.Remove(itemVM);
				itemVM.IsPopupOpen = false;
				itemVM.OnClose();
				OnPropertyChanged(nameof(FieldValue));
			};

			popup.Show();
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
			if (!FieldType.IsGenericType) return;

			if (_activeValueSourcePopup != null && _activeValueSourcePopup.IsLoaded)
			{
				_activeValueSourcePopup.Focus();
				return;
			}

			ValueSourceEditorPopup popup = new ValueSourceEditorPopup(this)
			{
				Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
					?? Application.Current?.MainWindow
			};
			_activeValueSourcePopup = popup;
			popup.Closed += (_, _) =>
			{
				_activeValueSourcePopup = null;
				RefreshValueSourceDisplay();
			};
			popup.Show();
		}

		internal void RefreshValueSourceDisplay()
		{
			_valueSourceChip?.Refresh();
			OnPropertyChanged(nameof(FieldValue));
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
