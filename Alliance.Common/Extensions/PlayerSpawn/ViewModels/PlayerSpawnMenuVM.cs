#if !SERVER
using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Views.Popups;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// Main view model for the Player Spawn menu.
	/// </summary>
	public class PlayerSpawnMenuVM : ViewModel
	{
		private bool _isVisible;
		private bool _editMode;
		private string _menuTitle;
		private PlayerSpawnMenu _playerSpawnMenu;
		private OptionVM _preferredLanguage;
		private MBBindingList<PlayerTeamVM> _teamsVM;
		private MBBindingList<PlayerFormationVM> _formationsVM;
		private MBBindingList<PlayerCharacterVM> _charactersVM;
		private MBBindingList<OfficerCandidacyVM> _officerCandidaciesVM;
		private PlayerTeamVM _selectedTeamVM;
		private PlayerFormationVM _selectedFormationVM;
		private PlayerCharacterVM _selectedCharacterVM;
		private PlayerCharacterVM _previouslySelectedCharacterVM;
		private CharacterEditorPopup _characterEditorPopup;
		private FormationEditorPopup _formationEditorPopup;
		private TeamEditorPopup _teamEditorPopup;
		private SaveFileDialog _saveFile;
		private OpenFileDialog _openFile;
		private List<string> _availableLanguages;

		public PlayerSpawnMenu PlayerSpawnMenu => _playerSpawnMenu;
		public PlayerTeamVM SelectedTeamVM => _selectedTeamVM;
		public PlayerFormationVM SelectedFormationVM => _selectedFormationVM;

		[DataSourceProperty]
		public PlayerCharacterVM SelectedCharacterVM
		{
			get
			{
				return _selectedCharacterVM;
			}
			set
			{
				if (value != _selectedCharacterVM)
				{
					_selectedCharacterVM = value;
					OnPropertyChangedWithValue(value, nameof(SelectedCharacterVM));
				}
			}
		}

		[DataSourceProperty]
		public OptionVM PreferredLanguage
		{
			get
			{
				return _preferredLanguage;
			}
			set
			{
				if (value != _preferredLanguage)
				{
					_preferredLanguage = value;
					OnPropertyChangedWithValue(value, nameof(PreferredLanguage));
				}
			}
		}

		[DataSourceProperty]
		public bool IsAdmin
		{
			get
			{
				return GameNetwork.MyPeer == null || GameNetwork.MyPeer.IsAdmin();
			}
		}

		[DataSourceProperty]
		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				if (value != _isVisible)
				{
					_isVisible = value;
					OnPropertyChangedWithValue(value, nameof(IsVisible));
				}
			}
		}

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

					// Propagate to all formations
					foreach (PlayerFormationVM formation in _formationsVM)
					{
						formation.EditMode = _editMode;
					}

					// Propagate to all characters
					foreach (PlayerCharacterVM character in _charactersVM)
					{
						character.EditMode = _editMode;
					}
				}
			}
		}

		[DataSourceProperty]
		public string MenuTitle
		{
			get => _menuTitle;
			set
			{
				if (value != _menuTitle)
				{
					_menuTitle = value;
					OnPropertyChangedWithValue(value, nameof(MenuTitle));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerTeamVM> Teams
		{
			get => _teamsVM;
			set
			{
				if (value != _teamsVM)
				{
					_teamsVM = value;
					OnPropertyChangedWithValue(value, nameof(Teams));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerFormationVM> Formations
		{
			get => _formationsVM;
			set
			{
				if (value != _formationsVM)
				{
					_formationsVM = value;
					OnPropertyChangedWithValue(value, nameof(Formations));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerCharacterVM> Characters
		{
			get => _charactersVM;
			set
			{
				if (value != _charactersVM)
				{
					_charactersVM = value;
					OnPropertyChangedWithValue(value, nameof(Characters));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<OfficerCandidacyVM> OfficerCandidacies
		{
			get => _officerCandidaciesVM;
			set
			{
				if (value != _officerCandidaciesVM)
				{
					_officerCandidaciesVM = value;
					OnPropertyChangedWithValue(value, nameof(OfficerCandidacies));
				}
			}
		}

		public PlayerSpawnMenuVM(PlayerSpawnMenu playerSpawnMenu)
		{
			_saveFile = new SaveFileDialog();
			_saveFile.Title = "Save Preset";
			_saveFile.FileName = "spawn_preset";
			_saveFile.DefaultExt = ".xml";
			_saveFile.CreatePrompt = false;
			_saveFile.OverwritePrompt = true;
			_saveFile.Filter = "XML File (.xml)|*.xml";
			_saveFile.InitialDirectory = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName), "Spawn_Presets"));
			_saveFile.AddExtension = true;
			_openFile = new OpenFileDialog();
			_openFile.Title = "Load Preset";
			_openFile.FileName = "";
			_openFile.DefaultExt = ".xml";
			_openFile.Filter = "XML File (.xml)|*.xml";
			_openFile.InitialDirectory = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName), "Spawn_Presets"));

			_teamsVM = new MBBindingList<PlayerTeamVM>();
			_formationsVM = new MBBindingList<PlayerFormationVM>();
			_charactersVM = new MBBindingList<PlayerCharacterVM>();
			_officerCandidaciesVM = new MBBindingList<OfficerCandidacyVM>();
			_characterEditorPopup = new CharacterEditorPopup();
			_formationEditorPopup = new FormationEditorPopup();
			_teamEditorPopup = new TeamEditorPopup();

			_availableLanguages = LocalizationHelper.GetAvailableLanguages();

			PreferredLanguage = new SelectionOptionVM(
									new TextObject("VOIP preference :"),
									new TextObject("Your preferred language for VOIP"),
									new SelectionOptionData(
										() => UserConfig.Instance.PreferredLanguageIndex,
										newValue => SetLanguage(newValue),
										_availableLanguages.Count,
										_availableLanguages),
								false);

			_playerSpawnMenu = playerSpawnMenu;
			GenerateMenu();

			_playerSpawnMenu.OnCharacterSelected += OnCharacterSelected;
			_playerSpawnMenu.OnCharacterDeselected += OnCharacterDeselected;
		}

		private void SetLanguage(int newValue)
		{
			if (UserConfig.Instance.PreferredLanguageIndex != newValue)
			{
				UserConfig.Instance.PreferredLanguageIndex = newValue;
				UserConfig.Instance.Save();
				ConfigManager.Instance.SendMyConfigToServer(UserConfig.Instance);
			}
		}

		/// <summary>
		/// Fully regenerate menu from PlayerSpawnMenu model data.
		/// </summary>
		public void GenerateMenu()
		{
			Teams.Clear();
			Formations.Clear();
			Characters.Clear();
			OfficerCandidacies.Clear();
			foreach (PlayerTeam team in _playerSpawnMenu.Teams)
			{
				Teams.Add(new PlayerTeamVM(team, SelectTeam, EditTeam, DeleteTeam, EditMode));
			}

			if (_playerSpawnMenu.MyAssignment?.Team == null) return;

			PlayerTeamVM teamVM = Teams.FirstOrDefault(t => t.Team.Index == _playerSpawnMenu.MyAssignment.Team.Index);
			SelectTeam(teamVM);

			if (_playerSpawnMenu.MyAssignment.Formation == null) return;

			PlayerFormationVM formationVM = Formations.FirstOrDefault(f => f.Formation.Index == _playerSpawnMenu.MyAssignment.Formation.Index);
			SelectFormation(formationVM);

			if (_playerSpawnMenu.MyAssignment.Character == null) return;

			PlayerCharacterVM characterVM = Characters.FirstOrDefault(c => c.AvailableCharacter.Index == _playerSpawnMenu.MyAssignment.Character.Index);
			SelectCharacter(characterVM);
		}

		/// <summary>
		/// "Light" refresh of all values in the menu.
		/// </summary>
		public override void RefreshValues()
		{
			foreach (PlayerTeamVM teamVM in Teams)
			{
				teamVM.RefreshValues();
			}
			foreach (PlayerFormationVM formationVM in Formations)
			{
				formationVM.RefreshValues();
			}
			foreach (PlayerCharacterVM characterVM in Characters)
			{
				characterVM.RefreshValues();
			}
		}

		[UsedImplicitly]
		public void AddTeam()
		{
			// todo check if setting default side to defender is pertinent
			PlayerTeam newTeam = _playerSpawnMenu.AddTeam(TaleWorlds.Core.BattleSideEnum.Defender, "New Team");
			if (newTeam != null)
			{
				PlayerTeamVM newTeamVM = new PlayerTeamVM(newTeam, SelectTeam, EditTeam, DeleteTeam, EditMode);
				Teams.Add(newTeamVM);
			}
		}

		public void EditTeam(PlayerTeamVM teamVM)
		{
			if (teamVM != null)
			{
				_teamEditorPopup.OpenMenu(teamVM.Team, OnTeamEdited);
			}
		}

		private void OnTeamEdited(PlayerTeam team)
		{
			_teamsVM.FirstOrDefault(vm => vm.Team == team)?.RefreshValues();
		}

		public void DeleteTeam(PlayerTeamVM teamVM)
		{
			if (Teams.Remove(teamVM))
			{
				_playerSpawnMenu.RemoveTeam(teamVM.Team);
			}
			if (teamVM != null && SelectedTeamVM == teamVM)
			{
				_selectedTeamVM = null;
				_selectedFormationVM = null;
				GenerateMenu();
			}
		}

		[UsedImplicitly]
		public void AddFormation()
		{
			// Ensure a team is selected before adding a formation
			PlayerTeam selectedTeam = SelectedTeamVM?.Team;
			if (selectedTeam == null) return;

			PlayerFormation newFormation = selectedTeam.AddFormation("New formation");
			PlayerFormationVM newFormationVM = new PlayerFormationVM(SelectedTeamVM, newFormation, SelectFormation, EditFormation, DeleteFormation, EditMode);
			Formations.Add(newFormationVM);
		}

		public void EditFormation(PlayerFormationVM formationVM)
		{
			if (formationVM != null)
			{
				_formationEditorPopup.OpenMenu(formationVM.Formation, OnFormationEdited);
			}
		}

		private void OnFormationEdited(PlayerFormation formation)
		{
			_formationsVM.FirstOrDefault(vm => vm.Formation == formation)?.RefreshValues();
		}

		public void DeleteFormation(PlayerFormationVM formationVM)
		{
			if (formationVM != null && SelectedFormationVM == formationVM)
			{
				_selectedFormationVM = null;
			}
			if (Formations.Remove(formationVM))
			{
				formationVM.TeamVM.Team.RemoveFormation(formationVM.Formation);
			}
		}

		[UsedImplicitly]
		public void AddCharacter()
		{
			if (SelectedFormationVM == null) return;

			_characterEditorPopup.OpenMenu(new AvailableCharacter(), SelectedFormationVM.Formation.MainCulture, OnCharacterAdded);
		}

		private void OnCharacterAdded(AvailableCharacter character)
		{
			if (SelectedFormationVM == null || character?.CharacterId == null) return;

			SelectedFormationVM.Formation.AddCharacter(character);
			RefreshCharacters();
		}

		public void EditCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM != null)
			{
				_characterEditorPopup.OpenMenu(characterVM.AvailableCharacter, SelectedFormationVM.Formation.MainCulture, OnCharacterEdited);
			}
		}

		private void OnCharacterEdited(AvailableCharacter character)
		{
			RefreshCharacters();
		}

		public void DeleteCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM != null && SelectedFormationVM != null && SelectedFormationVM.Formation.AvailableCharacters.Contains(characterVM.AvailableCharacter))
			{
				SelectedFormationVM.Formation.AvailableCharacters.Remove(characterVM.AvailableCharacter);
				RefreshCharacters();
			}
		}

		public void SelectTeam(PlayerTeamVM playerTeamVM)
		{
			if (playerTeamVM != null)
			{
				// Deselect previous Formation/Team
				if (SelectedFormationVM != null) SelectedFormationVM.IsSelected = false;
				if (SelectedTeamVM != null) SelectedTeamVM.IsSelected = false;
				if (SelectedCharacterVM != null) SelectedCharacterVM.IsSelected = false;

				SelectedCharacterVM = null;
				_selectedFormationVM = null;
				_selectedTeamVM = playerTeamVM;
				playerTeamVM.IsSelected = true;
				MenuTitle = $"{SelectedTeamVM.Team.TeamSide} - {SelectedTeamVM.Team.Name}";

				// Clear existing characters and officer candidacies
				Characters.Clear();
				OfficerCandidacies.Clear();

				// Clear existing formations and add new ones for the selected team
				Formations.Clear();
				foreach (PlayerFormation formation in playerTeamVM.Team.Formations)
				{
					PlayerFormationVM formationVM = new PlayerFormationVM(playerTeamVM, formation, SelectFormation, EditFormation, DeleteFormation, EditMode);
					Formations.Add(formationVM);
					//if (formation == _playerSpawnMenu.MyAssignment.Formation)
					//{
					//	SelectFormation(formationVM);
					//}
				}
			}
		}

		public void SelectFormation(PlayerFormationVM formationVM)
		{
			if (formationVM != null)
			{
				if (SelectedFormationVM != null) SelectedFormationVM.IsSelected = false;
				_selectedFormationVM = formationVM;
				formationVM.IsSelected = true;
				MenuTitle = $"{SelectedTeamVM.Team.TeamSide} - {SelectedTeamVM.Team.Name} - {SelectedFormationVM.Name}";
				// Refresh characters and officer candidacies for the selected formation
				RefreshCharacters();
				RefreshOfficerCandidacies();
			}
		}

		/// <summary>
		/// Called when changing perks for a character.
		/// </summary>
		public void UpdateCharacterPerks(PlayerCharacterVM characterVM)
		{
			// If the character is already selected, we can update perks directly
			if (characterVM == SelectedCharacterVM)
			{
				List<int> selectedPerks = characterVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
				_playerSpawnMenu.RequestToUseCharacter(SelectedTeamVM.Team, SelectedFormationVM.Formation, characterVM.AvailableCharacter, selectedPerks);
			}
		}

		/// <summary>
		/// Called when clicking on a character. Sends a request to the server to confirm selection.
		/// </summary>
		public void TrySelectCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM?.AvailableCharacter == null || characterVM == SelectedCharacterVM) return;

			if (!GameNetwork.IsMyPeerReady) return;

			// If character is an officer, prompt for pitch then send request to server
			if (characterVM.Officer)
			{
				// todo: display popup then send variant request
				List<int> selectedPerks = characterVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
				_playerSpawnMenu.RequestToUseCharacter(SelectedTeamVM.Team, SelectedFormationVM.Formation, characterVM.AvailableCharacter, selectedPerks);
			}
			// Classic character - Ask server if we are allowed to select this character
			else
			{
				List<int> selectedPerks = characterVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
				_playerSpawnMenu.RequestToUseCharacter(SelectedTeamVM.Team, SelectedFormationVM.Formation, characterVM.AvailableCharacter, selectedPerks);
			}
		}

		/// <summary>
		/// Called when server has validated player's choice. Updates the VM accordingly.
		/// </summary>
		private void OnCharacterDeselected(NetworkCommunicator player, int teamIndex, int formationIndex, int characterIndex)
		{
			if (player.IsMine) SelectCharacter(null);
			RefreshTeamFormationCharacter(teamIndex, formationIndex, characterIndex);
		}

		/// <summary>
		/// Called when server has validated player's choice. Updates the VM accordingly.
		/// </summary>
		private void OnCharacterSelected(NetworkCommunicator player, int teamIndex, int formationIndex, int characterIndex)
		{
			if (player.IsMine)
			{
				PlayerTeamVM teamVM = Teams.FirstOrDefault(t => t.Team.Index == teamIndex);
				if (SelectedTeamVM != teamVM) SelectTeam(teamVM);

				PlayerFormationVM formationVM = Formations.FirstOrDefault(f => f.Formation.Index == formationIndex);
				if (SelectedFormationVM != formationVM) SelectFormation(formationVM);

				PlayerCharacterVM characterVM = Characters.FirstOrDefault(c => c.AvailableCharacter.Index == characterIndex);
				SelectCharacter(characterVM);
			}
			else
			{
				RefreshTeamFormationCharacter(teamIndex, formationIndex, characterIndex);
			}
		}

		private void RefreshTeamFormationCharacter(int teamIndex, int formationIndex, int characterIndex)
		{
			Teams.FirstOrDefault(t => t.Team.Index == teamIndex)?.RefreshValues();
			if (SelectedTeamVM.Team.Index == teamIndex)
			{
				Formations.FirstOrDefault(f => f.Formation.Index == formationIndex)?.RefreshValues();
				if (SelectedFormationVM.Formation.Index == formationIndex)
				{
					Characters.FirstOrDefault(c => c.AvailableCharacter.Index == characterIndex)?.RefreshValues();
				}
			}
		}

		private void SelectCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM == null)
			{
				if (SelectedCharacterVM != null)
				{
					_previouslySelectedCharacterVM = SelectedCharacterVM;
					_previouslySelectedCharacterVM.FallBack();
					_previouslySelectedCharacterVM.IsSelected = false;
					SelectedCharacterVM = null;
				}

				return;
			}

			// Set the new selected character
			SelectedCharacterVM = characterVM;
			SelectedCharacterVM.IsSelected = true;
			SelectedCharacterVM.Advance();

			// Change sibling index to display selected button on top of others
			SelectedCharacterVM.SiblingOrder = 0;

			// Make the rest of the characters cheer
			foreach (PlayerCharacterVM playerCharacterVM in _charactersVM)
			{
				if (playerCharacterVM != SelectedCharacterVM && playerCharacterVM != _previouslySelectedCharacterVM)
				{
					playerCharacterVM.Cheer();
				}
			}

			SelectedCharacterVM.RefreshValues();
			SelectedFormationVM.RefreshValues();
		}

		private void RefreshCharacters()
		{
			Characters.Clear();
			PlayerCharacterVM characterToSelect = null;
			if (SelectedFormationVM != null)
			{
				int width = 1440 / (SelectedFormationVM.Formation.AvailableCharacters.Count + 1);
				int marginLeft = 0;
				foreach (AvailableCharacter availableCharacter in SelectedFormationVM.Formation.AvailableCharacters)
				{
					PlayerCharacterVM characterVM = new PlayerCharacterVM(SelectedTeamVM, SelectedFormationVM, availableCharacter, TrySelectCharacter, UpdateCharacterPerks, EditCharacter, DeleteCharacter, EditMode);
					characterVM.Width = width;
					characterVM.MarginLeft = marginLeft;
					characterVM.Idle();
					Characters.Add(characterVM);
					if (availableCharacter == SelectedCharacterVM?.AvailableCharacter)
					{
						characterToSelect = characterVM;
					}

					marginLeft += width;
				}
			}

			if (characterToSelect != null) SelectCharacter(characterToSelect);
		}

		private void RefreshOfficerCandidacies()
		{
			OfficerCandidacies.Clear();
			if (SelectedFormationVM != null)
			{
				foreach (CandidateInfo candidateInfo in SelectedFormationVM.Formation.Candidates)
				{
					OfficerCandidacyVM candidacyVM = new OfficerCandidacyVM(SelectedFormationVM.Formation, SelectOfficerCandidacy, candidateInfo);
					OfficerCandidacies.Add(candidacyVM);
				}
			}
		}

		private void SelectOfficerCandidacy(OfficerCandidacyVM vm)
		{
			if (vm != null)
			{
				if (vm.IsSelected)
				{
					// If already selected, deselect and remove vote
					vm.IsSelected = false;
					vm.Formation.RemoveVote(GameNetwork.MyPeer, vm.Candidate);
				}
				else
				{
					// If not selected, select and add vote
					vm.IsSelected = true;
					vm.Formation.AddVote(GameNetwork.MyPeer, vm.Candidate);
				}
			}
		}

		[UsedImplicitly]
		public void Load()
		{
			if (_openFile.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// Load the selected file and update the PlayerSpawnMenu model
					string filePath = _openFile.FileName;
					if (File.Exists(filePath))
					{
						_playerSpawnMenu = SerializeHelper.LoadClassFromFile(filePath, _playerSpawnMenu);
						_playerSpawnMenu.RefreshIndices();
						PlayerSpawnMenu.Instance = _playerSpawnMenu;
						GenerateMenu();
					}
				}
				catch (Exception ex)
				{
					Log($"Failed to load preset from {_openFile.FileName}: {ex.Message}", LogLevel.Error);
				}
			}
		}

		[UsedImplicitly]
		public void Save()
		{
			if (_saveFile.ShowDialog() == DialogResult.OK)
			{
				SerializeHelper.SaveClassToFile(_saveFile.FileName, _playerSpawnMenu);
			}
		}

		[UsedImplicitly]
		public void Sync()
		{
			PlayerSpawnMenuNetworkHelper.RequestUpdatePlayerSpawnMenu(_playerSpawnMenu);
		}

		public override void OnFinalize()
		{
			base.OnFinalize();

			_playerSpawnMenu.OnCharacterSelected -= OnCharacterSelected;
			_playerSpawnMenu.OnCharacterDeselected -= OnCharacterDeselected;

			// Ensure all popups are closed when finalizing the menu
			if (_characterEditorPopup.IsMenuOpen)
			{
				_characterEditorPopup.CloseMenu();
			}
			if (_formationEditorPopup.IsMenuOpen)
			{
				_formationEditorPopup.CloseMenu();
			}
			if (_teamEditorPopup.IsMenuOpen)
			{
				_teamEditorPopup.CloseMenu();
			}
		}
	}
}
#endif