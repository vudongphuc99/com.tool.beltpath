using System.Collections.Generic;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>An immutable copy of the map: its points and whether the belt loops.</summary>
    public sealed class MapSnapshot
    {
        private readonly MapNode[] nodes;

        /// <summary>Takes ownership of <paramref name="nodes"/>; the caller must not change the array afterwards.</summary>
        public MapSnapshot(MapNode[] nodes, bool isClosed)
        {
            this.nodes = nodes;
            IsClosed = isClosed;
        }

        public IReadOnlyList<MapNode> Nodes => nodes;

        public bool IsClosed { get; }
    }
}
