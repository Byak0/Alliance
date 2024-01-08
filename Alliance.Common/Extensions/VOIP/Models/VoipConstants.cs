namespace Alliance.Common.Extensions.VOIP.Models
{
    public class VoipConstants
    {
        /// <summary>
        /// Number of audio samples per second. 48 kHz is standard, but 12kHz is sufficient for our case.
        /// </summary>
        public const int SampleRate = 12000;

        /// <summary>
        /// Mono (1) or stereo (2) audio.
        /// </summary>
        public const int Channels = 1;

        /// <summary>
        /// Number of samples per channel that Opus will process in one frame (SampleRate * RefreshRate).
        /// </summary>
        public const int FrameSize = 720;

        /// <summary>
        /// Size in bytes of the raw audio data for one frame (FrameSize * 2 bytes).
        /// </summary>
        public const int VoiceFrameRawSizeInBytes = 1440;

        /// <summary>
        /// Maximum size of the compressed data buffer.
        /// </summary>
        public const int CompressionMaxChunkSizeInBytes = 1440;

        /// <summary>
        /// Maximum size of the (uncompressed) buffer.
        /// </summary>
        public const int VoiceRecordMaxChunkSizeInBytes = 72000;

        /// <summary>
        /// Minimum buffered bytes to play.
        /// </summary>
        public const int MinimumBufferedBytesToPlay = 1440;

        /// <summary>
        /// Maximum concurrent voices a player can hear.
        /// </summary>
        public const int MaxConcurrentVoices = 10;

        /// <summary>
        /// Maximum hearing range for a standard voice.
        /// </summary>
        public const float MaxHearingRange = 30f;
    }
}
