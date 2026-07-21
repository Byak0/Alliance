using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using TaleWorlds.Engine;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	public enum ValueSourceOrigin
	{
		Literal,
		Variable,
		Function
	}

	/// <summary>One selectable origin in the expression editor's top-level dropdown.</summary>
	public sealed class ValueSourceOriginOption
	{
		public ValueSourceOrigin Origin { get; }
		public string Label { get; }
		public bool IsAvailable { get; }

		public ValueSourceOriginOption(ValueSourceOrigin origin, string label, bool isAvailable = true)
		{
			Origin = origin;
			Label = label;
			IsAvailable = isAvailable;
		}
	}

	/// <summary>
	/// Owns the Literal / Variable / Function selector for one <c>ValueSource&lt;T&gt;</c> field. The body
	/// deliberately reuses <see cref="ObjectEditorViewModel"/>: a Function's own PhraseTemplate therefore
	/// renders each of its <c>ValueSource&lt;X&gt;</c> arguments as another chip, giving recursive editing.
	/// </summary>
	public sealed class ValueSourceEditorViewModel : INotifyPropertyChanged
	{
		private readonly FieldViewModel _owner;
		private readonly Type _valueType;
		private ValueSourceOriginOption _selectedOrigin;
		private ObjectEditorViewModel _detailEditor;

		public string Title => $"Edit {_owner.Label}";
		public string TargetTypeLabel => _valueType.Name;
		public IReadOnlyList<ValueSourceOriginOption> Origins { get; }
		public ICommand CloseCommand { get; }

		public ValueSourceOriginOption SelectedOrigin
		{
			get => _selectedOrigin;
			set
			{
				if (value == null || !value.IsAvailable || ReferenceEquals(_selectedOrigin, value)) return;
				_selectedOrigin = value;
				if (GetCurrentOrigin() != value.Origin)
				{
					_owner.FieldValue = CreateSource(value.Origin);
				}
				RebuildDetailEditor();
				OnPropertyChanged(nameof(SelectedOrigin));
			}
		}

		public ObjectEditorViewModel DetailEditor
		{
			get => _detailEditor;
			private set
			{
				if (ReferenceEquals(_detailEditor, value)) return;
				_detailEditor?.Close();
				_detailEditor = value;
				OnPropertyChanged(nameof(DetailEditor));
				OnPropertyChanged(nameof(HasDetailEditor));
			}
		}

		public bool HasDetailEditor => DetailEditor != null;

		public ValueSourceEditorViewModel(FieldViewModel owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			if (!ValueSourceTypeSupport.TryGetValueType(owner.FieldType, out _valueType))
			{
				throw new ArgumentException("The owning field must be ValueSource<T>.", nameof(owner));
			}

			bool hasVariables = _owner.CollectAvailableVariables(_valueType).Length > 0;
			bool hasFunctions = FieldViewModel.DiscoverConcreteTypes(typeof(Function))
				.Any(t => FieldViewModel.FunctionReturns(t, _valueType));

			Origins = new List<ValueSourceOriginOption>
			{
				new ValueSourceOriginOption(ValueSourceOrigin.Literal, "Literal", ValueSourceTypeSupport.SupportsLiteral(_valueType)),
				new ValueSourceOriginOption(ValueSourceOrigin.Variable, "Variable", hasVariables),
				new ValueSourceOriginOption(ValueSourceOrigin.Function, "Function", hasFunctions)
			};
			CloseCommand = new RelayCommand(CloseWindow);

			ValueSourceOrigin? currentOrigin = GetCurrentOrigin();
			if (currentOrigin.HasValue)
			{
				_selectedOrigin = Origins.FirstOrDefault(option => option.Origin == currentOrigin.Value);
				RebuildDetailEditor();
			}
		}

		public void Close()
		{
			DetailEditor?.Close();
			_detailEditor = null;
		}

		private void CloseWindow(object parameter)
		{
			if (parameter is Window window) window.Close();
		}

		private ValueSourceOrigin? GetCurrentOrigin()
		{
			object source = _owner.FieldValue;
			if (source == null) return null;

			Type type = source.GetType();
			if (!type.IsGenericType) return null;
			Type definition = type.GetGenericTypeDefinition();
			if (definition == typeof(LiteralValue<>)) return ValueSourceOrigin.Literal;
			if (definition == typeof(VariableValue<>)) return ValueSourceOrigin.Variable;
			if (definition == typeof(FunctionCall<>)) return ValueSourceOrigin.Function;
			return null;
		}

		private object CreateSource(ValueSourceOrigin origin)
		{
			Type sourceType;
			switch (origin)
			{
				case ValueSourceOrigin.Literal:
					sourceType = typeof(LiteralValue<>).MakeGenericType(_valueType);
					object literal = Activator.CreateInstance(sourceType);
					sourceType.GetField(nameof(LiteralValue<int>.Value), BindingFlags.Instance | BindingFlags.Public)
						.SetValue(literal, CreateLiteralDefault());
					return literal;
				case ValueSourceOrigin.Variable:
					return Activator.CreateInstance(typeof(VariableValue<>).MakeGenericType(_valueType));
				case ValueSourceOrigin.Function:
					return Activator.CreateInstance(typeof(FunctionCall<>).MakeGenericType(_valueType));
				default:
					throw new ArgumentOutOfRangeException(nameof(origin));
			}
		}

		private object CreateLiteralDefault()
		{
			if (_valueType == typeof(Zone)) return new Zone();
			if (_valueType == typeof(LocalizedString)) return new LocalizedString("");
			if (_valueType == typeof(string)) return string.Empty;
			return _valueType.IsValueType ? Activator.CreateInstance(_valueType) : null;
		}

		private void RebuildDetailEditor()
		{
			object source = _owner.FieldValue;
			if (source == null)
			{
				DetailEditor = null;
				return;
			}

			WeakGameEntity entity = _owner.parentViewModel?.GameEntity ?? WeakGameEntity.Invalid;
			DetailEditor = new ObjectEditorViewModel(source, _owner, _owner.scenarioEditorViewModel, "", entity);
			_owner.RefreshValueSourceDisplay();
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
