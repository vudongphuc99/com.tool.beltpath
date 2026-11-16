using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>One belt waypoint. Field names match the level JSON.</summary>
    [Serializable]
    public class BeltNodeData
    {
        [SerializeField] private float x;
        [SerializeField] private float y;
        [SerializeField] private float elevation;
        [SerializeField] private bool hasCustomTangent;
        [SerializeField] private Vector2 tangentIn;
        [SerializeField] private Vector2 tangentOut;
        [SerializeField] private float elevTangentIn;
        [SerializeField] private float elevTangentOut;

        // JsonUtility needs a default constructor.
        public BeltNodeData()
        {
        }

        /// <summary>A node without handles: smoothed, or rounded when it forms a right angle.</summary>
        public static BeltNodeData Smooth(Vector2 grid, float elevation = 0f)
        {
            return new BeltNodeData { x = grid.x, y = grid.y, elevation = elevation };
        }

        /// <summary>A node with authored Bezier handles (absolute grid positions and elevations).</summary>
        public static BeltNodeData Custom(Vector2 grid, float elevation, Vector2 handleInGrid, Vector2 handleOutGrid,
            float handleInElevation, float handleOutElevation)
        {
            return new BeltNodeData
            {
                x = grid.x,
                y = grid.y,
                elevation = elevation,
                hasCustomTangent = true,
                tangentIn = handleInGrid,
                tangentOut = handleOutGrid,
                elevTangentIn = handleInElevation,
                elevTangentOut = handleOutElevation
            };
        }

        public Vector2 GridPosition => new Vector2(x, y);
        public float Elevation => elevation;
        public bool HasCustomTangent => hasCustomTangent;

        /// <summary>Absolute grid position of the incoming Bezier handle.</summary>
        public Vector2 TangentInGrid => tangentIn;

        /// <summary>Absolute grid position of the outgoing Bezier handle.</summary>
        public Vector2 TangentOutGrid => tangentOut;

        /// <summary>Absolute elevation (in cells) of the incoming handle.</summary>
        public float ElevTangentIn => elevTangentIn;

        /// <summary>Absolute elevation (in cells) of the outgoing handle.</summary>
        public float ElevTangentOut => elevTangentOut;
    }

    /// <summary>Input of the pack: the waypoints of one belt and whether it loops. Reads and writes JSON.</summary>
    [Serializable]
    public class BeltPathData
    {
        [SerializeField] private bool isClosed;
        [SerializeField] private BeltNodeData[] nodes;

        // JsonUtility needs a default constructor.
        public BeltPathData()
        {
        }

        public BeltPathData(IEnumerable<BeltNodeData> pathNodes, bool closed)
        {
            nodes = new List<BeltNodeData>(pathNodes).ToArray();
            isClosed = closed;
        }

        public bool IsClosed => isClosed;
        public IReadOnlyList<BeltNodeData> Nodes => nodes ?? Array.Empty<BeltNodeData>();

        /// <summary>Reads a path. Unknown JSON fields (level id, pools, ...) are ignored.</summary>
        public static BeltPathData FromJson(string json)
        {
            return JsonUtility.FromJson<BeltPathData>(json);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}
