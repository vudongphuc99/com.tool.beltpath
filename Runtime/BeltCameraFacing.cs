using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>
    /// Pose of an object that stands on the tilted duck plane of the reference game and faces along the belt.
    /// The game keeps its camera flat and tilts the plane its ducks stand on by 45 degrees about the screen X axis;
    /// the duck's up axis is the normal of that plane, so it leans toward the viewer and its face stays visible.
    /// </summary>
    public static class BeltCameraFacing
    {
        /// <summary>Tilt of the reference game's duck plane, in degrees.</summary>
        public const float DefaultTiltDegrees = 45f;

        private const float MinHorizontalSqrLength = 1e-8f;

        /// <summary>
        /// Rotation whose forward axis follows <paramref name="pathForward"/> as seen on the screen (the XZ part)
        /// and whose up axis is tilted toward the viewer. Height changes of the path are ignored.
        /// </summary>
        /// <returns>False when the direction has no horizontal part; the rotation is then identity.</returns>
        public static bool TryGetRotation(Vector3 pathForward, float tiltDegrees, out Quaternion rotation)
        {
            var horizontal = new Vector3(pathForward.x, 0f, pathForward.z);
            if (horizontal.sqrMagnitude < MinHorizontalSqrLength)
            {
                rotation = Quaternion.identity;
                return false;
            }

            rotation = Quaternion.Euler(tiltDegrees, 0f, 0f) * Quaternion.LookRotation(horizontal, Vector3.up);
            return true;
        }
    }
}
