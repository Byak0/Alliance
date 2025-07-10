using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.Extensions.PlayerSpawn.Utilities;
using Alliance.Common.Utilities;
using System;
using System.IO;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.PlayerSpawn.Utilities.PlayerSpawnMenuNetworkHelper;
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
		}

		private bool HandleRequestCharacterUsage(NetworkCommunicator peer, RequestCharacterUsage message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);
			if (peer == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {peer?.UserName} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Warning);
				return false;
			}

			// If player is already assigned to this team/formation/character, just update its perks
			PlayerAssignment assignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);
			if (assignment.Team == team && assignment.Formation == formation && assignment.Character == character)
			{
				PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);
				return true;
			}

			// Try to reserve the character
			string failReason = string.Empty;
			if (PlayerSpawnMenu.Instance.TrySelectCharacter(peer, team, formation, character, ref failReason))
			{
				// Character successfully reserved
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} reserved character {team.Name} - {formation.Name} - {character.Name}", LogLevel.Information);

				// Update the perks
				PlayerSpawnMenu.Instance.UpdatePerks(peer, team, formation, character, message.SelectedPerks);

				// Notify all players about the character usage
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncPlayerCharacterUsage(peer, team, formation, character));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			// Character couldn't be reserved
			else
			{
				Log($"Alliance - PlayerSpawnMenu - {peer.UserName} can't reserve character {team.Name} - {formation.Name} - {character.Name} : {failReason}", LogLevel.Warning);
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

			HandlePlayerSpawnMenuOperation(message);
			return true;
		}

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
			PlayerSpawnMenuNetworkHelper.SendPlayerSpawnMenuToAll();
		}
	}
}
