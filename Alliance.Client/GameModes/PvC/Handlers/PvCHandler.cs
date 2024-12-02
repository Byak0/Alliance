using Alliance.Common.Extensions;
using NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Client.GameModes.PvC.Handlers
{
    public class PvCHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncGoldsForSkirmish>(HandleServerEventUpdateGold);
            reg.Register<FlagDominationMoraleChangeMessage>(HandleMoraleChangedMessage);
            reg.Register<FlagDominationFlagsRemovedMessage>(HandleFlagsRemovedMessage);
            reg.Register<FlagDominationCapturePointMessage>(HandleServerEventPointCapturedMessage);
            reg.Register<FormationWipedMessage>(HandleServerEventFormationWipedMessage);
        }

        public void HandleServerEventUpdateGold(SyncGoldsForSkirmish message)
        {
            MissionMultiplayerGameModeBaseClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            gameModeClient.OnGoldAmountChangedForRepresentative(message.VirtualPlayer.GetComponent<MissionRepresentativeBase>(), message.GoldAmount);
        }

        // Works only for PvC GameMode
        public void HandleMoraleChangedMessage(FlagDominationMoraleChangeMessage message)
        {
            string gameMode = MultiplayerOptions.OptionType.GameType.GetStrValue();
            if (gameMode != "PvC" && gameMode != "CvC") return;

            MissionMultiplayerGameModeFlagDominationClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();

            gameModeClient?.OnMoraleChanged(message.Morale);
        }

        // Works only for PvC GameMode
        public void HandleFlagsRemovedMessage(FlagDominationFlagsRemovedMessage message)
        {
            string gameMode = MultiplayerOptions.OptionType.GameType.GetStrValue();
            if (gameMode != "PvC" && gameMode != "CvC") return;

            MissionMultiplayerGameModeFlagDominationClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();

            gameModeClient?.OnNumberOfFlagsChanged();
        }

        // Works only for PvC GameMode
        public void HandleServerEventPointCapturedMessage(FlagDominationCapturePointMessage message)
        {
            string gameMode = MultiplayerOptions.OptionType.GameType.GetStrValue();
            if (gameMode != "PvC" && gameMode != "CvC") return;

            MissionMultiplayerGameModeFlagDominationClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
            if (gameModeClient == null) return;

            Team ownerTeam = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(message.OwnerTeamIndex);

            foreach (FlagCapturePoint flagCapturePoint in gameModeClient.AllCapturePoints)
            {
                if (flagCapturePoint.FlagIndex == message.FlagIndex)
                {
                    gameModeClient.OnCapturePointOwnerChanged(flagCapturePoint, ownerTeam);
                    break;
                }
            }
        }

        public void HandleServerEventFormationWipedMessage(FormationWipedMessage message)
        {
            MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
            Vec3 vec = cameraFrame.origin + cameraFrame.rotation.u;
            MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/squad_wiped"), vec);
        }
    }
}
