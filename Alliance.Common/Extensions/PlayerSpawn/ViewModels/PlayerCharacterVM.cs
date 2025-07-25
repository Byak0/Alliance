#if !SERVER
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview;
using Alliance.Common.Patch.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Core.Utils.AgentExtensions;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for a character in the Player Spawn menu.
	/// </summary>
	public class PlayerCharacterVM : ViewModel
	{
		private bool _editMode;
		private readonly PlayerTeamVM _teamVM;
		private readonly PlayerFormationVM _formationVM;
		private readonly AvailableCharacter _availableCharacter;
		private readonly Action<PlayerCharacterVM> _onCharacterSelected;
		private readonly Action<PlayerCharacterVM> _onCharacterPerksUpdated;
		private readonly Action<PlayerCharacterVM> _onCharacterEdited;
		private readonly Action<PlayerCharacterVM> _onCharacterDeleted;
		private AL_CharacterViewModel _characterViewModel;
		private bool _isSelected;
		private int _siblingOrder = -1;
		private int _width;
		private int _marginLeft;
		private MPHeroClass _heroClass;
		private int _heroClassIndex;
		private bool _advanced = false;

		private MBBindingList<HeroPerkVM> _perks;

		public List<IReadOnlyPerkObject> SelectedPerks { get; private set; }
		public readonly ClassType TroopType;

		public PlayerTeamVM TeamVM => _teamVM;
		public PlayerFormationVM FormationVM => _formationVM;
		public AvailableCharacter AvailableCharacter => _availableCharacter;

		[DataSourceProperty]
		public HeroPerkVM FirstPerk => Perks.ElementAtOrDefault(0);

		[DataSourceProperty]
		public HeroPerkVM SecondPerk => Perks.ElementAtOrDefault(1);

		[DataSourceProperty]
		public HeroPerkVM ThirdPerk => Perks.ElementAtOrDefault(2);

		[DataSourceProperty]
		public bool EditMode
		{
			get => _editMode;
			set
			{
				if (value != _editMode)
				{
					_editMode = value;
					OnPropertyChangedWithValue(value, nameof(EditMode));
				}
			}
		}

		[DataSourceProperty]
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					OnPropertyChangedWithValue(value, nameof(IsSelected));
				}
			}
		}

		[DataSourceProperty]
		public int SiblingOrder
		{
			get => _siblingOrder;
			set
			{
				_siblingOrder = value;
				OnPropertyChangedWithValue(value, nameof(SiblingOrder));
			}
		}

		[DataSourceProperty]
		public int Width
		{
			get => _width;
			set
			{
				if (value != _width)
				{
					_width = value;
					OnPropertyChangedWithValue(value, nameof(Width));
				}
			}
		}

		[DataSourceProperty]
		public int MarginLeft
		{
			get => _marginLeft;
			set
			{
				if (value != _marginLeft)
				{
					_marginLeft = value;
					OnPropertyChangedWithValue(value, nameof(MarginLeft));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<HeroPerkVM> Perks
		{
			get
			{
				return _perks ??= new MBBindingList<HeroPerkVM>();
			}
			set
			{
				if (value != _perks)
				{
					_perks = value;
					OnPropertyChangedWithValue(value, nameof(Perks));
				}
			}
		}

		[DataSourceProperty]
		public AL_CharacterViewModel CharacterViewModel
		{
			get => _characterViewModel;
			set
			{
				if (value != _characterViewModel)
				{
					_characterViewModel = value;
					OnPropertyChangedWithValue(value, nameof(CharacterViewModel));
				}
			}
		}

		[DataSourceProperty]
		public string CharacterId
		{
			get => AvailableCharacter.CharacterId;
		}

		[DataSourceProperty]
		public string Name
		{
			get => AvailableCharacter.Name;
		}

		[DataSourceProperty]
		public bool Officer
		{
			get => AvailableCharacter.Officer;
		}

		[DataSourceProperty]
		public string Occupation
		{
			get => AvailableCharacter.UsedSlots + "/" + AvailableCharacter.MaxSlots;
		}

		[DataSourceProperty]
		public bool IsFull
		{
			get => AvailableCharacter.AvailableSlots <= 0;
		}

		public PlayerCharacterVM(
								PlayerTeamVM teamVM,
								PlayerFormationVM playerFormationVM,
								AvailableCharacter availableCharacter,
								Action<PlayerCharacterVM> onCharacterSelected,
								Action<PlayerCharacterVM> onCharacterPerksUpdated,
								Action<PlayerCharacterVM> onCharacterEdited = null,
								Action<PlayerCharacterVM> onCharacterDeleted = null,
								bool editMode = false)
		{
			_teamVM = teamVM;
			_formationVM = playerFormationVM;
			_availableCharacter = availableCharacter;
			if (availableCharacter.Character != null)
			{
				CharacterViewModel = new AL_CharacterViewModel();
				CharacterViewModel.FillFrom(availableCharacter);
				CharacterViewModel.ArmorColor1 = _formationVM.Formation.MainCulture.Color;
				CharacterViewModel.ArmorColor2 = _formationVM.Formation.MainCulture.Color2;
				_heroClass = AvailableCharacter.Character.GetHeroClass();
				_heroClassIndex = MultiplayerClassDivisions.GetMPHeroClasses(_heroClass.Culture).ToList().IndexOf(_heroClass);
			}
			_onCharacterSelected = onCharacterSelected;
			_onCharacterPerksUpdated = onCharacterPerksUpdated;
			_onCharacterEdited = onCharacterEdited;
			_onCharacterDeleted = onCharacterDeleted;
			_editMode = editMode;

			InitPerks();

			UpdateCharacterPreview();
		}

		private void InitPerks()
		{
			SelectedPerks = new List<IReadOnlyPerkObject>();
			Perks = new MBBindingList<HeroPerkVM>();

			// Check that the character has a hero class
			if (_heroClass == null)
			{
				Log($"Character {AvailableCharacter.Name} does not have a hero class", LogLevel.Warning);
				return;
			}

			// Get list of perks available for the character
			List<List<IReadOnlyPerkObject>> perksToShow = AvailableCharacter.Character.GetMPPerks();
			if (perksToShow == null || perksToShow.Count == 0)
			{
				Log($"No perks available for {AvailableCharacter.Name}", LogLevel.Warning);
				return;
			}

			// Initialize SelectedPerks with the first perk from each list
			foreach (List<IReadOnlyPerkObject> perkList in perksToShow)
			{
				SelectedPerks.Add(perkList[0]);
			}

			// Update SelectedPerks with user's selection if possible
			if (GameNetwork.IsMyPeerReady)
			{
				MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
				for (int j = 0; j < perksToShow.Count; j++)
				{
					int num2 = component.GetSelectedPerkIndexWithPerkListIndex(_heroClassIndex, j);
					if (num2 >= perksToShow[j].Count)
					{
						num2 = 0;
					}

					IReadOnlyPerkObject value = perksToShow[j][num2];
					SelectedPerks[j] = value;
				}
				// Log selected perks for debug
				string logPerks = AvailableCharacter.Name + " selected perks : ";
				List<string> perksNames = new List<string>();
				foreach (IReadOnlyPerkObject perkObj in SelectedPerks)
				{
					perksNames.Add(perkObj.Name.ToString());
				}
				logPerks += string.Join(", ", perksNames);
				Log(logPerks, LogLevel.Debug);
			}

			// Create HeroPerkVM instances for each perk and add them to the MBBindingList
			MBBindingList<HeroPerkVM> newPerks = new MBBindingList<HeroPerkVM>();
			for (int k = 0; k < perksToShow.Count; k++)
			{
				IReadOnlyPerkObject selected = SelectedPerks[k];
				List<IReadOnlyPerkObject> candidates = perksToShow[k];

				if (selected == null || candidates == null || candidates.Count == 0)
				{
					Log($"Warning: Null or empty perk list at index {k} for character {AvailableCharacter.Name}", LogLevel.Warning);
					continue; // Skip adding this one
				}

				HeroPerkVM vm = new HeroPerkVM(SelectPerk, selected, candidates, k);
				if (vm != null)
					newPerks.Add(vm);
			}
			Perks = newPerks;
		}

		private void SelectPerk(HeroPerkVM perkContainer, MPPerkVM perkChoice)
		{
			if (GameNetwork.IsMyPeerReady && Perks != null && Perks.Contains(perkContainer))
			{
				UpdateCharacterPreview();
				_onCharacterPerksUpdated?.Invoke(this);
				GameNetwork.MyPeer.GetComponent<MissionPeer>()?.SelectPerk(perkContainer.PerkIndex, perkChoice.PerkIndex, _heroClassIndex);
			}
		}

		private void UpdateCharacterPreview()
		{
			if (CharacterViewModel == null) return;

			List<IReadOnlyPerkObject> perks = Perks.Select(p => p.SelectedPerk).ToList();

			Equipment equipment = AvailableCharacter.Character.Equipment.Clone();
			MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(perks);
			IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipements = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
			if (alternativeEquipements != null)
			{
				foreach ((EquipmentIndex, EquipmentElement) item in alternativeEquipements)
				{
					equipment[item.Item1] = item.Item2;
				}
			}

			CharacterViewModel.EquipmentCode = equipment.CalculateEquipmentCode();
			if (FormationVM.Formation.MainCulture != null) CharacterViewModel.BannerCodeText = FormationVM.Formation.MainCulture.BannerKey;
		}


		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(Officer));
			OnPropertyChanged(nameof(Occupation));
			OnPropertyChanged(nameof(IsFull));
			//OnPropertyChanged(nameof(CharacterViewModel));
			OnPropertyChanged(nameof(IsSelected));
			Perks.ApplyActionOnAllItems(delegate (HeroPerkVM x)
			{
				x.RefreshValues();
			});
		}

		public void Advance()
		{

			Log($"{AvailableCharacter.Name} is advancing", LogLevel.Debug);
			if (_characterViewModel == null || _advanced) return;
			_characterViewModel.ExecuteStartCustomAnimation("act_walk_forward_1h");
			_characterViewModel.CameraZoom = -1.2f;
			_characterViewModel.CameraElevation = 0f;
			_characterViewModel.CameraPitch = 0f;
			_characterViewModel.CameraAnimDuration = 1.2f;
			_characterViewModel.ApplyCameraChange = true;
			_characterViewModel.EnableLight = true;
			_advanced = true;
		}

		public void FallBack()
		{
			Log($"{AvailableCharacter.Name} is fallback", LogLevel.Debug);
			if (_characterViewModel == null || !_advanced) return;
			_characterViewModel.ExecuteStartCustomAnimation("act_walk_backward_1h");
			_characterViewModel.CameraZoom = 1.2f;
			_characterViewModel.CameraElevation = 0f;
			_characterViewModel.CameraPitch = 0f;
			_characterViewModel.CameraAnimDuration = 1.2f;
			_characterViewModel.ApplyCameraChange = true;
			_characterViewModel.EnableLight = false;
			_advanced = false;
		}

		public void Cheer()
		{
			Log($"{AvailableCharacter.Name} is cheering", LogLevel.Debug);
			if (_characterViewModel == null) return;
			_characterViewModel.ExecuteStartCustomAnimation("act_cheer_1");
		}

		public void Idle()
		{
			Log($"{AvailableCharacter.Name} is idle", LogLevel.Debug);
			if (CharacterViewModel == null) return;
			CharacterViewModel.IdleAction = "act_walk_idle_1h_with_h_shld_left_stance";
			CharacterViewModel.CameraElevation = 0f;
			CharacterViewModel.CameraAnimDuration = 0f;
			CharacterViewModel.ApplyCameraChange = true;
		}

		[UsedImplicitly]
		public void SelectCharacter()
		{
			if (EditMode) return;
			_onCharacterSelected?.Invoke(this);
		}

		[UsedImplicitly]
		public void EditCharacter()
		{
			if (!EditMode) return;
			_onCharacterEdited?.Invoke(this);
		}

		[UsedImplicitly]
		public void DeleteCharacter()
		{
			if (!EditMode) return;
			_onCharacterDeleted?.Invoke(this);
		}
	}
}
#endif