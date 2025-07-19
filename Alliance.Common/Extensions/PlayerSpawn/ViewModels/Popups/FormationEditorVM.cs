using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels.Popups
{
	/// <summary>
	/// View model for editing a formation in the Player Spawn menu.
	/// </summary>
	public class FormationEditorVM : ViewModel
	{
		public event EventHandler OnCloseMenu;

		private PlayerFormation _formationCopy;
		private PlayerFormation _formation;
		public PlayerFormation Formation => _formation;

		private MBBindingList<OptionVM> _options = new MBBindingList<OptionVM>();

		[DataSourceProperty]
		public MBBindingList<OptionVM> Options
		{
			get
			{
				return _options;
			}
			set
			{
				if (value != _options)
				{
					_options = value;
					OnPropertyChangedWithValue(value, "Options");
				}
			}
		}

		[DataSourceProperty]
		public string Name
		{
			get => _formation.Name;
			set
			{
				if (_formation.Name != value)
				{
					_formation.Name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		[DataSourceProperty]
		public bool UseMorale
		{
			get => _formation.Settings.UseMorale;
			set
			{
				if (_formation.Settings.UseMorale != value)
				{
					_formation.Settings.UseMorale = value;
					OnPropertyChangedWithValue(value, nameof(UseMorale));
				}
			}
		}

		public FormationEditorVM(PlayerFormation formation)
		{
			_formation = formation;
			_formationCopy = new PlayerFormation
			{
				Name = formation.Name,
				Settings = new FormationSettings
				{
					UseMorale = formation.Settings.UseMorale
				}
			};
			InitializeOptions();
		}

		[UsedImplicitly]
		public void Cancel()
		{
			// Reset the formation to its original state
			_formation.Name = _formationCopy.Name;
			_formation.Settings.UseMorale = _formationCopy.Settings.UseMorale;
			OnCloseMenu(this, EventArgs.Empty);
		}

		[UsedImplicitly]
		public void Save()
		{
			OnCloseMenu(this, EventArgs.Empty);
		}

		private void InitializeOptions()
		{
			Options = new MBBindingList<OptionVM>();

			_options.Add(new SelectionOptionVM(
								new TextObject(nameof(Formation.MainCultureId)),
								new TextObject(nameof(Formation.MainCultureId)),
								new SelectionOptionData(
									() => AllianceData.AvailableCultures().FindIndexQ(Formation.MainCultureId),
									newValue => Formation.MainCultureId = AllianceData.AvailableCultures()[newValue],
									AllianceData.AvailableCultures().Length,
									AllianceData.AvailableCultures()),
								false));

			// Iterate over Formation.Settings properties to create options
			foreach (PropertyInfo property in _formation.Settings.GetType().GetProperties())
			{
				if (property.CanRead && property.CanWrite)
				{
					var value = property.GetValue(_formation.Settings);
					var attribute = property.GetCustomAttribute<ConfigPropertyAttribute>();
					// todo clean this is just for testing
					attribute ??= new ConfigPropertyAttribute(true, property.Name, "coucou");
					switch (property.PropertyType)
					{
						case Type boolType when boolType == typeof(bool):
							Options.Add(new BoolOptionVM(
								new TextObject(attribute.Label ?? property.Name),
								new TextObject(attribute.Tooltip ?? property.Name),
								() => (bool)property.GetValue(_formation.Settings),
								newValue => property.SetValue(_formation.Settings, newValue)));
							break;
						case Type intType when intType == typeof(int):
							Options.Add(new NumericOptionVM(
								new TextObject(attribute.Label ?? property.Name),
								new TextObject(attribute.Tooltip ?? property.Name),
								() => (float)property.GetValue(_formation.Settings),
								newValue => property.SetValue(_formation.Settings, newValue),
								attribute.MinValue,
								attribute.MaxValue,
								true, true));
							break;
							//case Type stringType when stringType == typeof(string):
							//	Options.Add(new SelectionOptionVM(
							//		new TextObject(attribute.Label ?? property.Name),
							//		new TextObject(attribute.Tooltip ?? property.Name),
							//		new SelectionOptionData(
							//			() => 0, // Placeholder for selection index
							//			newValue => { }, // Placeholder for setting value
							//			2, // Limit of selectable options
							//			attribute.PossibleValues.ToList()), // Placeholder for faction choices
							//		false));
							//	break;
					}
				}
			}
		}
	}
}