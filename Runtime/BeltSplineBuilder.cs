using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath
{
    /// <summary>
    /// Turns the nodes of a path into a Unity <see cref="Spline"/>.
    /// Nodes with custom tangents keep their authored Bezier handles, right-angle corners are rounded
    /// with a circular arc, every other node is auto-smoothed.
    /// </summary>
    public static class BeltSplineBuilder
    {
        private const float RightAngleDotTolerance = 0.09f;
        private const float QuarterArcHandleFactor = 0.5523f;
        private const float MinSegmentLengthSqr = 1e-6f;
        private const float CoincideTolerance = 1e-3f;
        private const float HalfSegment = 0.5f;

        private readonly struct PathContext
        {
            public PathContext(IReadOnlyList<BeltNodeData> nodes, bool[] corners, float[] cuts, bool closed,
                BeltShapeSettings settings)
            {
                Nodes = nodes;
                Corners = corners;
                Cuts = cuts;
                Closed = closed;
                Settings = settings;
            }

            public IReadOnlyList<BeltNodeData> Nodes { get; }
            public bool[] Corners { get; }

            /// <summary>Distance from each corner node to where its arc starts and ends (0 for non-corners).</summary>
            public float[] Cuts { get; }
            public bool Closed { get; }
            public BeltShapeSettings Settings { get; }
        }

        public static Spline Build(BeltPathData data, BeltShapeSettings settings)
        {
            var nodes = data.Nodes;
            var corners = FindCorners(nodes, data.IsClosed);
            var cuts = ComputeCuts(nodes, corners, data.IsClosed, settings.CornerRadiusCells);
            var context = new PathContext(nodes, corners, cuts, data.IsClosed, settings);

            var spline = new Spline(nodes.Count);
            for (var i = 0; i < nodes.Count; i++)
            {
                AddNode(spline, context, i);
            }

            spline.Closed = data.IsClosed;
            return spline;
        }

        private static void AddNode(Spline spline, in PathContext context, int index)
        {
            var node = context.Nodes[index];
            if (node.HasCustomTangent)
            {
                AddCustomKnot(spline, node, context.Settings);
                return;
            }

            if (context.Corners[index])
            {
                AddCornerKnots(spline, context, index);
                return;
            }

            // The data lists the point where a neighbouring arc starts as a node of its own; the arc already
            // passes through it, and a second knot there would make a zero-length curve with a loop in it.
            if (IsSwallowedByCornerCut(context, index))
            {
                return;
            }

            var position = BeltGridSpace.ToWorld(node.GridPosition, node.Elevation, context.Settings);
            spline.Add(position, TangentMode.AutoSmooth);
        }

        private static void AddCustomKnot(Spline spline, BeltNodeData node, BeltShapeSettings settings)
        {
            var position = BeltGridSpace.ToWorld(node.GridPosition, node.Elevation, settings);
            var handleIn = BeltGridSpace.ToWorld(node.TangentInGrid, node.ElevTangentIn, settings);
            var handleOut = BeltGridSpace.ToWorld(node.TangentOutGrid, node.ElevTangentOut, settings);
            spline.Add(new BezierKnot(position, handleIn - position, handleOut - position), TangentMode.Broken);
        }

        private static void AddCornerKnots(Spline spline, in PathContext context, int index)
        {
            TryGetNeighbors(index, context.Nodes.Count, context.Closed, out var prev, out var next);
            var corner = context.Nodes[index].GridPosition;
            var dirIn = (corner - context.Nodes[prev].GridPosition).normalized;
            var dirOut = (context.Nodes[next].GridPosition - corner).normalized;
            var cut = context.Cuts[index];
            var handle = cut * QuarterArcHandleFactor;
            var elevation = context.Nodes[index].Elevation;

            AddArcKnot(spline, corner - dirIn * cut, elevation, Vector2.zero, dirIn * handle, context.Settings);
            AddArcKnot(spline, corner + dirOut * cut, elevation, -dirOut * handle, Vector2.zero, context.Settings);
        }

        private static void AddArcKnot(Spline spline, Vector2 grid, float elevation,
            Vector2 handleInGrid, Vector2 handleOutGrid, BeltShapeSettings settings)
        {
            var position = BeltGridSpace.ToWorld(grid, elevation, settings);
            var handleIn = BeltGridSpace.ToWorldOffset(handleInGrid, settings);
            var handleOut = BeltGridSpace.ToWorldOffset(handleOutGrid, settings);
            spline.Add(new BezierKnot(position, handleIn, handleOut), TangentMode.Broken);
        }

        private static bool IsSwallowedByCornerCut(in PathContext context, int index)
        {
            return IsCutEndpoint(context, index, index - 1) || IsCutEndpoint(context, index, index + 1);
        }

        private static bool IsCutEndpoint(in PathContext context, int index, int neighborIndex)
        {
            var count = context.Nodes.Count;
            if (context.Closed)
            {
                neighborIndex = (neighborIndex + count) % count;
            }
            else if (neighborIndex < 0 || neighborIndex >= count)
            {
                return false;
            }

            if (!context.Corners[neighborIndex])
            {
                return false;
            }

            var distance = Vector2.Distance(context.Nodes[index].GridPosition, context.Nodes[neighborIndex].GridPosition);
            return Mathf.Abs(distance - context.Cuts[neighborIndex]) < CoincideTolerance;
        }

        // When the neighbour on a leg is a corner too, the two arcs share that leg, so each gets half of it.
        private static float[] ComputeCuts(IReadOnlyList<BeltNodeData> nodes, bool[] corners, bool closed, float radius)
        {
            var cuts = new float[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
            {
                if (!corners[i])
                {
                    continue;
                }

                TryGetNeighbors(i, nodes.Count, closed, out var prev, out var next);
                var lengthIn = Vector2.Distance(nodes[prev].GridPosition, nodes[i].GridPosition);
                var lengthOut = Vector2.Distance(nodes[i].GridPosition, nodes[next].GridPosition);
                var maxIn = corners[prev] ? lengthIn * HalfSegment : lengthIn;
                var maxOut = corners[next] ? lengthOut * HalfSegment : lengthOut;
                cuts[i] = Mathf.Min(radius, maxIn, maxOut);
            }

            return cuts;
        }

        private static bool[] FindCorners(IReadOnlyList<BeltNodeData> nodes, bool closed)
        {
            var corners = new bool[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].HasCustomTangent || !TryGetNeighbors(i, nodes.Count, closed, out var prev, out var next))
                {
                    continue;
                }

                corners[i] = IsRightAngle(nodes[prev].GridPosition, nodes[i].GridPosition, nodes[next].GridPosition);
            }

            return corners;
        }

        private static bool TryGetNeighbors(int index, int count, bool closed, out int prev, out int next)
        {
            if (closed)
            {
                prev = (index + count - 1) % count;
                next = (index + 1) % count;
                return count >= 3;
            }

            prev = index - 1;
            next = index + 1;
            return prev >= 0 && next < count;
        }

        private static bool IsRightAngle(Vector2 prev, Vector2 current, Vector2 next)
        {
            var incoming = current - prev;
            var outgoing = next - current;
            if (incoming.sqrMagnitude < MinSegmentLengthSqr || outgoing.sqrMagnitude < MinSegmentLengthSqr)
            {
                return false;
            }

            return Mathf.Abs(Vector2.Dot(incoming.normalized, outgoing.normalized)) < RightAngleDotTolerance;
        }
    }
}
