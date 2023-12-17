using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
    /// <summary>
    /// Party hard
    /// </summary>
    public class CS_PartyHard : CS_UsableObject
    {
        public string Particle1 = "fireworks_1";
        public string Particle2 = "fireworks_2";
        public string Particle3 = "fireworks_3";
        public string Particle4 = "fireworks_4";
        public string Particle5 = "fireworks_5";
        public string Sound1 = "";
        public string Sound2 = "";
        public string Sound3 = "";
        public string Sound4 = "";
        public string Sound5 = "";
        public int SoundDuration1 = 0;
        public int SoundDuration2 = 0;
        public int SoundDuration3 = 0;
        public int SoundDuration4 = 0;
        public int SoundDuration5 = 0;

        private List<GameEntity> _emitters;
        private List<string> _particles;
        private List<string> _sounds;
        private List<int> _soundsDurations;

        protected CS_PartyHard()
        {
        }

        protected override void OnInit()
        {
            base.OnInit();
            _emitters = new List<GameEntity>();
            _emitters = GameEntity.CollectChildrenEntitiesWithTag("emitter");
            _particles = new List<string>();
            if (Particle1 != "") _particles.Add(Particle1);
            if (Particle2 != "") _particles.Add(Particle2);
            if (Particle3 != "") _particles.Add(Particle3);
            if (Particle4 != "") _particles.Add(Particle4);
            if (Particle5 != "") _particles.Add(Particle5);
            _sounds = new List<string>();
            if (Sound1 != "") _sounds.Add(Sound1);
            if (Sound2 != "") _sounds.Add(Sound2);
            if (Sound3 != "") _sounds.Add(Sound3);
            if (Sound4 != "") _sounds.Add(Sound4);
            if (Sound5 != "") _sounds.Add(Sound5);
            _soundsDurations = new List<int>
            {
                SoundDuration1,
                SoundDuration2,
                SoundDuration3,
                SoundDuration4,
                SoundDuration5
            };

        }

        protected override void AfterUse(Agent userAgent, bool actionCompleted = true)
        {
            base.AfterUse(userAgent);

            NetworkCommunicator player = userAgent?.MissionPeer?.GetNetworkPeer();

            // Only admins can party hard
            if (player == null || !player.IsAdmin()) return;

            SyncParticle(MBRandom.RandomInt(0, _emitters.Count), MBRandom.RandomInt(0, _particles.Count));

            int randomIndex = MBRandom.RandomInt(0, _sounds.Count);
            int soundIndex = SoundEvent.GetEventIdFromString(_sounds[randomIndex]);
            int soundDuration = _soundsDurations[randomIndex];
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncSound(soundIndex, soundDuration));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        public void SyncParticle(int emitterIndex, int particleIndex)
        {
            Vec3 pos = _emitters[emitterIndex].GlobalPosition;
            string particle = _particles[particleIndex];
            string log = "Party hard : Playing " + particle + " on position " + pos;
            if (GameNetwork.IsClient)
            {
                var tempEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
                MatrixFrame frame = MatrixFrame.Identity;
                ParticleSystem.CreateParticleSystemAttachedToEntity(particle, tempEntity, ref frame);
                var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in pos), pos);
                tempEntity.SetGlobalFrame(globalFrame);
                tempEntity.FadeOut(30, true);
            }
            else if (GameNetwork.IsServerOrRecorder)
            {
                Log(log, LogLevel.Information);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncParticleObject(Id, emitterIndex, particleIndex));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }
        }

        static CS_PartyHard()
        {
        }
    }
}
