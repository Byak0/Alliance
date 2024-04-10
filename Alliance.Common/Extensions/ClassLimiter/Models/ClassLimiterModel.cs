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
        private Dictionary<BasicCharacterObject, int> _charactersTaken = new();
        private Dictionary<MissionPeer, BasicCharacterObject> _characterSelected = new();

        public ClassLimiterModel()
        {
        }

        public void Init()
        {
            _charactersTaken = new Dictionary<BasicCharacterObject, int>();
            _characterAvailability = new Dictionary<BasicCharacterObject, bool>();
            foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
            {
                ExtendedCharacter exCharacter = character.GetExtendedCharacterObject();
                _charactersTaken.Add(character, 0);
                int scaledPlayerCount = GameNetwork.NetworkPeers.Count + MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
                bool isAvailable = exCharacter.HardLimit ? exCharacter.PlayerSelectLimit >= 1 : exCharacter.PlayerSelectLimit * scaledPlayerCount / 100 >= 1;
                ChangeCharacterAvailability(character, isAvailable);
            }
            _characterSelected = new();
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

            Log($"{missionPeer.Name} is requesting to use {message.Character.Name} ({_charactersTaken[message.Character]} remaining).", LogLevel.Debug);

            bool hadPreviousSelection = _characterSelected.TryGetValue(missionPeer, out BasicCharacterObject previousSelection);

            if (hadPreviousSelection)
            {
                FreeCharacterSlot(previousSelection);
            }

            if (TryReserveCharacterSlot(message.Character))
            {
                // Character is reserved
                _characterSelected[missionPeer] = message.Character;
                SendMessageToPeer($"You reserved {message.Character.Name}. {_charactersTaken[message.Character]} remaining.", peer);
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
            ExtendedCharacter exCharacter = character.GetExtendedCharacterObject();
            int scaledPlayerCount = GameNetwork.NetworkPeers.Count + MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
            int availableSlots = exCharacter.HardLimit ? exCharacter.PlayerSelectLimit : exCharacter.PlayerSelectLimit * scaledPlayerCount / 100;
            if (_charactersTaken[character] < availableSlots)
            {
                _charactersTaken[character]++;
                if (_charactersTaken[character] >= availableSlots)
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
            ExtendedCharacter exCharacter = character.GetExtendedCharacterObject();
            int availableSlots = exCharacter.HardLimit ? exCharacter.PlayerSelectLimit : exCharacter.PlayerSelectLimit * GameNetwork.NetworkPeers.Count / 100;
            _charactersTaken[character]--;
            if (!_characterAvailability[character] && availableSlots > _charactersTaken[character])
            {
                ChangeCharacterAvailability(character, true);
            }
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
