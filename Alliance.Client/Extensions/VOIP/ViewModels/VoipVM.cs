﻿using Alliance.Common.Extensions.VOIP.Behaviors;
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

        private readonly VoipHandler _voiceChatHandler;

        private MBBindingList<MPVoicePlayerVM> _activeVoicePlayers;

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

        public VoipVM(Mission mission)
        {
            _mission = mission;
            _voiceChatHandler = _mission.GetMissionBehavior<VoipHandler>();
            if (_voiceChatHandler != null)
            {
                _voiceChatHandler.OnPeerVoiceStatusUpdated += OnPeerVoiceStatusUpdated;
                _voiceChatHandler.OnVoiceRecordStarted += OnVoiceRecordStarted;
                _voiceChatHandler.OnVoiceRecordStopped += OnVoiceRecordStopped;
            }

            ActiveVoicePlayers = new MBBindingList<MPVoicePlayerVM>();
        }

        public override void OnFinalize()
        {
            if (_voiceChatHandler != null)
            {
                _voiceChatHandler.OnPeerVoiceStatusUpdated -= OnPeerVoiceStatusUpdated;
                _voiceChatHandler.OnVoiceRecordStarted -= OnVoiceRecordStarted;
                _voiceChatHandler.OnVoiceRecordStopped -= OnVoiceRecordStopped;
            }

            base.OnFinalize();
        }

        public void OnTick(float dt)
        {
            for (int i = 0; i < ActiveVoicePlayers.Count; i++)
            {
                if (!ActiveVoicePlayers[i].IsMyPeer && ActiveVoicePlayers[i].UpdatesSinceSilence >= 30)
                {
                    ActiveVoicePlayers.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnPeerVoiceStatusUpdated(MissionPeer peer, bool isTalking)
        {
            MPVoicePlayerVM mPVoicePlayerVM = ActiveVoicePlayers.FirstOrDefault((MPVoicePlayerVM vp) => vp.Peer == peer);
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
