using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>
    /// The single place that converts level grid coordinates (x right, y down, elevation in cells)
    /// to Unity world space (x right, y up, z = -ground y).
    /// </summary>
    public static class BeltGridSpace
    {
        /// <summary>World position of a point. Raised points are moved back to their ground position first.</summary>
        public static Vector3 ToWorld(Vector2 grid, float elevationCells, BeltShapeSettings shape)
        {
            var groundY = grid.y + shape.ElevationPlaneShift * elevationCells;
            return new Vector3(grid.x, elevationCells, -groundY) * shape.CellSize;
        }

        /// <summary>Inverse of <see cref="ToWorld"/>: grid position and elevation of a world point.</summary>
        public static Vector2 ToGrid(Vector3 world, BeltShapeSettings shape, out float elevationCells)
        {
            var cells = world / shape.CellSize;
            elevationCells = cells.y;
            var groundY = -cells.z;
            return new Vector2(cells.x, groundY - shape.ElevationPlaneShift * elevationCells);
        }

        /// <summary>World vector for a grid-space offset. Offsets carry no elevation, so no plane shift applies.</summary>
        public static Vector3 ToWorldOffset(Vector2 gridOffset, BeltShapeSettings shape)
        {
            return new Vector3(gridOffset.x, 0f, -gridOffset.y) * shape.CellSize;
        }
    }
}
