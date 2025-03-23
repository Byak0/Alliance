using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Editor.GameModes.Story.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Input;
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

		public FieldInfo FieldInfo { get; private set; }
		public string FieldName { get; private set; }
		public Type FieldType { get; private set; }
		public string Label { get; private set; }
		public string Tooltip { get; private set; }
		public bool ShowTooltip { get; private set; }
		public string[] PossibleValues { get; private set; }
		public bool IsMultiChoiceString => PossibleValues != null && PossibleValues.Length > 0;
		public bool IsLocalizedString => typeof(LocalizedString).IsAssignableFrom(FieldType);
		public bool IsComplexType => !FieldType.IsEnum && !IsLocalizedString && !IsCollection && !FieldType.IsPrimitive && FieldType != typeof(string) && FieldType != typeof(bool);
		public bool IsCollection => typeof(IEnumerable).IsAssignableFrom(FieldType) && FieldType != typeof(string);
		public bool IsZone => FieldType == typeof(SerializableZone);
		public ObservableCollection<ItemViewModel> Items { get; }
		public ZoneViewModel ZoneVM { get; }

		public object FieldValue
		{
			get => _fieldValue;
			set
			{
				if (_fieldValue != value)
				{
					_fieldValue = value;
					// Propagate the value to the parent object
					FieldInfo.SetValue(parentViewModel.Object, _fieldValue);
					OnPropertyChanged(nameof(FieldValue));
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

		public ICommand EditCommand { get; }
		public ICommand DeleteCommand { get; }
		public ICommand AddCommand { get; }

		public FieldViewModel(FieldInfo fieldInfo, object fieldValue, ObjectEditorViewModel parentViewModel, ScenarioEditorViewModel scenarioEditorViewModel)
		{
			FieldInfo = fieldInfo;
			this.parentViewModel = parentViewModel;
			this.scenarioEditorViewModel = scenarioEditorViewModel;
			FieldName = fieldInfo.Name;
			FieldValue = fieldValue;
			FieldType = fieldInfo.FieldType;

			var attribute = fieldInfo.GetCustomAttribute<ScenarioEditorAttribute>();

			if (attribute != null)
			{
				Label = attribute.Label ?? FieldName;
				Tooltip = attribute.Tooltip;
				ShowTooltip = !string.IsNullOrEmpty(Tooltip);
				PossibleValues = attribute.PossibleValues;
			}
			else
			{
				Label = FieldName;
			}

			EditCommand = new RelayCommand(_ => EditObjectFromFieldInfo(FieldInfo), _ => IsComplexType);
			DeleteCommand = new RelayCommand(DeleteItem);
			AddCommand = new RelayCommand(_ => AddItem());

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
			var editorWindow = new ObjectEditorWindow(obj, scenarioEditorViewModel, parentViewModel.Title, parentViewModel.GameEntity);
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

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Close()
		{
			ZoneVM?.Close();
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
