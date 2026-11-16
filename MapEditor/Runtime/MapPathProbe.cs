using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Where a new point would go: a spot on the belt, its elevation, and its place in the point list.</summary>
    public readonly struct MapInsertion
    {
        public MapInsertion(Vector2 ground, float elevation, int insertIndex)
        {
            Ground = ground;
            Elevation = elevation;
            InsertIndex = insertIndex;
        }

        public Vector2 Ground { get; }

        public float Elevation { get; }

        /// <summary>Index to give the new point; equal to the point count when it goes at the end.</summary>
        public int InsertIndex { get; }
    }

    /// <summary>
    /// Finds where a point can be inserted on the belt that is really drawn (the built path polyline), so the spot
    /// always lies on the visible line. The polyline holds (ground x, elevation, ground y) samples in cells.
    /// </summary>
    public static class MapPathProbe
    {
        private const float MinNodeGapCells = 0.25f;
        private const float MinSegmentLengthSqr = 1e-8f;

        /// <summary>Finds the spot on the path nearest to a ground point; false when it sits on an existing point.</summary>
        public static bool TryProbe(IReadOnlyList<Vector3> path, IReadOnlyList<MapNode> nodes, bool closed,
            Vector2 ground, out MapInsertion insertion)
        {
            insertion = default;
            if (path.Count < 2 || nodes.Count < 2)
            {
                return false;
            }

            var segment = FindNearestSegment(path, ground, out var point);
            var insertIndex = FindInsertIndex(AssignNodeSamples(path, nodes), segment);
            var pointGround = new Vector2(point.x, point.z);
            if (IsNearNeighbor(nodes, insertIndex, closed, pointGround))
            {
                return false;
            }

            insertion = new MapInsertion(pointGround, point.y, insertIndex);
            return true;
        }

        // Each point gets the polyline sample it stands closest to. The search only moves forward, so a path that
        // crosses itself cannot send a later point back to an earlier crossing.
        private static int[] AssignNodeSamples(IReadOnlyList<Vector3> path, IReadOnlyList<MapNode> nodes)
        {
            var samples = new int[nodes.Count];
            var start = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                samples[i] = NearestSampleFrom(path, start, nodes[i].Ground);
                start = samples[i];
            }

            return samples;
        }

        private static int NearestSampleFrom(IReadOnlyList<Vector3> path, int start, Vector2 ground)
        {
            var best = start;
            var bestSqr = float.MaxValue;
            for (var k = start; k < path.Count; k++)
            {
                var sqr = (new Vector2(path[k].x, path[k].z) - ground).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = k;
                }
            }

            return best;
        }

        // Returns the index of the polyline segment (sample j to j + 1) holding the closest spot.
        private static int FindNearestSegment(IReadOnlyList<Vector3> path, Vector2 ground, out Vector3 point)
        {
            var bestSegment = 0;
            var bestSqr = float.MaxValue;
            point = path[0];
            for (var j = 0; j < path.Count - 1; j++)
            {
                var candidate = ClosestOnSegment(path[j], path[j + 1], ground);
                var sqr = (new Vector2(candidate.x, candidate.z) - ground).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestSegment = j;
                    point = candidate;
                }
            }

            return bestSegment;
        }

        private static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector2 ground)
        {
            var start = new Vector2(a.x, a.z);
            var direction = new Vector2(b.x, b.z) - start;
            var lengthSqr = direction.sqrMagnitude;
            if (lengthSqr < MinSegmentLengthSqr)
            {
                return a;
            }

            var u = Mathf.Clamp01(Vector2.Dot(ground - start, direction) / lengthSqr);
            return Vector3.Lerp(a, b, u);
        }

        // The new point goes right after the last point whose sample is at or before the segment.
        private static int FindInsertIndex(int[] nodeSamples, int segment)
        {
            var index = 1;
            for (var i = 0; i < nodeSamples.Length; i++)
            {
                if (nodeSamples[i] <= segment)
                {
                    index = i + 1;
                }
            }

            return index;
        }

        private static bool IsNearNeighbor(IReadOnlyList<MapNode> nodes, int insertIndex, bool closed, Vector2 ground)
        {
            var count = nodes.Count;
            if (Vector2.Distance(nodes[insertIndex - 1].Ground, ground) < MinNodeGapCells)
            {
                return true;
            }

            var nextIndex = insertIndex < count ? insertIndex : (closed ? 0 : -1);
            return nextIndex >= 0 && Vector2.Distance(nodes[nextIndex].Ground, ground) < MinNodeGapCells;
        }
    }
}
