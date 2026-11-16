using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// One editor point: ground position (x, z in cells), elevation (y in cells) and optional Bezier handles.
    /// A point without handles is smoothed automatically.
    /// </summary>
    public readonly struct MapNode
    {
        public MapNode(Vector2 ground, float elevation)
            : this(ground, elevation, false, default, MapHandleMode.Aligned)
        {
        }

        private MapNode(Vector2 ground, float elevation, bool hasHandles, MapHandles handles, MapHandleMode handleMode)
        {
            Ground = ground;
            Elevation = elevation;
            HasHandles = hasHandles;
            Handles = handles;
            HandleMode = handleMode;
        }

        /// <summary>Where the point stands on the ground plane, in cells (x right, y = ground z).</summary>
        public Vector2 Ground { get; }

        /// <summary>Height above the ground, in cells.</summary>
        public float Elevation { get; }

        /// <summary>True when the handles were authored; false means the curve is smoothed automatically.</summary>
        public bool HasHandles { get; }

        public MapHandles Handles { get; }

        /// <summary>How the two handles follow each other; only used while <see cref="HasHandles"/> is true.</summary>
        public MapHandleMode HandleMode { get; }

        /// <summary>The point as (ground x, elevation, ground y): the space the handle offsets live in.</summary>
        public Vector3 Position => new Vector3(Ground.x, Elevation, Ground.y);

        public MapNode WithGround(Vector2 ground)
        {
            return new MapNode(ground, Elevation, HasHandles, Handles, HandleMode);
        }

        public MapNode WithElevation(float elevation)
        {
            return new MapNode(Ground, elevation, HasHandles, Handles, HandleMode);
        }

        public MapNode WithHandles(MapHandles handles)
        {
            return new MapNode(Ground, Elevation, true, handles, HandleMode);
        }

        public MapNode WithHandleMode(MapHandleMode handleMode)
        {
            return new MapNode(Ground, Elevation, HasHandles, Handles, handleMode);
        }

        public MapNode WithoutHandles()
        {
            return new MapNode(Ground, Elevation, false, default, MapHandleMode.Aligned);
        }
    }
}
