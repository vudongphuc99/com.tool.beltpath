using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath
{
    /// <summary>
    /// Converts between level nodes and the editable spline (one knot per node) used for authoring.
    /// Knots with a handle tangent mode are custom nodes; auto-smooth and linear knots are plain nodes.
    /// </summary>
    public static class BeltNodeSplineConverter
    {
        public static void Populate(Spline spline, IReadOnlyList<BeltNodeData> nodes, bool closed,
            BeltShapeSettings shape)
        {
            spline.Clear();
            foreach (var node in nodes)
            {
                AddKnot(spline, node, shape);
            }

            spline.Closed = closed;
        }

        public static BeltNodeData[] ToNodes(Spline spline, BeltShapeSettings shape)
        {
            var nodes = new BeltNodeData[spline.Count];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = ToNode(spline, i, shape);
            }

            return nodes;
        }

        private static void AddKnot(Spline spline, BeltNodeData node, BeltShapeSettings shape)
        {
            var position = BeltGridSpace.ToWorld(node.GridPosition, node.Elevation, shape);
            if (!node.HasCustomTangent)
            {
                spline.Add(position, TangentMode.AutoSmooth);
                return;
            }

            var handleIn = BeltGridSpace.ToWorld(node.TangentInGrid, node.ElevTangentIn, shape);
            var handleOut = BeltGridSpace.ToWorld(node.TangentOutGrid, node.ElevTangentOut, shape);
            spline.Add(new BezierKnot(position, handleIn - position, handleOut - position), TangentMode.Broken);
        }

        private static BeltNodeData ToNode(Spline spline, int index, BeltShapeSettings shape)
        {
            var knot = spline[index];
            Vector3 position = knot.Position;
            var grid = BeltGridSpace.ToGrid(position, shape, out var elevation);
            if (!IsHandleMode(spline.GetTangentMode(index)))
            {
                return BeltNodeData.Smooth(grid, elevation);
            }

            // Knot tangents live in the knot's rotated local space, so rotate them before adding the position.
            Vector3 handleIn = position + (Vector3)math.rotate(knot.Rotation, knot.TangentIn);
            Vector3 handleOut = position + (Vector3)math.rotate(knot.Rotation, knot.TangentOut);
            var gridIn = BeltGridSpace.ToGrid(handleIn, shape, out var elevationIn);
            var gridOut = BeltGridSpace.ToGrid(handleOut, shape, out var elevationOut);
            return BeltNodeData.Custom(grid, elevation, gridIn, gridOut, elevationIn, elevationOut);
        }

        private static bool IsHandleMode(TangentMode mode)
        {
            return mode == TangentMode.Broken || mode == TangentMode.Continuous || mode == TangentMode.Mirrored;
        }
    }
}
