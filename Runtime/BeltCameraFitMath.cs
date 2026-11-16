using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>Camera framing result: the screen-plane point to center on and the orthographic size.</summary>
    public readonly struct BeltCameraFit
    {
        public BeltCameraFit(Vector2 center, float orthoSize)
        {
            Center = center;
            OrthoSize = orthoSize;
        }

        public Vector2 Center { get; }

        public float OrthoSize { get; }
    }

    /// <summary>
    /// Pure math for framing the belt with a top-down orthographic camera. On screen a world point lands at
    /// horizontal <c>x</c> and vertical <c>z</c>.
    /// </summary>
    public static class BeltCameraFitMath
    {
        private const float Half = 0.5f;

        /// <summary>The screen-plane rectangle (x, z) covered by a world-space box.</summary>
        public static Rect ProjectToScreenPlane(Bounds world)
        {
            var min = world.min;
            var max = world.max;
            return Rect.MinMaxRect(min.x, min.z, max.x, max.z);
        }

        /// <summary>
        /// The smallest orthographic frame that shows <paramref name="content"/> plus <paramref name="padding"/> on
        /// every side. <paramref name="aspect"/> is width / height; the size never goes below <paramref name="minSize"/>.
        /// </summary>
        public static BeltCameraFit Solve(Rect content, float aspect, float padding, float minSize)
        {
            if (aspect <= 0f)
            {
                return new BeltCameraFit(content.center, minSize);
            }

            var sizeByHeight = (content.height + 2f * padding) * Half;
            var sizeByWidth = (content.width + 2f * padding) * Half / aspect;
            return new BeltCameraFit(content.center, Mathf.Max(minSize, sizeByHeight, sizeByWidth));
        }
    }
}
