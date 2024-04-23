using TaleWorlds.Library;

namespace Alliance.Common.Extensions.Audio.Utilities
{
    /// <summary>
    /// Contains utility methods for audio-related calculations.
    /// </summary>
    public class AudioHelper
    {
        public static bool CanTargetHearSound(Vec3 emitterPos, Vec3 targetPos, float maxDistance = 30f)
        {
            // TODO : Add check for obstacles between the 2 positions
            return targetPos.Distance(emitterPos) < maxDistance;
        }

        /// <summary>
        /// Calculate the volume (from 0 to 1) of a sound based on the distance between the speaker and the listener.
        /// </summary>
        public static float CalculateVolume(Vec3 speakerPosition, Vec3 listenerPosition, float distanceCutoff)
        {
            // Calculate the distance between the speaker and the listener
            float distance = speakerPosition.Distance(listenerPosition);

            // Adjust volume based on distance
            float volume = 1.0f - MathF.Clamp(distance / distanceCutoff, 0.0f, 1.0f);

            return volume;
        }

        /// <summary>
        /// Pan determines the stereo balance of the sound based on the speaker's position relative to the listener.
        /// A pan value of -1.0 means the sound is fully to the left, 1.0 fully to the right, and 0.0 is centered.
        /// </summary>
        public static float CalculatePan(Vec3 speakerPosition, Vec3 listenerPosition, Mat3 listenerRotation)
        {
            // Calculate the vector from speaker to listener
            Vec3 speakerToListener = speakerPosition - listenerPosition;  // Correct the direction

            // Project the speaker-to-listener vector onto the horizontal plane defined by the listener's right and forward vectors
            Vec3 listenerRight = listenerRotation.s;  // Assuming 's' is the right vector in the rotation matrix

            // Normalize to ignore the influence of the distance (distance will affect volume, not panning)
            speakerToListener.Normalize();

            // Calculate the dot product to find how much of the speakerToListener is in the direction of the listener's right
            float pan = Vec3.DotProduct(speakerToListener, listenerRight);

            // The dot product gives us the cosine of the angle which is sufficient for panning calculation
            // No need for angle calculation, as the dot product directly gives the projection scale
            return MathF.Clamp(pan, -1.0f, 1.0f);
        }

        /// <summary>
        /// Normalizes an angle to the range [-π, π].
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            return (angle + MathF.PI) % (2 * MathF.PI) - MathF.PI;
        }

        private static float CalculateDistanceFactor(float distance)
        {
            return 0.5f + 0.5f / (1 + distance / 10);
        }
    }
}
