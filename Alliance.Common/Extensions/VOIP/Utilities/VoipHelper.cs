using TaleWorlds.Library;

namespace Alliance.Common.Extensions.VOIP.Utilities
{
    public class VoipHelper
    {
        public static bool CanTargetHearVoice(Vec3 emitterPos, Vec3 targetPos, float maxDistance = 30f)
        {
            // TODO : Add check for obstacles between the 2 positions
            return targetPos.Distance(emitterPos) < maxDistance;
        }

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
            Vec3 speakerToListener = listenerPosition - speakerPosition;

            // Invert the listener's rotation matrix to get the listener's forward vector in world space
            Mat3 invertedRotation = listenerRotation.Transpose(); // Note: Inversion for rotation matrices is equivalent to transpose
            Vec3 listenerForward = invertedRotation.TransformToParent(Vec3.Forward);
            // Project the speaker-to-listener vector onto the plane defined by the listener's forward vector
            Vec3 projectedHorizontal = speakerToListener - Vec3.DotProduct(speakerToListener, listenerForward) * listenerForward;
            Vec3 projectedVertical = speakerToListener - Vec3.DotProduct(speakerToListener, invertedRotation.u) * invertedRotation.u;

            // Calculate horizontal and vertical angles
            float horizontalAngle = MathF.Atan2(Vec3.DotProduct(projectedHorizontal, listenerRotation.s), Vec3.DotProduct(projectedHorizontal, listenerRotation.u));
            float verticalAngle = MathF.Atan2(Vec3.DotProduct(projectedVertical, invertedRotation.u), Vec3.DotProduct(projectedVertical, listenerForward));

            // Normalize angles to the range [-π, π]
            horizontalAngle = NormalizeAngle(horizontalAngle);
            verticalAngle = NormalizeAngle(verticalAngle);

            // Calculate panning based on horizontal angle and distance
            float distance = speakerToListener.Length;
            float pan = horizontalAngle / MathF.PI;
            pan *= CalculateDistanceFactor(distance);

            // Adjust panning based on vertical angle
            float verticalFactor = 1.0f - MathF.Abs(verticalAngle) / MathF.PI; // Reduces panning for sounds above/below
            pan *= verticalFactor;

            return MathF.Clamp(pan, -1.0f, 1.0f);
        }

        private static float NormalizeAngle(float angle)
        {
            return (angle + MathF.PI) % (2 * MathF.PI) - MathF.PI;
        }

        private static float CalculateDistanceFactor(float distance)
        {
            // Adjust this function based on how you want the distance to affect panning
            return 1.0f / (0.5f + distance / 5);
        }
    }
}
