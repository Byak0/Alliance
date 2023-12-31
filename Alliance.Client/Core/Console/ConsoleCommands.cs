﻿using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromClient;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Core.Console
{
    public static class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("get_player_id", "alliance")]
        public static string GetPlayerId(List<string> playerName)
        {
            string name = playerName.ElementAtOrValue(0, "");
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            List<NetworkCommunicator> matchingPlayers = GameNetwork.NetworkPeers?.ToMBList().FindAll(x => x.UserName.Contains(name));
            string result = "";
            foreach (NetworkCommunicator player in matchingPlayers)
            {
                result += player.UserName + " : " + player.VirtualPlayer?.Id + "\n";
            }
            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("play_sound", "alliance")]
        public static string PlaySound(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }
            else if (args.ElementAtOrValue(0, "") == "")
            {
                return "Usage: alliance.play_sound sound_name sound_duration";
            }

            int soundIndex = SoundEvent.GetEventIdFromString(args[0]);
            int soundDuration = 300;
            if (int.TryParse(args.ElementAtOrValue(1, ""), out int duration))
            {
                soundDuration = duration;
            }

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new SoundRequest(soundIndex, soundDuration));
            GameNetwork.EndModuleEventAsClient();

            return "Requested server to play " + soundIndex + " for " + soundDuration + " seconds.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("toggle_invulnerable", "alliance")]
        public static string ToggleInvulnerable(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }
            else if (args.Count() == 0)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new AdminClient() { ToggleInvulnerable = true, PlayerSelected = "" });
                GameNetwork.EndModuleEventAsClient();
                return "Requested server to toggle invulnerable for all";
            }

            string name = ConcatenateString(args);
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.UserName.ToString() == name).FirstOrDefault();
            if (playerSelected == null) return "No player found with name " + name;

            string playerId = playerSelected.VirtualPlayer?.Id.ToString();

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { ToggleInvulnerable = true, PlayerSelected = playerId });
            GameNetwork.EndModuleEventAsClient();
            return "Requested server to toggle invulnerable for " + name;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("send_notification", "alliance")]
        public static string SendNotification(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }

            string text = ConcatenateString(args);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestNotification(text, 0));
            GameNetwork.EndModuleEventAsClient();

            return "Requested server to send notification : " + text;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("send_information", "alliance")]
        public static string SendInformation(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }

            string text = ConcatenateString(args);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestNotification(text, 1));
            GameNetwork.EndModuleEventAsClient();

            return "Requested server to send information : " + text;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("send_message", "alliance")]
        public static string SendMessage(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }

            string text = ConcatenateString(args);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestNotification(text, 2));
            GameNetwork.EndModuleEventAsClient();

            return "Requested server to send message : " + text;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_horse", "alliance")]
        public static string SpawnHorse(List<string> args)
        {
            if (GameNetwork.NetworkPeerCount == 0)
            {
                return "Log into a server to use this command.";
            }
            else if (!GameNetwork.MyPeer.IsAdmin())
            {
                return "You need to be admin to use this command.";
            }

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new SpawnHorseRequest());
            GameNetwork.EndModuleEventAsClient();

            return "Requested server to spawn hrose";
        }

        public static string ConcatenateString(List<string> strings)
        {
            if (strings == null || strings.IsEmpty())
            {
                return string.Empty;
            }
            string text = strings[0];
            if (strings.Count > 1)
            {
                for (int i = 1; i < strings.Count; i++)
                {
                    text = text + " " + strings[i];
                }
            }
            return text;
        }
    }
}
