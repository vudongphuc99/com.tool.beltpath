using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Box selection: which points lie inside a rectangle of the top view.</summary>
    public static class MapSelection
    {
        /// <summary>Indices (ascending) of the points whose pixel lies in <paramref name="pixelRect"/>.</summary>
        public static List<int> IndicesInRect(IReadOnlyList<MapNode> nodes, MapViewTransform view, Rect pixelRect)
        {
            var result = new List<int>();
            for (var i = 0; i < nodes.Count; i++)
            {
                if (pixelRect.Contains(view.CellToPixel(nodes[i].Ground)))
                {
                    result.Add(i);
                }
            }

            return result;
        }
    }
}
