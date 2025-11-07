using Alliance.Common.Core.Security.Models;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Security
{
	public sealed class PlayerStore
	{
		public readonly Dictionary<PlayerId, AL_PlayerData> AllPlayersData = new Dictionary<PlayerId, AL_PlayerData>();
		public readonly Dictionary<NetworkCommunicator, AL_PlayerData> OnlinePlayersData = new Dictionary<NetworkCommunicator, AL_PlayerData>();
		public readonly HashSet<NetworkCommunicator> OnlineAdmins = new HashSet<NetworkCommunicator>();
		public readonly Dictionary<PlayerId, NetworkCommunicator> PlayerIdToCommunicator = new Dictionary<PlayerId, NetworkCommunicator>();

		public static PlayerStore Instance { get; } = new PlayerStore();

		private PlayerStore() { }

		public DateTime lastRead = DateTime.MinValue;
		private FileSystemWatcher _playerDataWatcher;
		private PlayerDataFile _playerDataFile = new PlayerDataFile();
		private string _playerDataFilePath = string.Empty;
		private bool _suppressWatcher;

		/// <summary>
		/// Initialize the players store from a file and keep watch on it.
		/// </summary>
		public void InitFromFile(string path)
		{
			_playerDataFilePath = path;
			_playerDataFile = SerializeHelper.LoadClassFromFile(_playerDataFilePath, new PlayerDataFile());
			InitPlayersData();

			// Watch changes to the roles file
			_playerDataWatcher = SerializeHelper.CreateFileWatcher(_playerDataFilePath, OnPlayerDataFileChanged);
		}

		/// <summary>
		/// Serialize to file.
		/// </summary>
		public void Save()
		{
			_suppressWatcher = true; // prevent watcher reacting to our own save
			_playerDataFile.Players = AllPlayersData.Values.ToList();
			SerializeHelper.SaveClassToFile(_playerDataFilePath, _playerDataFile);
		}

		/// <summary>
		/// Initialize the players store from a PlayerDataFile instance.
		/// </summary>
		public void InitFromData(PlayerDataFile data)
		{
			_playerDataFile = data;
			InitPlayersData();
		}

		/// <summary>
		/// Called when changes are made manually to the file.
		/// </summary>
		private void OnPlayerDataFileChanged(object source, FileSystemEventArgs e)
		{
			try
			{
				DateTime lastWriteTime = File.GetLastWriteTime(_playerDataFilePath);
				if (lastWriteTime.Ticks - lastRead.Ticks > 100000)
				{
					lastRead = lastWriteTime;

					if (_suppressWatcher)
					{
						_suppressWatcher = false; // re-enable after one ignore
						return; // ignore file change caused by internal Save()
					}

					_playerDataFile = SerializeHelper.LoadClassFromFile(_playerDataFilePath, _playerDataFile);
					InitPlayersData();
					PlayerSyncService.BroadcastPlayerStore();
					Log($"Alliance - Player datafile was edited externally at {lastWriteTime}");
				}
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to update player data :", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		private void InitPlayersData()
		{
			AllPlayersData.Clear();
			OnlinePlayersData.Clear();
			OnlineAdmins.Clear();
			PlayerIdToCommunicator.Clear();

			// Populate AllPlayersData dictionary
			foreach (AL_PlayerData playerData in _playerDataFile.Players)
			{
				AllPlayersData[playerData.Id] = playerData;
			}
			if (GameNetwork.NetworkPeers == null) return;
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				LoadPlayerData(peer);
			}
		}

		/// <summary>
		/// Load a player's data from the database when they connect.
		/// Returns the data if it was found and loaded, null otherwise.
		/// </summary>
		public AL_PlayerData LoadPlayerData(NetworkCommunicator peer)
		{
			if (peer?.VirtualPlayer == null) return null;
			AL_PlayerData playerData = _playerDataFile.Players.Find(player => player.Id == peer.VirtualPlayer.Id);
			if (playerData != null)
			{
				OnlinePlayersData[peer] = playerData;
				PlayerIdToCommunicator[peer.VirtualPlayer.Id] = peer;
				if (playerData.IsAdmin) OnlineAdmins.Add(peer);
				return playerData;
			}
			return null;
		}

		/// <summary>
		/// Unload a player's data (when they disconnect).
		/// </summary>
		public void UnloadPlayerData(NetworkCommunicator peer)
		{
			if (OnlinePlayersData.ContainsKey(peer)) OnlinePlayersData.Remove(peer);
			if (OnlineAdmins.Contains(peer)) OnlineAdmins.Remove(peer);
			if (PlayerIdToCommunicator.ContainsKey(peer.VirtualPlayer.Id)) PlayerIdToCommunicator.Remove(peer.VirtualPlayer.Id);
		}
	}
}
