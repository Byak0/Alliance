using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.ClassLimiter.Models
{
	/// <summary>
	/// Singleton model storing which characters are available in Equipment selection menu.
	/// Synchronized between server and clients.
	/// </summary>
	public class ClassLimiterModel
	{
		private static readonly ClassLimiterModel instance = new();
		public static ClassLimiterModel Instance { get { return instance; } }

		public event Action<BasicCharacterObject, bool> CharacterAvailabilityChanged;
		public Dictionary<BasicCharacterObject, CharacterAvailability> CharactersAvailability { get; private set; }
		public Dictionary<BasicCharacterObject, bool> CharactersAvailable { get; private set; }

		private Dictionary<PlayerId, BasicCharacterObject> _characterSelected = new();

		public ClassLimiterModel()
		{
		}

		public void Init()
		{
			CharactersAvailable = new();
			foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
			{
				CharactersAvailable.Add(character, false);
				//ChangeCharacterAvailability(character, true);
			}

			if (GameNetwork.IsServer)
			{
				CharactersAvailable = new();
				CharactersAvailability = new();
				foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
				{
					CharactersAvailable.Add(character, false);
					CharactersAvailability.Add(character, new CharacterAvailability(character));
					ChangeCharacterAvailability(character, CharactersAvailability[character].IsAvailable);
				}
				foreach (KeyValuePair<PlayerId, BasicCharacterObject> kvp in _characterSelected)
				{
					Log($"Reserving slot for {kvp.Key.Id1} : {kvp.Value.Name}", LogLevel.Debug);
					ReserveCharacterSlot(kvp.Value);
				}
			}
		}

		/// <summary>
		/// Client method - Request usage of specified character.
		/// </summary>
		public void RequestUsage(BasicCharacterObject character)
		{
			Log($"Requesting usage of {character.Name}", LogLevel.Debug);
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestCharacterUsage(character));
			GameNetwork.EndModuleEventAsClient();
		}

		/// <summary>
		/// Server method - Handle usage request of specific character by player.
		/// </summary>
		public bool HandleRequestUsage(NetworkCommunicator peer, RequestCharacterUsage message)
		{
			MissionPeer missionPeer = peer.GetComponent<MissionPeer>();
			if (missionPeer == null || !Config.Instance.UsePlayerLimit) return false;

			Log($"{missionPeer.Name} is requesting to use {message.Character.Name} ({CharactersAvailability[message.Character].Slots} remaining).", LogLevel.Debug);

			if (CharactersAvailability[message.Character].IsAvailable)
			{
				bool hadPreviousSelection = _characterSelected.TryGetValue(missionPeer.Peer.Id, out BasicCharacterObject previousSelection);
				if (hadPreviousSelection)
				{
					CharactersAvailability[previousSelection].FreeSlot();
					if (CharactersAvailability[previousSelection].IsAvailable)
					{
						ChangeCharacterAvailability(previousSelection, true);
					}
				}
				ReserveCharacterSlot(message.Character);
				_characterSelected[missionPeer.Peer.Id] = message.Character;
				SendMessageToPeer($"You reserved {message.Character.Name} ({CharactersAvailability[message.Character].Taken}/{CharactersAvailability[message.Character].Slots})", peer);
			}
			else
			{
				SendMessageToPeer($"There is no slot remaining for {message.Character.Name} !", peer);
			}
			return true;
		}

		/// <summary>
		/// Request usage of a specific character and check its availability.
		/// </summary>
		public void ReserveCharacterSlot(BasicCharacterObject character)
		{
			CharactersAvailability[character].ReserveSlot();
			if (!CharactersAvailability[character].IsAvailable)
			{
				ChangeCharacterAvailability(character, false);
			}
		}

		public void ChangeCharacterAvailability(BasicCharacterObject character, bool isAvailable)
		{
			if (isAvailable == CharactersAvailable[character]) return;

			Log($"Changed availability of {character.Name} to {isAvailable}", LogLevel.Debug);

			CharactersAvailable[character] = isAvailable;
			CharacterAvailabilityChanged?.Invoke(character, isAvailable);

			if (GameNetwork.IsServer)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new CharacterAvailableMessage(character, isAvailable));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		/// <summary>
		/// Server method - Send available characters to a client.
		/// </summary>
		public void SendAvailableCharactersToClient(NetworkCommunicator peer)
		{
			Log($"Sending available characters to {peer.UserName}", LogLevel.Debug);

			foreach (KeyValuePair<BasicCharacterObject, bool> kvp in CharactersAvailable)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new CharacterAvailableMessage(kvp.Key, kvp.Value));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	public class CharacterAvailability
	{
		private int _taken;

		public BasicCharacterObject Character { get; set; }
		public ExtendedCharacter ExtendedCharacter { get; set; }
		public int Taken => _taken;
		public int Slots => GetSlots();
		public bool IsAvailable => _taken < Slots;

		public CharacterAvailability(BasicCharacterObject character)
		{
			Character = character;
			ExtendedCharacter = character.GetExtendedCharacterObject();
			_taken = 0;
		}

		public void ReserveSlot()
		{
			_taken++;
		}

		public void FreeSlot()
		{
			_taken--;
		}

		private int GetSlots()
		{
			int scaledPlayerCount = GameNetwork.NetworkPeers.Count + MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			return ExtendedCharacter.HardLimit ? ExtendedCharacter.PlayerSelectLimit : ExtendedCharacter.PlayerSelectLimit * scaledPlayerCount / 100;
		}
	}
}
