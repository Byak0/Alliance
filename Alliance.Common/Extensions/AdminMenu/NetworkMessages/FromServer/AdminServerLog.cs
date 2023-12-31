﻿using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AdminServerLog : GameNetworkMessage
    {
        public string LogMessage { get; set; }
        public string Color { get; set; }

        public AdminServerLog()
        {
        }

        public AdminServerLog(string logMsg, ColorList color)
        {

            LogMessage = logMsg;
            Color = InitColorChoice(color);
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(LogMessage);
            WriteStringToPacket(Color);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            LogMessage = ReadStringFromPacket(ref bufferReadValid);
            Color = ReadStringFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }



        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }
        protected override string OnGetLogFormat()
        {
            return "Serveur Network message log";
        }

        public string InitColorChoice(ColorList listColor)
        {
            switch (listColor)
            {
                case ColorList.Success:
                    {
                        return "Alliance.Text.Success";
                    }
                case ColorList.Info:
                    {
                        return "Alliance.Text.Info";
                    }
                case ColorList.Warning:
                    {
                        return "Alliance.Text.Warning";
                    }
                case ColorList.Danger:
                    {
                        return "Alliance.Text.Danger";
                    }
                default:
                    {
                        return "Alliance.Text.Danger";
                    }
            }
        }

        public enum ColorList
        {
            Success = 0,
            Info = 1,
            Warning = 2,
            Danger = 3,
        }
    }
}
