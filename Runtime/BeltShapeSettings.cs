using System;
using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>Settings that decide the geometric shape of the belt path.</summary>
    [Serializable]
    public class BeltShapeSettings
    {
        private const float DefaultCellSize = 0.5f;
        // Corner radius baked into the reference game's belt paths (WaterPipeRecipe fingerprint "5,2.5").
        private const float DefaultCornerRadiusCells = 2.5f;
        // Level data already lists the ground path, so nodes need no shift by default. The reference game lifts a
        // raised node by tilting the whole board plane, which a scene reproduces with a tilted parent of the BeltView.
        private const float DefaultElevationPlaneShift = 0f;

        [SerializeField] private float cellSize = DefaultCellSize;
        [SerializeField] private float cornerRadiusCells = DefaultCornerRadiusCells;
        [SerializeField] private float elevationPlaneShift = DefaultElevationPlaneShift;

        public BeltShapeSettings()
        {
        }

        public BeltShapeSettings(float cellSize, float cornerRadiusCells, float elevationPlaneShift = 0f)
        {
            this.cellSize = cellSize;
            this.cornerRadiusCells = cornerRadiusCells;
            this.elevationPlaneShift = elevationPlaneShift;
        }

        /// <summary>World units per grid cell.</summary>
        public float CellSize => cellSize;

        /// <summary>Radius of the arc that rounds right-angle corners, in cells.</summary>
        public float CornerRadiusCells => cornerRadiusCells;

        /// <summary>
        /// Grid y that the level data adds per cell of elevation (the data stores the position as seen on a tilted
        /// board plane). The ground position of a raised node is its grid y plus this value times its elevation.
        /// </summary>
        public float ElevationPlaneShift => elevationPlaneShift;
    }
}
