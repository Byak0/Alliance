using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Utilities;
using Alliance.Server.Extensions.PlayerSpawn.Behaviors;
using System;
using System.IO;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.PlayerSpawn.Handlers
{
	public class PlayerSpawnServerHandler : PlayerSpawnMenuHandlerBase, IHandlerRegister
	{
		private NetworkCommunicator _currentPeerMakingChanges;

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<UpdatePlayerSpawnMenu>(HandleUpdatePlayerSpawnMenu);
			reg.Register<RequestCharacterUsage>(HandleRequestCharacterUsage);
			reg.Register<RequestOfficerUsage>(HandleRequestOfficerUsage);
			reg.Register<VoteForOfficer>(HandleVoteForOfficer);
		}

		private bool HandleRequestCharacterUsage(NetworkCommunicator peer, RequestCharacterUsage message)
		{
			PlayerSpawnBehavior playerSpawnBehavior = Mission.Current.GetMissionBehavior<PlayerSpawnBehavior>();
			if (playerSpawnBehavior == null || !playerSpawnBehavior.Initialized)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName} tried to request character usage but PlayerSpawnBehavior is not enabled.", LogLevel.Warning);
				return false;
			}

			// Retrieve team/formation/character and check their validity
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);
			if (peer == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Error);
				return false;
			}

			// If player is already assigned to this team/formation/character, just update its perks
			PlayerAssignment assignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);
			if (assignment.Team == team && assignment.Formation == formation && assignment.Character == character)
			{
				PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);
				string perks = "";
				message.SelectedPerks.ForEach(i => perks += i);
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} updated perks for character {team.Name} - {formation.Name} - {character.Name} : {perks}", LogLevel.Debug);
				return true;
			}

			if (character.Officer) return false;

			// Check if player is authorized to select this character
			if (formation.GetAvailableSlots() < 1 || character.AvailableSlots < 1)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName}'s selection refused : Formation {formation.Name} or character {character.Name} has no available slots", LogLevel.Error);
				return false;
			}

			// If the character was an officer candidate, remove the candidacy and broadcast the change
			if (formation.CandidateInfo.TryGetValue(peer, out CandidateInfo candidateInfo))
			{
				formation.RemoveCandidate(candidateInfo);
				PlayerSpawnMenuMsg.SendRemoveOfficerCandidacyToAll(peer, team, formation, character);
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} removed from officer candidacy in {team.Name} - {formation.Name} - {character.Name}", LogLevel.Debug);
			}

			// If the player was the elected officer of this formation, remove the officer status
			if (formation.Officer == peer)
			{
				formation.SetOfficer(null);
				PlayerSpawnMenuMsg.SendSetFormationOfficerToAll(null, team, formation);
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} removed from officer status in {team.Name} - {formation.Name}", LogLevel.Debug);
			}

			// Validation passed, proceed to select the character and broadcast the change			
			PlayerSpawnMenu.Instance.SelectCharacter(peer, team, formation, character);
			PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);
			// Update Spawn status / time before spawn
			if (playerSpawnBehavior.SpawnInProgress)
			{
				// Default time before spawn
				float timeBeforeSpawn = playerSpawnBehavior.SpawnWaitTimeAfterSelection;

				// Ensure player don't spawn after spawn session ended
				if (playerSpawnBehavior.SpawnSessionLifeTime > -1f && timeBeforeSpawn > playerSpawnBehavior.TimeLeftBeforeSpawnEnd)
				{
					timeBeforeSpawn = Math.Max(playerSpawnBehavior.TimeLeftBeforeSpawnEnd, 0f);
				}
				// Keep previous spawn timer if possible
				else if (assignment.TimeBeforeSpawn > 0f)
				{
					timeBeforeSpawn = Math.Min(assignment.TimeBeforeSpawn, playerSpawnBehavior.SpawnWaitTimeAfterSelection);
				}
				PlayerSpawnMenu.Instance.UpdateSpawnStatus(peer, true, timeBeforeSpawn);
			}
			else
			{
				PlayerSpawnMenu.Instance.UpdateSpawnStatus(peer, false, -1f);
			}
			PlayerSpawnMenuMsg.SendAddPlayerCharacterUsageToAll(peer, team, formation, character);
			Log($"Alliance - PlayerSpawnMenu - {peer.UserName} reserved character {team.Name} - {formation.Name} - {character.Name}", LogLevel.Information);
			return true;
		}

		private bool HandleRequestOfficerUsage(NetworkCommunicator peer, RequestOfficerUsage message)
		{
			PlayerSpawnBehavior playerSpawnBehavior = Mission.Current.GetMissionBehavior<PlayerSpawnBehavior>();
			if (playerSpawnBehavior == null || !playerSpawnBehavior.Initialized)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName} tried to request character usage but PlayerSpawnBehavior is not initialized.", LogLevel.Warning);
				return false;
			}

			// Retrieve team/formation/character and check their validity
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);
			if (peer == null || team == null || formation == null || character == null || !character.Officer)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName} requested invalid officer usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Warning);
				return false;
			}

			// If player is already assigned to this team/formation/character, just update its perks
			PlayerAssignment assignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);
			if (assignment.Team == team && assignment.Formation == formation && assignment.Character == character)
			{
				PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);
				string perks = "";
				message.SelectedPerks.ForEach(i => perks += i);
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} updated perks for character {team.Name} - {formation.Name} - {character.Name} : {perks}", LogLevel.Debug);
				return true;
			}

			// Check if player is authorized to select this officer character
			if (formation.GetAvailableSlots() < 1)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName}'s selection refused : Formation {formation.Name} has no available slots", LogLevel.Error);
				return false;
			}

			// If the character was an officer candidate, remove the candidacy and broadcast the change
			if (formation.CandidateInfo.TryGetValue(peer, out CandidateInfo oldCandidateInfo))
			{
				formation.RemoveCandidate(oldCandidateInfo);
				PlayerSpawnMenuMsg.SendRemoveOfficerCandidacyToAll(peer, team, formation, character);
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} removed from officer candidacy in {team.Name} - {formation.Name} - {character.Name}", LogLevel.Debug);
			}

			// Check if pitch is too long
			if (message.Pitch.Length > 200)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName}'s pitch is too long ({message.Pitch.Length} characters), maximum allowed is 200 characters.", LogLevel.Error);
				return false;
			}

			// Validation passed, proceed to select the character and broadcast the change
			PlayerSpawnMenu.Instance.SelectCharacter(peer, team, formation, character);
			PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);
			// Update Spawn status / time before spawn
			if (playerSpawnBehavior.SpawnInProgress)
			{
				// Default time before spawn
				float timeBeforeSpawn = playerSpawnBehavior.SpawnWaitTimeAfterSelection;

				// Ensure player don't spawn after spawn session ended
				if (playerSpawnBehavior.SpawnSessionLifeTime > -1f && timeBeforeSpawn > playerSpawnBehavior.TimeLeftBeforeSpawnEnd)
				{
					timeBeforeSpawn = Math.Max(playerSpawnBehavior.TimeLeftBeforeSpawnEnd, 0f);
				}
				// Keep previous spawn timer if possible
				else if (assignment.TimeBeforeSpawn > 0f)
				{
					timeBeforeSpawn = Math.Min(assignment.TimeBeforeSpawn, playerSpawnBehavior.SpawnWaitTimeAfterSelection);
				}
				PlayerSpawnMenu.Instance.UpdateSpawnStatus(peer, true, timeBeforeSpawn);
			}
			PlayerSpawnMenuMsg.SendAddPlayerCharacterUsageToAll(peer, team, formation, character);

			// Add player as officer candidate and broadcast the candidacy
			formation.AddCandidate(peer, message.Pitch);
			PlayerSpawnMenuMsg.SendAddOfficerCandidacyToAll(peer, team, formation, character, message.Pitch);
			Log($"Alliance - PlayerSpawnMenu - {peer.UserName} is now candidate for officer in {team.Name} - {formation.Name} - {character.Name} : {message.Pitch}", LogLevel.Information);
			return true;
		}

		private bool HandleVoteForOfficer(NetworkCommunicator peer, VoteForOfficer message)
		{
			// Check if the target candidate is valid
			PlayerAssignment playerAssignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);
			PlayerAssignment candidateAssignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(message.Target);
			PlayerFormation formation = candidateAssignment.Formation;
			if (formation == null || !formation.CandidateInfo.TryGetValue(message.Target, out CandidateInfo candidateInfo))
			{
				Log($"Alliance - {peer.UserName} tried to vote for {message.Target?.UserName} but the target is not a candidate in formation {formation?.Name}", LogLevel.Error);
				return false;
			}

			// Check if the peer is authorized to vote (same formation as the candidate)
			if (playerAssignment.Team != candidateAssignment.Team
				|| playerAssignment.Formation != candidateAssignment.Formation)
			{
				Log($"Alliance - {peer.UserName} tried to vote for {message.Target?.UserName} but they're not in the same formation", LogLevel.Error);
				return false;
			}

			// Add or remove the vote based on the message
			if (message.Add)
			{
				formation.AddVote(peer, candidateInfo);
				Log($"Alliance - {peer.UserName} voted for {message.Target?.UserName} (he now has {candidateInfo.Votes} votes)", LogLevel.Debug);
			}
			else
			{
				formation.RemoveVote(peer, candidateInfo);
				Log($"Alliance - {peer.UserName} removed his vote for {message.Target?.UserName} (he now has {candidateInfo.Votes} votes)", LogLevel.Debug);
			}

			return true;
		}

		private bool HandleUpdatePlayerSpawnMenu(NetworkCommunicator peer, UpdatePlayerSpawnMenu message)
		{
			// Check if the peer is authorized to update the player spawn menu
			if (!peer.IsAdmin())
			{
				Log($"Alliance - Unauthorized attempt to update player spawn menu by {peer.UserName}", LogLevel.Warning);
				return false;
			}

			if (message.Operation == PlayerSpawnMenuOperation.BeginMenuSync)
			{
				_currentPeerMakingChanges = peer;
			}
			else if (peer != _currentPeerMakingChanges)
			{
				// If the peer is not the one who started the sync, ignore the message
				Log($"Alliance - Ignoring PlayerSpawnMenu update from {peer.UserName} as they are not the current editor.", LogLevel.Warning);
				return false;
			}

			// Use the base handler to process the message
			HandlePlayerSpawnMenuOperation(message);
			return true;
		}

		// Override the base method to add custom behavior for when the menu sync ends
		protected override void EndMenuSyncHandler()
		{
			base.EndMenuSyncHandler();

			// Save the updated player spawn menu to file
			string fileName = $"spawn_preset_{DateTime.Now:yyyyMMdd_HHmmss}_{_currentPeerMakingChanges.UserName}.xml";
			string filePath = Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Spawn_Presets", fileName);
			try
			{
				Log($"Alliance - Saving PlayerSpawnMenu to {filePath}");
				SerializeHelper.SaveClassToFile(filePath, _receivedPlayerSpawnMenu);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to save PlayerSpawnMenu to {filePath}: {ex.Message}", LogLevel.Error);
				return;
			}
			finally
			{
				_currentPeerMakingChanges = null;
			}

			// Broadcast the updated player spawn menu to all players
			PlayerSpawnMenuMsg.SendPlayerSpawnMenuToAll();
		}
	}
}
