using System;
using TaleWorlds.Library;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Utilities
{
    public static class MathHelper
    {
        /// <summary>
        /// Return the angle (in radian) between 2 vectors.
        /// Result is positive when angle goes left, negative when angle goes right.
        /// </summary>
        public static float AngleBetweenTwoVectors(Vec3 v1, Vec3 v2)
        {
            v1.Normalize();
            v2.Normalize();

            // Calculate the dot product
            float dotProduct = Vec3.DotProduct(v1, v2);

            // Calculate the angle in radians using the inverse cosine
            float angleInRadians = MathF.Acos(dotProduct);

            // Check if the angle is NaN (indicates invalid or undefined angle)
            if (float.IsNaN(angleInRadians))
            {
                // Return 0 or any default value as desired
                return 0f;
            }

            // Calculate the cross product
            Vec3 crossProduct = Vec3.CrossProduct(v1, v2);

            // Determine the turning direction
            if (crossProduct.z > 0)
            {
                return -angleInRadians; // Right
            }
            else
            {
                return angleInRadians; // Left
            }
        }

        public static float ToRadian(float degree)
        {
            return degree * (float)(Math.PI / 180);
        }

        public static float ToDegrees(float radian)
        {
            return radian * (float)(180 / Math.PI);
        }
    }
}
