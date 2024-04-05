using Alliance.Client.Extensions.VOIP.Behaviors;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

namespace Alliance.Client.Extensions.VOIP.ViewModels
{
    /// <summary>
    /// Main view model for VOIP.
    /// </summary>
    public class VoipVM : ViewModel
    {
        private readonly Mission _mission;

        private readonly PBVoiceChatHandlerClient _voiceChatHandler;

        private MBBindingList<MPVoicePlayerVM> _activeVoicePlayers;
        private MBBindingList<SpeakerVM> _activeVoiceBots;

        [DataSourceProperty]
        public MBBindingList<MPVoicePlayerVM> ActiveVoicePlayers
        {
            get
            {
                return _activeVoicePlayers;
            }
            set
            {
                if (value != _activeVoicePlayers)
                {
                    _activeVoicePlayers = value;
                    OnPropertyChangedWithValue(value, "ActiveVoicePlayers");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SpeakerVM> ActiveVoiceBots
        {
            get
            {
                return _activeVoiceBots;
            }
            set
            {
                if (value != _activeVoiceBots)
                {
                    _activeVoiceBots = value;
                    OnPropertyChangedWithValue(value, "ActiveVoiceBots");
                }
            }
        }

        public VoipVM(Mission mission)
        {
            _mission = mission;
            _voiceChatHandler = _mission.GetMissionBehavior<PBVoiceChatHandlerClient>();
            if (_voiceChatHandler != null)
            {
                _voiceChatHandler.OnPeerVoiceStatusUpdated += OnPeerVoiceStatusUpdated;
                _voiceChatHandler.OnBotVoiceStatusUpdated += OnBotVoiceStatusUpdated;
                _voiceChatHandler.OnVoiceRecordStarted += OnVoiceRecordStarted;
                _voiceChatHandler.OnVoiceRecordStopped += OnVoiceRecordStopped;
            }

            ActiveVoicePlayers = new MBBindingList<MPVoicePlayerVM>();
            ActiveVoiceBots = new MBBindingList<SpeakerVM>();
        }

        public override void OnFinalize()
        {
            if (_voiceChatHandler != null)
            {
                _voiceChatHandler.OnPeerVoiceStatusUpdated -= OnPeerVoiceStatusUpdated;
                _voiceChatHandler.OnBotVoiceStatusUpdated -= OnBotVoiceStatusUpdated;
                _voiceChatHandler.OnVoiceRecordStarted -= OnVoiceRecordStarted;
                _voiceChatHandler.OnVoiceRecordStopped -= OnVoiceRecordStopped;
            }

            base.OnFinalize();
        }

        public void OnTick(float dt)
        {
            for (int i = 0; i < ActiveVoicePlayers.Count; i++)
            {
                if (ActiveVoicePlayers[i] != null && !ActiveVoicePlayers[i].IsMyPeer)
                {
                    ActiveVoicePlayers[i].UpdatesSinceSilence++;
                    if (ActiveVoicePlayers[i].UpdatesSinceSilence >= 30)
                    {
                        ActiveVoicePlayers.RemoveAt(i);
                        i--;
                    }
                }
            }
            for (int i = 0; i < ActiveVoiceBots.Count; i++)
            {
                if (ActiveVoiceBots[i] != null)
                {
                    ActiveVoiceBots[i].UpdatesSinceSilence++;
                    if (ActiveVoiceBots[i].UpdatesSinceSilence >= 30)
                    {
                        ActiveVoiceBots.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private void OnPeerVoiceStatusUpdated(MissionPeer peer, bool isTalking, bool forceRemove)
        {
            MPVoicePlayerVM mPVoicePlayerVM = ActiveVoicePlayers.FirstOrDefault((MPVoicePlayerVM vp) => vp.Peer == peer);
            if(forceRemove)
            {
                if (mPVoicePlayerVM != null) mPVoicePlayerVM.UpdatesSinceSilence = MPVoicePlayerVM.UpdatesRequiredToRemoveForSilence;
                return;
            }
            if (isTalking)
            {
                if (mPVoicePlayerVM == null)
                {
                    ActiveVoicePlayers.Add(new MPVoicePlayerVM(peer));
                }
                else
                {
                    mPVoicePlayerVM.UpdatesSinceSilence = 0;
                }
            }
            else if (!isTalking && mPVoicePlayerVM != null)
            {
                mPVoicePlayerVM.UpdatesSinceSilence++;
            }
        }

        private void OnBotVoiceStatusUpdated(Agent bot, bool isTalking, bool forceRemove)
        {
            SpeakerVM mpVoiceBotVM = ActiveVoiceBots.FirstOrDefault((SpeakerVM vp) => vp?.Agent?.Index == bot.Index);
            if (forceRemove)
            {
                if(mpVoiceBotVM != null) mpVoiceBotVM.UpdatesSinceSilence = MPVoicePlayerVM.UpdatesRequiredToRemoveForSilence;
                return;
            }
            if (isTalking)
            {
                if (mpVoiceBotVM == null)
                {
                    ActiveVoiceBots.Add(new SpeakerVM(bot));
                }
                else
                {
                    mpVoiceBotVM.UpdatesSinceSilence = 0;
                }
            }
            else if (!isTalking && mpVoiceBotVM != null)
            {
                mpVoiceBotVM.UpdatesSinceSilence++;
            }
        }

        private void OnVoiceRecordStarted()
        {
            ActiveVoicePlayers.Add(new MPVoicePlayerVM(GameNetwork.MyPeer.GetComponent<MissionPeer>()));
        }

        private void OnVoiceRecordStopped()
        {
            MPVoicePlayerVM item = ActiveVoicePlayers.FirstOrDefault((MPVoicePlayerVM vp) => vp.Peer == GameNetwork.MyPeer.GetComponent<MissionPeer>());
            ActiveVoicePlayers.Remove(item);
        }
    }
}
