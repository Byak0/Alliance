using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.GameModeMenu.Behaviors
{
    /// <summary>
    /// TODO : Placeholder class for now, not used yet.
    /// MissionBehavior used to handle polls.
    /// Players can suggest and vote for a custom GameMode/Map/Options.
    /// Once the vote is over, the winning GameMode will begin.
    /// </summary>
    public class PollBehavior : MissionNetwork, IMissionBehavior
    {
        public List<Poll> Polls { get; set; }

        public PollBehavior() : base()
        {
            Polls = new List<Poll>();
            Module.CurrentModule.GetMultiplayerGameTypes();
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
        }

        public override void OnMissionTick(float dt)
        {
            foreach (Poll poll in Polls.FindAll(p => p.State != Poll.PollState.Ended))
            {
                poll.LifeTime += dt;
                if (poll.State is Poll.PollState.None) PreparePoll(poll);
                if (poll.State is Poll.PollState.BeforeVote && poll.LifeTime >= poll.TimeBeforeVote) StartVote(poll);
                if (poll.State is Poll.PollState.Voting && poll.LifeTime >= poll.TimeToVote) EndVote(poll);
            }
        }

        public void CreateNativeGameModePoll()
        {
            Poll poll = new Poll("Vote for the next game :");
            poll.Choices.Add(new GameModePollChoice(0, "FFA", () => { CreateNativeGameModePoll(); }, null));
            poll.Choices.Add(new GameModePollChoice(1, "TDM", null, null));
            Polls.Add(poll);
        }

        public void PreparePoll(Poll poll)
        {
            poll.State = Poll.PollState.BeforeVote;
            // Sync announcement that voting will soon begin
        }

        public void StartVote(Poll poll)
        {
            poll.State = Poll.PollState.Voting;
            // Sync announcement that voting has started
        }

        public void EndVote(Poll poll)
        {
            poll.State = Poll.PollState.Ended;
            // Sync announcement that voting has ended
            poll.GetWinningChoice().OnChoiceWin();
        }

        public void Vote(int playerId, Poll poll, PollChoice choice)
        {
            if (poll == null || poll.State != Poll.PollState.Voting || choice == null || choice == poll.PlayersVotes[playerId]) return;
            if (poll.PlayersVotes[playerId] != null) poll.PlayersVotes[playerId].Votes--;
            poll.PlayersVotes[playerId] = choice;
            poll.PlayersVotes[playerId].Votes++;
        }
    }

    /// <summary>
    /// Poll class. Contains a list of choices.
    /// </summary>
    public class Poll
    {
        public string PollName { get; set; }
        public List<PollChoice> Choices { get; set; }
        public Dictionary<int, PollChoice> PlayersVotes { get; set; }
        public PollState State { get; set; }
        public float LifeTime = 0f;
        public float TimeBeforeVote = 30f;
        public float TimeToVote = 30f;

        public Poll(string pollName)
        {
            PollName = pollName;
            Choices = new List<PollChoice>();
            PlayersVotes = new Dictionary<int, PollChoice>();
            State = PollState.None;
        }

        /// <summary>
        /// Return the choice with most votes.
        /// In case of draw, choose randomly among the winning choices.
        /// </summary>
        public PollChoice GetWinningChoice()
        {
            if (Choices.Count == 0) return null;

            List<PollChoice> tiedChoices = new List<PollChoice>();
            PollChoice winningChoice = Choices[0];

            foreach (PollChoice choice in Choices)
            {
                if (choice.Votes > winningChoice.Votes)
                {
                    tiedChoices.Clear();
                    winningChoice = choice;
                }
                else if (choice.Votes == winningChoice.Votes)
                {
                    tiedChoices.Add(choice);
                }
            }

            if (tiedChoices.Count > 0)
            {
                winningChoice = tiedChoices[MBRandom.RandomInt(0, tiedChoices.Count)];
            }

            return winningChoice;
        }

        public enum PollState
        {
            None,
            BeforeVote,
            Voting,
            Ended
        }
    }

    /// <summary>
    /// Poll choice.
    /// </summary>
    public class PollChoice
    {
        public int Index { get; }
        public string Name { get; set; }
        public int Votes { get; set; }
        public Action OnChoiceWin;

        public PollChoice(int index, string name, Action onChoiceWin)
        {
            Index = index;
            Name = name;
            Votes = 0;
            OnChoiceWin = onChoiceWin;
        }
    }

    public class GameModePollChoice : PollChoice
    {
        public MultiplayerGameMode MultiplayerGameMode { get; set; }

        public GameModePollChoice(int index, string name, Action onChoiceWin, MultiplayerGameMode multiplayerGameMode) : base(index, name, onChoiceWin)
        {
            MultiplayerGameMode = multiplayerGameMode;
        }
    }

    public class TeamPollChoice : PollChoice
    {
        public Team Team { get; set; }

        public TeamPollChoice(int index, string name, Action onChoiceWin, Team team) : base(index, name, onChoiceWin)
        {
            Team = team;
        }
    }
}