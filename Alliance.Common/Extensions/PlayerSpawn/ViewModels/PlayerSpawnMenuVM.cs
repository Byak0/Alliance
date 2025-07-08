#if !SERVER
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Views.Popups;
using Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview;
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
		private MBBindingList<PlayerTeamVM> _teams;
		private MBBindingList<PlayerFormationVM> _formations;
		private MBBindingList<PlayerCharacterVM> _characters;
		private MBBindingList<OfficerCandidacyVM> _officerCandidacies;
		private PlayerTeamVM _selectedTeam;
		private PlayerFormationVM _selectedFormation;
		private PlayerCharacterVM _selectedCharacter;
		private CharacterEditorPopup _characterEditorPopup;
		private FormationEditorPopup _formationEditorPopup;
		private TeamEditorPopup _teamEditorPopup;
		private SaveFileDialog _saveFile;
		private OpenFileDialog _openFile;
		private List<string> _availableLanguages;

		public PlayerSpawnMenu PlayerSpawnMenu => _playerSpawnMenu;
		public PlayerTeamVM SelectedTeam => _selectedTeam;
		public PlayerFormationVM SelectedFormation => _selectedFormation;
		public PlayerCharacterVM SelectedCharacter => _selectedCharacter;

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
					foreach (PlayerFormationVM formation in _formations)
					{
						formation.EditMode = _editMode;
					}

					// Propagate to all characters
					foreach (PlayerCharacterVM character in _characters)
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
			get => _teams;
			set
			{
				if (value != _teams)
				{
					_teams = value;
					OnPropertyChangedWithValue(value, nameof(Teams));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerFormationVM> Formations
		{
			get => _formations;
			set
			{
				if (value != _formations)
				{
					_formations = value;
					OnPropertyChangedWithValue(value, nameof(Formations));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<PlayerCharacterVM> Characters
		{
			get => _characters;
			set
			{
				if (value != _characters)
				{
					_characters = value;
					OnPropertyChangedWithValue(value, nameof(Characters));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<OfficerCandidacyVM> OfficerCandidacies
		{
			get => _officerCandidacies;
			set
			{
				if (value != _officerCandidacies)
				{
					_officerCandidacies = value;
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

			_teams = new MBBindingList<PlayerTeamVM>();
			_formations = new MBBindingList<PlayerFormationVM>();
			_characters = new MBBindingList<PlayerCharacterVM>();
			_officerCandidacies = new MBBindingList<OfficerCandidacyVM>();
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
			RefreshMenu();
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
		/// Refresh menu from PlayerSpawnMenu model data.
		/// </summary>
		public void RefreshMenu()
		{
			Teams.Clear();
			Formations.Clear();
			Characters.Clear();
			OfficerCandidacies.Clear();
			foreach (PlayerTeam team in _playerSpawnMenu.Teams)
			{
				PlayerTeamVM teamVM = new PlayerTeamVM(team, SelectTeam, EditTeam, DeleteTeam, EditMode);
				Teams.Add(teamVM);
				if (team == _playerSpawnMenu.SelectedTeam)
				{
					SelectTeam(teamVM);
				}
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
			_teams.FirstOrDefault(vm => vm.Team == team)?.RefreshValues();
		}

		public void DeleteTeam(PlayerTeamVM teamVM)
		{
			if (teamVM != null && SelectedTeam == teamVM)
			{
				_selectedTeam = null;
				_selectedFormation = null;
				_playerSpawnMenu.SelectTeam(null);
				RefreshMenu();
			}
			if (Teams.Remove(teamVM))
			{
				_playerSpawnMenu.RemoveTeam(teamVM.Team);
			}
		}

		[UsedImplicitly]
		public void AddFormation()
		{
			// Ensure a team is selected before adding a formation
			PlayerTeam selectedTeam = SelectedTeam?.Team;
			if (selectedTeam == null) return;

			PlayerFormation newFormation = _playerSpawnMenu.AddFormation(selectedTeam, "New formation");
			PlayerFormationVM newFormationVM = new PlayerFormationVM(selectedTeam, newFormation, SelectFormation, EditFormation, DeleteFormation, EditMode);
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
			_formations.FirstOrDefault(vm => vm.Formation == formation)?.RefreshValues();
		}

		public void DeleteFormation(PlayerFormationVM formationVM)
		{
			if (formationVM != null && SelectedFormation == formationVM)
			{
				_selectedFormation = null;
				_playerSpawnMenu.SelectFormation(null);
			}
			if (Formations.Remove(formationVM))
			{
				_playerSpawnMenu.RemoveFormation(formationVM.Team, formationVM.Formation);
			}
		}

		[UsedImplicitly]
		public void AddCharacter()
		{
			if (SelectedFormation == null) return;

			_characterEditorPopup.OpenMenu(new AvailableCharacter(), SelectedFormation.Formation.MainCulture, OnCharacterAdded);
		}

		private void OnCharacterAdded(AvailableCharacter character)
		{
			if (SelectedFormation == null || character?.CharacterId == null) return;

			_playerSpawnMenu.AddCharacter(SelectedFormation.Formation, character);
			RefreshCharacters();
		}

		public void EditCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM != null)
			{
				_characterEditorPopup.OpenMenu(characterVM.AvailableCharacter, SelectedFormation.Formation.MainCulture, OnCharacterEdited);
			}
		}

		private void OnCharacterEdited(AvailableCharacter character)
		{
			RefreshCharacters();
		}

		public void DeleteCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM != null && SelectedFormation != null && SelectedFormation.Formation.AvailableCharacters.Contains(characterVM.AvailableCharacter))
			{
				SelectedFormation.Formation.AvailableCharacters.Remove(characterVM.AvailableCharacter);
				RefreshCharacters();
			}
		}

		public void SelectTeam(PlayerTeamVM playerTeamVM)
		{
			if (playerTeamVM != null)
			{
				// Deselect previous Formation/Team
				if (SelectedFormation != null) SelectedFormation.IsSelected = false;
				if (SelectedTeam != null) SelectedTeam.IsSelected = false;
				if (SelectedCharacter != null) SelectedCharacter.IsSelected = false;

				_selectedCharacter = null;
				_selectedFormation = null;
				_selectedTeam = playerTeamVM;
				_playerSpawnMenu.SelectTeam(playerTeamVM.Team);
				playerTeamVM.IsSelected = true;
				MenuTitle = $"{SelectedTeam.Team.TeamSide} - {SelectedTeam.Team.Name}";

				// Clear existing characters and officer candidacies
				Characters.Clear();
				OfficerCandidacies.Clear();

				// Clear existing formations and add new ones for the selected team
				Formations.Clear();
				foreach (PlayerFormation formation in playerTeamVM.Team.Formations)
				{
					PlayerFormationVM formationVM = new PlayerFormationVM(playerTeamVM.Team, formation, SelectFormation, EditFormation, DeleteFormation, EditMode);
					Formations.Add(formationVM);
					if (formation == _playerSpawnMenu.SelectedFormation)
					{
						SelectFormation(formationVM);
					}
				}
			}
		}

		public void SelectFormation(PlayerFormationVM formationVM)
		{
			if (formationVM != null)
			{
				if (SelectedFormation != null) SelectedFormation.IsSelected = false;
				_selectedFormation = formationVM;
				_playerSpawnMenu.SelectFormation(formationVM.Formation);
				formationVM.IsSelected = true;
				MenuTitle = $"{SelectedTeam.Team.TeamSide} - {SelectedTeam.Team.Name} - {SelectedFormation.Name}";
				// Refresh characters and officer candidacies for the selected formation
				RefreshCharacters();
				RefreshOfficerCandidacies();
			}
		}

		public void SelectCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM?.CharacterViewModel != null)
			{
				_playerSpawnMenu.MovePlayerToFormation(GameNetwork.MyPeer, SelectedFormation.Team, SelectedFormation.Formation);

				if (_playerSpawnMenu.SelectCharacter(GameNetwork.MyPeer, characterVM.AvailableCharacter))
				{
					//if (SelectedCharacter != null) SelectedCharacter.IsSelected = false;
					//characterVM.IsSelected = true;
					//_selectedCharacter = characterVM;

					// Test candidacy
					if (characterVM.AvailableCharacter.Officer)
					{
						_playerSpawnMenu.DeclareCandidacy(GameNetwork.MyPeer, "For Frodo !");
						RefreshOfficerCandidacies();
					}
				}
				else
				{
					characterVM.IsSelected = false;
				}

				//todo test remove and put under condition
				// todo here ask model if we are authorize to select character (potentially wait for server answer)
				AL_CharacterViewModel _previouslySelectedCharacter = _selectedCharacter?.CharacterViewModel;

				if (SelectedCharacter != null) SelectedCharacter.IsSelected = false;
				characterVM.IsSelected = true;
				_selectedCharacter = characterVM;

				if (_previouslySelectedCharacter != null)
				{
					try
					{
						_previouslySelectedCharacter.ExecuteStartCustomAnimation("act_walk_backward_1h");
						_previouslySelectedCharacter.CameraZoom = 1.2f;
						_previouslySelectedCharacter.CameraAnimDuration = 1.2f;
						_previouslySelectedCharacter.ApplyCameraChange = true;
						_previouslySelectedCharacter.EnableLight = false;
					}
					catch (Exception ex)
					{
						Log("Exception in PlayerSpawnMenuVM : " + ex, LogLevel.Error);
					}
				}

				foreach (PlayerCharacterVM playerCharacterVM in _characters)
				{
					AL_CharacterViewModel charVM = playerCharacterVM.CharacterViewModel;
					if (charVM != _selectedCharacter.CharacterViewModel && charVM != _previouslySelectedCharacter)
					{
						charVM.ExecuteStartCustomAnimation("act_cheer_1");
					}
				}

				_selectedCharacter.CharacterViewModel.ExecuteStartCustomAnimation("act_walk_forward_1h");
				_selectedCharacter.CharacterViewModel.CameraZoom = -1.2f;
				_selectedCharacter.CharacterViewModel.CameraElevation = 0f;
				_selectedCharacter.CharacterViewModel.CameraAnimDuration = 1.2f;
				_selectedCharacter.CharacterViewModel.CameraPitch = 0f;
				_selectedCharacter.CharacterViewModel.ApplyCameraChange = true;
				_selectedCharacter.CharacterViewModel.EnableLight = true;

				// Change sibling index to display selected button on top of others
				_selectedCharacter.SiblingOrder = 0;

				SelectedCharacter.RefreshValues();
				SelectedFormation.RefreshValues();
			}
		}

		private void RefreshCharacters()
		{
			Characters.Clear();
			if (SelectedFormation != null)
			{
				int width = 1440 / (SelectedFormation.Formation.AvailableCharacters.Count + 1);
				int marginLeft = 0;
				foreach (AvailableCharacter availableCharacter in SelectedFormation.Formation.AvailableCharacters)
				{
					PlayerCharacterVM characterVM = new PlayerCharacterVM(SelectedFormation.Team, SelectedFormation.Formation, availableCharacter, SelectCharacter, EditCharacter, DeleteCharacter, EditMode);
					characterVM.Width = width;
					characterVM.MarginLeft = marginLeft;
					if (characterVM.CharacterViewModel != null)
					{
						characterVM.CharacterViewModel.IdleAction = "act_walk_idle_1h_with_h_shld_left_stance";
						characterVM.CharacterViewModel.CameraElevation = 0.2f;
						characterVM.CharacterViewModel.CameraAnimDuration = 0f;
						characterVM.CharacterViewModel.ApplyCameraChange = true;
					}

					Characters.Add(characterVM);
					if (availableCharacter == _playerSpawnMenu.SelectedCharacter)
					{
						characterVM.IsSelected = true;
					}

					marginLeft += width;
				}
			}
		}

		private void RefreshOfficerCandidacies()
		{
			OfficerCandidacies.Clear();
			if (SelectedFormation != null)
			{
				foreach (CandidateInfo candidateInfo in SelectedFormation.Formation.Candidates)
				{
					OfficerCandidacyVM candidacyVM = new OfficerCandidacyVM(SelectedFormation.Formation, SelectOfficerCandidacy, candidateInfo);
					OfficerCandidacies.Add(candidacyVM);
				}
			}
		}

		private void SelectOfficerCandidacy(OfficerCandidacyVM vM)
		{
			if (vM != null)
			{
				vM.IsSelected = _playerSpawnMenu.ToggleVote(vM.Formation, GameNetwork.MyPeer, vM.Candidate);
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
						RefreshMenu();
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