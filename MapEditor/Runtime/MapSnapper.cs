using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Rounds editor values to the grid unless free mode is on.</summary>
    public static class MapSnapper
    {
        public const float GroundStepCells = 1f;
        public const float ElevationStepCells = 0.5f;

        /// <summary>Factor of one scale step; scaling down uses its inverse so up then down is a round trip.</summary>
        public const float ScaleStep = 1.1f;

        /// <summary>Factor of one fine scale step (Shift held).</summary>
        public const float ScaleFineStep = 1.01f;

        public static Vector2 SnapGround(Vector2 ground, bool free)
        {
            if (free)
            {
                return ground;
            }

            return new Vector2(Snap(ground.x, GroundStepCells), Snap(ground.y, GroundStepCells));
        }

        public static float SnapElevation(float elevation, bool free)
        {
            return free ? elevation : Snap(elevation, ElevationStepCells);
        }

        private static float Snap(float value, float step)
        {
            return Mathf.Round(value / step) * step;
        }
    }
}
