using Alliance.Common.GameModes.PvC.Behaviors;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

namespace Alliance.Common.GameModes.PvC.Models
{
    public class PvCScoreboardData : IScoreboardData
    {
        public MissionScoreboardComponent.ScoreboardHeader[] GetScoreboardHeaders()
        {
            return new MissionScoreboardComponent.ScoreboardHeader[9]
            {
                new MissionScoreboardComponent.ScoreboardHeader("ping", (missionPeer) => MathF.Round(missionPeer.GetNetworkPeer().AveragePingInMilliseconds).ToString(), (bot) => ""),
                new MissionScoreboardComponent.ScoreboardHeader("avatar", (missionPeer) => "", (bot) => ""),
                new MissionScoreboardComponent.ScoreboardHeader("badge", (missionPeer) => BadgeManager.GetByIndex(missionPeer.GetPeer().ChosenBadgeIndex)?.StringId, (bot) => ""),
                new MissionScoreboardComponent.ScoreboardHeader("name", (missionPeer) => missionPeer.GetComponent<MissionPeer>().DisplayedName, (bot) => new TextObject("{=hvQSOi79}Bot").ToString()),
                new MissionScoreboardComponent.ScoreboardHeader("kill", (missionPeer) => missionPeer.KillCount.ToString(), (bot) => bot.KillCount.ToString()),
                new MissionScoreboardComponent.ScoreboardHeader("death", (missionPeer) => missionPeer.DeathCount.ToString(), (bot) => bot.DeathCount.ToString()),
                new MissionScoreboardComponent.ScoreboardHeader("assist", (missionPeer) => missionPeer.AssistCount.ToString(), (bot) => bot.AssistCount.ToString()),
                new MissionScoreboardComponent.ScoreboardHeader("gold", (missionPeer) => missionPeer.GetComponent<PvCRepresentative>().Gold.ToString(), (bot) => ""),
                new MissionScoreboardComponent.ScoreboardHeader("score", (missionPeer) => missionPeer.Score.ToString(), (bot) => "".ToString())
            };
        }
    }
}