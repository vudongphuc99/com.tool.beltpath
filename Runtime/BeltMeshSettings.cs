using System;
using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>Dimensions (in grid cells) of the generated channel and water meshes.</summary>
    [CreateAssetMenu(menuName = "Tool/BeltPath/Mesh Settings", fileName = "BeltMeshSettings")]
    public sealed class BeltMeshSettings : ScriptableObject
    {
        // Defaults are the values of the reference game's WaterPipeRenderer prefab (water 2.3, wall 0.5, height 0.5,
        // water surface 0.18 above the floor).
        [SerializeField] private float channelWidthCells = 2.3f;
        [SerializeField] private float wallHeightCells = 0.5f;
        [SerializeField] private float wallThicknessCells = 0.5f;
        [SerializeField] private float floorFilletCells = 0.12f;
        [SerializeField] private int wallArcSegments = 6;
        [SerializeField] private float waterDepthCells = 0.18f;
        [SerializeField] private float sampleStepCells = 0.1f;
        [SerializeField] private float uvLengthCells = 9.39f;

        /// <summary>Raised in the editor after a value was edited in the inspector.</summary>
        public event Action Changed;

        public float ChannelWidthCells => channelWidthCells;
        public float WallHeightCells => wallHeightCells;
        public float WallThicknessCells => wallThicknessCells;

        /// <summary>Radius of the curve where a wall meets the channel floor.</summary>
        public float FloorFilletCells => floorFilletCells;

        /// <summary>Segments of the half circle that rounds the top of each wall.</summary>
        public int WallArcSegments => wallArcSegments;
        public float WaterDepthCells => waterDepthCells;

        /// <summary>Distance between two mesh rings along the path.</summary>
        public float SampleStepCells => sampleStepCells;

        /// <summary>Path length that maps to one unit of UV.v on generated meshes.</summary>
        public float UvLengthCells => uvLengthCells;

        private void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
