#if !SERVER
using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.Views.Popups;
using Alliance.Common.GameModes.Story.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
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
		private PlayerCharacterVM _officerCharacterWaitingForValidationVM;
		private PlayerCharacterVM _selectedCharacterVM;
		private PlayerCharacterVM _previouslySelectedCharacterVM;
		private CharacterEditorPopup _characterEditorPopup;
		private FormationEditorPopup _formationEditorPopup;
		private TeamEditorPopup _teamEditorPopup;
		private SaveFileDialog _saveFile;
		private OpenFileDialog _openFile;
		private List<string> _availableLanguages;
		private bool _electionInProgress;
		private float _timeBeforeOfficerElection;
		private string _formationInfoText;
		private bool _showTroops;
		private string _playerSpawnInfo;
		private float _timeBeforeSpawn;
		private bool _showSpawnInfo;

		public PlayerSpawnMenu PlayerSpawnMenu => _playerSpawnMenu;
		public PlayerTeamVM SelectedTeamVM => _selectedTeamVM;
		public PlayerFormationVM SelectedFormationVM => _selectedFormationVM;

		public Action OnCloseMenu;

		[DataSourceProperty]
		public bool ShowSpawnInfo
		{
			get => _showSpawnInfo;
			set
			{
				if (value != _showSpawnInfo)
				{
					_showSpawnInfo = value;
					OnPropertyChangedWithValue(value, nameof(ShowSpawnInfo));
				}
			}
		}

		[DataSourceProperty]
		public string PlayerSpawnInfo
		{
			get => _playerSpawnInfo;
			set
			{
				if (value != _playerSpawnInfo)
				{
					_playerSpawnInfo = value;
					OnPropertyChangedWithValue(value, nameof(PlayerSpawnInfo));
				}
			}
		}

		[DataSourceProperty]
		public float TimeBeforeSpawn
		{
			get => _timeBeforeSpawn;
			set
			{
				if (value != _timeBeforeSpawn)
				{
					_timeBeforeSpawn = value;
					OnPropertyChangedWithValue(value, nameof(TimeBeforeSpawn));
					if (ShowSpawnInfo) RefreshSpawnInfoText();
				}
			}
		}

		[DataSourceProperty]
		public string FormationInfoText
		{
			get => _formationInfoText;
			set
			{
				if (value != _formationInfoText)
				{
					_formationInfoText = value;
					OnPropertyChangedWithValue(value, nameof(FormationInfoText));
				}
			}
		}

		[DataSourceProperty]
		public bool ElectionInProgress
		{
			get => _electionInProgress;
			set
			{
				if (value != _electionInProgress)
				{
					_electionInProgress = value;
					OnPropertyChangedWithValue(value, nameof(ElectionInProgress));
				}
			}
		}

		[DataSourceProperty]
		public float TimeBeforeOfficerElection
		{
			get => _timeBeforeOfficerElection;
			set
			{
				if (value != _timeBeforeOfficerElection)
				{
					_timeBeforeOfficerElection = value;
					OnPropertyChangedWithValue(value, nameof(TimeBeforeOfficerElection));
				}
			}
		}

		[DataSourceProperty]
		public bool ShowTroops
		{
			get => _showTroops;
			set
			{
				if (value != _showTroops)
				{
					_showTroops = value;
					OnPropertyChangedWithValue(value, nameof(ShowTroops));
				}
			}
		}

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
			_showTroops = false;
			_electionInProgress = PlayerSpawnMenu.Instance.ElectionInProgress;

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

			PlayerSpawnMenu.OnCharacterSelected += OnCharacterSelected;
			PlayerSpawnMenu.OnCharacterDeselected += OnCharacterDeselected;
			PlayerSpawnMenu.OnElectionStatusChanged += OnElectionStatusChanged;
			PlayerSpawnMenu.OnSpawnStatusChanged += OnSpawnStatusChanged;

			RefreshFormationInfoText();
			RefreshSpawnInfoText();
		}

		public void CloseMenu()
		{
			OnCloseMenu?.Invoke();
		}

		private void RefreshFormationInfoText()
		{
			if (ElectionInProgress)
			{
				FormationInfoText = "An officer election is in progress. You can vote for your preferred candidate.";
			}
			else
			{

				NetworkCommunicator officer = SelectedFormationVM?.Formation?.Officer;
				if (officer != null)
				{
					if (officer.IsMine) FormationInfoText = $"You are the officer! Lead your formation to victory!";
					else if (officer == _playerSpawnMenu.MyAssignment?.Formation?.Officer) FormationInfoText = $"Your officer is {officer.UserName}. Press alt to see their name.";
					else FormationInfoText = $"This formation officer is {officer.UserName}.";
				}
				else
				{
					FormationInfoText = "No officer assigned";
				}
			}
		}

		private void RefreshSpawnInfoText()
		{
			if (SelectedCharacterVM == null)
			{
				PlayerSpawnInfo = $"Choose a character";
			}
			else if (_officerCharacterWaitingForValidationVM != null && _playerSpawnMenu.MyAssignment.CanSpawn)
			{
				PlayerSpawnInfo = $"If elected officer, you will spawn as {SelectedCharacterVM?.Name}";
			}
			else if (_playerSpawnMenu.MyAssignment != null && _playerSpawnMenu.MyAssignment.CanSpawn)
			{
				if (Agent.Main != null)
				{
					PlayerSpawnInfo = $"You will respawn as {SelectedCharacterVM?.Name}";
				}
				else
				{
					PlayerSpawnInfo = $"You will spawn as {SelectedCharacterVM?.Name} in {TimeBeforeSpawn:F1} seconds";
				}
			}
			else
			{
				PlayerSpawnInfo = "You cannot spawn at the moment";
			}
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
				ShowTroops = false;

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
					formation.OnOfficerCandidaciesUpdated -= OnOfficerCandidaciesUpdates;
					formation.OnOfficerCandidaciesUpdated += OnOfficerCandidaciesUpdates;
					formation.OnOfficerUpdated -= OnOfficerUpdated;
					formation.OnOfficerUpdated += OnOfficerUpdated;
					formation.OnLanguageChanged -= OnLanguageUpdated;
					formation.OnLanguageChanged += OnLanguageUpdated;
					//if (formation == _playerSpawnMenu.MyAssignment.Formation)
					//{
					//	SelectFormation(formationVM);
					//}
				}
			}
		}

		private void OnLanguageUpdated(PlayerFormation formation, string language)
		{
			PlayerFormationVM playerFormationVM = Formations.FirstOrDefault(f => f.Formation == formation);
			if (playerFormationVM != null)
			{
				playerFormationVM.MainLanguages = language;
			}
		}

		private void OnOfficerUpdated(PlayerFormation formation)
		{
			RefreshFormationInfoText();
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
				RefreshFormationInfoText();
				ShowTroops = true;
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
				PlayerSpawnMenuMsg.RequestCharacterUsage(SelectedTeamVM.Team, SelectedFormationVM.Formation, characterVM.AvailableCharacter, selectedPerks);
			}
		}

		/// <summary>
		/// Called when clicking on a character. Sends a request to the server to confirm selection.
		/// </summary>
		public void TrySelectCharacter(PlayerCharacterVM characterVM)
		{
			if (characterVM?.AvailableCharacter == null || characterVM == SelectedCharacterVM || characterVM == _officerCharacterWaitingForValidationVM) return;

			if (!GameNetwork.IsMyPeerReady) return;

			if (PlayerSpawnMenu.MyAssignment.Formation?.Officer != null && PlayerSpawnMenu.MyAssignment.Formation.Officer.IsMine)
			{
				InformationManager.ShowInquiry(
					new InquiryData(new TextObject("Give up officer role", null).ToString(),
					new TextObject("Are you sure you want to give up your officer role ?", null).ToString(), true, true,
					GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(),
					delegate
					{
						HandleCharacterSelection(characterVM);
					}, null, "", 0f, null, null, null), false, false);
			}
			else
			{
				HandleCharacterSelection(characterVM);
			}
		}

		private void HandleCharacterSelection(PlayerCharacterVM characterVM)
		{
			// If character is an officer, prompt for pitch then send request to server
			if (characterVM.Officer)
			{
				if (!ElectionInProgress)
				{
					Log($"You can't apply to be officer, there is no election in progress", LogLevel.Warning);
					return;
				}

				_officerCharacterWaitingForValidationVM = characterVM;
				// Prompt a text inquiry for user to enter his pitch
				InformationManager.ShowTextInquiry(
					new TextInquiryData("Candidate to be officer",
					"Choose a convincing pitch (200 characters max):", true, true,
					new TextObject("{=WiNRdfsm}Done", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(),
					new Action<string>(OnOfficerCandidacySent), null, false, null, "", "I'll lead you to victory!"),
					false);
			}
			// Classic character - Ask server if we are allowed to select this character
			else
			{
				List<int> selectedPerks = characterVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
				PlayerSpawnMenuMsg.RequestCharacterUsage(SelectedTeamVM.Team, SelectedFormationVM.Formation, characterVM.AvailableCharacter, selectedPerks);
			}
		}

		private void OnOfficerCandidacySent(string pitch)
		{
			// Check that pitch is not too long
			if (pitch.Length > 200)
			{
				Log("Pitch is too long! Maximum 200 characters.", LogLevel.Error);
				_officerCharacterWaitingForValidationVM = null;
				return;
			}

			// Send request to server to become officer
			List<int> selectedPerks = _officerCharacterWaitingForValidationVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
			PlayerSpawnMenuMsg.RequestOfficerUsage(SelectedTeamVM.Team, SelectedFormationVM.Formation, _officerCharacterWaitingForValidationVM.AvailableCharacter, selectedPerks, pitch);
		}

		private void OnElectionStatusChanged(bool electionInProgress)
		{
			ElectionInProgress = electionInProgress;
			RefreshFormationInfoText();
		}

		private void OnSpawnStatusChanged(bool spawnEnabled)
		{
			ShowSpawnInfo = spawnEnabled;
			RefreshSpawnInfoText();
		}

		/// <summary>
		/// Called when server has validated player's choice. Updates the VM accordingly.
		/// </summary>
		private void OnCharacterDeselected(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, AvailableCharacter character)
		{
			if (player.IsMine) ClearCharacterSelection();
			RefreshTeamFormationCharacter(team, formation, character);
		}

		/// <summary>
		/// Called when server has validated player's choice. Updates the VM accordingly.
		/// </summary>
		private void OnCharacterSelected(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, AvailableCharacter character)
		{
			if (player.IsMine)
			{
				PlayerTeamVM teamVM = Teams.FirstOrDefault(t => t.Team.Index == team.Index);
				if (SelectedTeamVM != teamVM) SelectTeam(teamVM);

				PlayerFormationVM formationVM = Formations.FirstOrDefault(f => f.Formation.Index == formation.Index);
				if (SelectedFormationVM != formationVM) SelectFormation(formationVM);

				PlayerCharacterVM characterVM = Characters.FirstOrDefault(c => c.AvailableCharacter.Index == character.Index);
				SelectCharacter(characterVM);
				RefreshSpawnInfoText();
			}
			else
			{
				RefreshTeamFormationCharacter(team, formation, character);
			}
		}

		// Refresh team/formation/character values if they are visible
		private void RefreshTeamFormationCharacter(PlayerTeam team, PlayerFormation formation, AvailableCharacter character)
		{
			Teams.FirstOrDefault(t => t.Team.Index == team.Index)?.RefreshValues();
			if (SelectedTeamVM?.Team?.Index == team.Index)
			{
				Formations.FirstOrDefault(f => f.Formation.Index == formation.Index)?.RefreshValues();
				if (SelectedFormationVM?.Formation.Index == formation.Index)
				{
					Characters.FirstOrDefault(c => c.AvailableCharacter.Index == character.Index)?.RefreshValues();
				}
			}
		}

		private void SelectCharacter(PlayerCharacterVM characterVM)
		{
			ClearCharacterSelection();

			// Set the new selected character
			SelectedCharacterVM = characterVM;

			if (SelectedCharacterVM == null) return;

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
			ShowSpawnInfo = true;
		}

		private void ClearCharacterSelection()
		{
			if (SelectedCharacterVM != null)
			{
				_previouslySelectedCharacterVM = SelectedCharacterVM;
				_previouslySelectedCharacterVM.FallBack();
				_previouslySelectedCharacterVM.IsSelected = false;
				_officerCharacterWaitingForValidationVM = null;
				SelectedCharacterVM = null;
			}
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
					if (PlayerSpawnMenu.Instance.MyAssignment != null && availableCharacter == PlayerSpawnMenu.Instance.MyAssignment.Character)
					{
						characterToSelect = characterVM;
					}

					marginLeft += width;
				}
			}

			if (characterToSelect != null) SelectCharacter(characterToSelect);
		}

		private void OnOfficerCandidaciesUpdates(PlayerFormation formation)
		{
			if (formation != SelectedFormationVM?.Formation) return;

			RefreshOfficerCandidacies();
		}

		private void RefreshOfficerCandidacies()
		{
			OfficerCandidacies.Clear();
			if (SelectedFormationVM != null)
			{
				foreach (CandidateInfo candidateInfo in SelectedFormationVM.Formation.Candidates)
				{
					OfficerCandidacyVM candidacyVM = new OfficerCandidacyVM(SelectedFormationVM.Formation, SelectOfficerCandidacy, candidateInfo);
					if (SelectedFormationVM.Formation.MyVotes.Contains(candidateInfo)) candidacyVM.IsSelected = true;
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
					SelectedFormationVM.Formation.MyVotes.Remove(vm.Candidate);
					//vm.Formation.RemoveVote(GameNetwork.MyPeer, vm.Candidate);

					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new VoteForOfficer(vm.Candidate.Candidate, false));
					GameNetwork.EndModuleEventAsClient();
				}
				else
				{
					// If not selected, select and add vote
					vm.IsSelected = true;
					SelectedFormationVM.Formation.MyVotes.Add(vm.Candidate);
					//vm.Formation.AddVote(GameNetwork.MyPeer, vm.Candidate);
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new VoteForOfficer(vm.Candidate.Candidate, true));
					GameNetwork.EndModuleEventAsClient();
				}
			}
		}

		[UsedImplicitly]
		public void Load()
		{
			if (_openFile.ShowDialog() == DialogResult.OK)
			{
				if (PlayerSpawnMenu.TryLoadFromFile(_openFile.FileName, out PlayerSpawnMenu newMenu))
				{
					_playerSpawnMenu = newMenu;
					GenerateMenu();
					Log($"Preset loaded from {_openFile.FileName}", LogLevel.Information);
				}
			}
		}

		[UsedImplicitly]
		public void Save()
		{
			if (_saveFile.ShowDialog() == DialogResult.OK)
			{
				_playerSpawnMenu.SaveToFile(_saveFile.FileName);
			}
		}

		[UsedImplicitly]
		public void Sync()
		{
			PlayerSpawnMenuMsg.RequestUpdatePlayerSpawnMenu(_playerSpawnMenu);
		}

		public override void OnFinalize()
		{
			base.OnFinalize();

			PlayerSpawnMenu.OnCharacterSelected -= OnCharacterSelected;
			PlayerSpawnMenu.OnCharacterDeselected -= OnCharacterDeselected;
			PlayerSpawnMenu.OnElectionStatusChanged -= OnElectionStatusChanged;
			PlayerSpawnMenu.OnSpawnStatusChanged -= OnSpawnStatusChanged;

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