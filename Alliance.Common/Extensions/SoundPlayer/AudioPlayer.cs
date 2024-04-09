using NAudio.Wave;
using System;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.SoundPlayer
{
    /// <summary>
    /// Test, WIP
    /// </summary>
    public class AudioPlayer
    {
        private IWavePlayer waveOutDevice;
        private AudioFileReader audioFileReader;

        public AudioPlayer()
        {
            waveOutDevice = new WaveOutEvent();
        }

        public void Play(string filePath, float volume = 1f)
        {
            Stop(); // Ensure that any previous audio is stopped before starting a new one
            try
            {
                audioFileReader = new AudioFileReader(filePath); // Supports multiple file formats
                audioFileReader.Volume = volume;
                waveOutDevice.Init(audioFileReader);
                waveOutDevice.Play();
            }
            catch (Exception ex)
            {
                Log($"An error occurred when playing audio: {ex.Message}", LogLevel.Debug);
                // Handle exceptions, such as file not found or unsupported file format
            }
        }

        public void Pause()
        {
            if (waveOutDevice != null)
                waveOutDevice.Pause();
        }

        public void Resume()
        {
            if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Paused)
                waveOutDevice.Play();
        }

        public void Stop()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }

            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                audioFileReader = null;
            }
        }

        public void SetVolume(float volume)
        {
            if (audioFileReader != null)
            {
                audioFileReader.Volume = volume;
            }
        }

        public void Dispose()
        {
            Stop(); // Stop and clean up resources
            if (waveOutDevice != null)
            {
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }
    }
}
