using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Core.ExtendedCharacter.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
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

        public Dictionary<BasicCharacterObject, bool> CharacterAvailability => _characterAvailability;
        public event Action<BasicCharacterObject, bool> CharacterAvailabilityChanged;
        private Dictionary<BasicCharacterObject, bool> _characterAvailability = new();
        private Dictionary<BasicCharacterObject, int> _charactersLeft = new();
        private readonly Dictionary<MissionPeer, BasicCharacterObject> _characterSelected = new();

        public ClassLimiterModel()
        {
        }

        public void Init()
        {
            _charactersLeft = new Dictionary<BasicCharacterObject, int>();
            _characterAvailability = new Dictionary<BasicCharacterObject, bool>();
            foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
            {
                ExtendedCharacterObject exCharacter = character.GetExtendedCharacterObject();
                _charactersLeft.Add(character, exCharacter.TroopLimit);
                ChangeCharacterAvailability(character, exCharacter.TroopLimit > 0);
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
            if (missionPeer == null) return false;

            Log($"{missionPeer.Name} is requesting to use {message.Character.Name} ({_charactersLeft[message.Character]} remaining).", LogLevel.Debug);

            bool hadPreviousSelection = _characterSelected.TryGetValue(missionPeer, out BasicCharacterObject previousSelection);

            if (hadPreviousSelection)
            {
                FreeCharacterSlot(previousSelection);
            }

            if (TryReserveCharacterSlot(message.Character))
            {
                // Character is reserved
                _characterSelected[missionPeer] = message.Character;
                SendMessageToPeer($"You reserved {message.Character.Name}. {_charactersLeft[message.Character]} remaining.", peer);
            }
            else
            {
                // No slot remaining
                SendMessageToPeer($"There is no slot remaining for {message.Character.Name} !", peer);
            }
            return true;
        }

        /// <summary>
        /// Request usage of a specific character and check its availability.
        /// </summary>
        /// <returns>True if usage permitted. False otherwise.</returns>
        public bool TryReserveCharacterSlot(BasicCharacterObject character)
        {
            if (_charactersLeft[character] > 0)
            {
                _charactersLeft[character]--;
                if (_charactersLeft[character] == 0)
                {
                    ChangeCharacterAvailability(character, false);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void FreeCharacterSlot(BasicCharacterObject character)
        {
            if (_charactersLeft[character] == 0)
            {
                ChangeCharacterAvailability(character, true);
            }
            _charactersLeft[character]++;
        }

        public void ChangeCharacterAvailability(BasicCharacterObject character, bool isAvailable)
        {
            Log($"Changed availability of {character.Name} to {isAvailable}", LogLevel.Debug);

            _characterAvailability[character] = isAvailable;
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

            foreach (KeyValuePair<BasicCharacterObject, bool> kvp in _characterAvailability)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new CharacterAvailableMessage(kvp.Key, kvp.Value));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
