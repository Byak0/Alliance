using Alliance.Common.Extensions.VOIP.Utilities;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.VOIP.Models
{
    public class BotVoiceData : VoiceData
    {
        public Agent Agent { get; private set; }

        public BotVoiceData(Agent agent, int sampleRate, int channels) : base(sampleRate, channels)
        {
            Agent = agent;
        }

        public override Vec3 GetSpeakerPosition()
        {
            return Agent.Position;
        }
    }

    public class PeerVoiceData : VoiceData
    {
        public NetworkCommunicator Peer { get; private set; }

        public PeerVoiceData(NetworkCommunicator peer, int sampleRate, int channels) : base(sampleRate, channels)
        {
            Peer = peer;
        }

        public override Vec3 GetSpeakerPosition()
        {
            return Peer.ControlledAgent?.Position ?? Vec3.Zero;
        }
    }

    public abstract class VoiceData : IDisposable
    {
        public DateTime LastUpdate { get; set; }

        protected BufferedWaveProvider waveProvider;
        protected VolumeSampleProvider volumeProvider;
        protected PanningSampleProvider panProvider;
        protected WaveOutEvent waveOut;
        protected Queue<short> voiceDataQueue;
        protected OpusDecoder decoder;

        public VoiceData(int sampleRate, int channels)
        {
            LastUpdate = DateTime.Now;

            if (GameNetwork.IsClient)
            {
                waveOut = new WaveOutEvent();

                waveProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, channels));
                waveProvider.DiscardOnBufferOverflow = true;
                waveOut.Init(waveProvider);

                volumeProvider = new VolumeSampleProvider(waveProvider.ToSampleProvider());
                panProvider = new PanningSampleProvider(volumeProvider);
                volumeProvider.Volume = 1;
                waveOut.Init(panProvider);

                voiceDataQueue = new Queue<short>();
                decoder = new OpusDecoder(sampleRate, channels);
            }
        }

        public abstract Vec3 GetSpeakerPosition();

        public void UpdatePanning(Vec3 listenerPosition, Mat3 listenerRotation)
        {
            Vec3 speakerPosition = GetSpeakerPosition();

            // Calculate the position difference between the speaker and the listener
            float pan = VoipHelper.CalculatePan(speakerPosition, listenerPosition, listenerRotation);

            // Apply panning to the left and right channels
            float clampedVolume = VoipHelper.CalculateVolume(speakerPosition, listenerPosition, VoipConstants.MAX_HEARING_RANGE) * 2;

            panProvider.Pan = -pan;
            volumeProvider.Volume = clampedVolume;
        }

        public void UpdateVoiceData(byte[] compressedData, int compressedLength)
        {
            LastUpdate = DateTime.Now;

            if (GameNetwork.IsClient)
            {
                try
                {
                    if (compressedData == null || compressedLength <= 0 || compressedLength > compressedData.Length)
                    {
                        throw new ArgumentException("Invalid compressed data or length.");
                    }

                    short[] outputBuffer = new short[VoipConstants.FRAME_SIZE];
                    int frameSizeDecoded = decoder.Decode(compressedData, 0, compressedLength, outputBuffer, 0, VoipConstants.FRAME_SIZE, false);

                    foreach (short sample in outputBuffer)
                    {
                        voiceDataQueue.Enqueue(sample);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Decompression error: {ex}", LogLevel.Error);
                }
            }
        }

        public void Play()
        {
            try
            {
                // Process the queue in chunks to avoid converting the entire queue to an array
                while (voiceDataQueue.Count >= VoipConstants.FRAME_SIZE)
                {
                    byte[] buffer = new byte[VoipConstants.FRAME_SIZE * 2]; // 2 bytes per short
                    for (int i = 0; i < VoipConstants.FRAME_SIZE; i++)
                    {
                        short sample = voiceDataQueue.Dequeue();
                        Buffer.BlockCopy(BitConverter.GetBytes(sample), 0, buffer, i * 2, 2);
                    }
                    waveProvider.AddSamples(buffer, 0, buffer.Length);
                }

                // Play the audio if not already playing and there is enough buffered data
                if (waveOut != null && waveOut.PlaybackState != PlaybackState.Playing && waveProvider.BufferedBytes > VoipConstants.MINIMUM_BUFFERED_BYTES_TO_PLAY)
                {
                    waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                Log($"Error while playing voice: {ex}", LogLevel.Error);
            }
        }

        public bool IsExpired(TimeSpan expirationTime)
        {
            return (DateTime.Now - LastUpdate) > expirationTime;
        }

        public void Dispose()
        {
            waveOut?.Dispose();
            waveProvider?.ClearBuffer();
            waveOut = null;
            waveProvider = null;
            decoder = null;
        }
    }
}
