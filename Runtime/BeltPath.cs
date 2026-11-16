using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath
{
    /// <summary>
    /// The built belt line: a spline plus an arc-length lookup table. Everything that needs "the point N world
    /// units along the belt" (meshes, moving objects) goes through this class. Positions are in the local space of
    /// the object that owns the path.
    /// </summary>
    public sealed class BeltPath
    {
        private const int MinNodeCount = 2;
        private const int SamplesPerCurve = 48;
        private const int MinSampleCount = 2;
        private const float ForwardProbeDistance = 0.05f;

        private readonly Vector3[] points;
        private readonly float[] cumulative;

        private BeltPath(Spline spline, Vector3[] points, float[] cumulative)
        {
            Spline = spline;
            this.points = points;
            this.cumulative = cumulative;
            Length = cumulative[cumulative.Length - 1];
        }

        /// <summary>Total length in world units.</summary>
        public float Length { get; }

        public bool IsClosed => Spline.Closed;

        /// <summary>The smoothed spline (rounded corners applied) the path follows.</summary>
        public Spline Spline { get; }

        /// <summary>Dense polyline along the path, from start to end; closed paths repeat the first point last.</summary>
        public IReadOnlyList<Vector3> Points => points;

        /// <exception cref="ArgumentException">The data has fewer than two nodes.</exception>
        public static BeltPath Build(BeltPathData data, BeltShapeSettings shape)
        {
            if (data == null || data.Nodes.Count < MinNodeCount)
            {
                throw new ArgumentException($"A belt path needs at least {MinNodeCount} nodes.", nameof(data));
            }

            return FromSpline(BeltSplineBuilder.Build(data, shape));
        }

        /// <summary>Position at a distance along the path. Wraps on closed paths, clamps on open ones.</summary>
        public Vector3 Position(float distance)
        {
            distance = NormalizeDistance(distance);
            var index = FindSegment(distance);
            var span = cumulative[index + 1] - cumulative[index];
            var fraction = span > Mathf.Epsilon ? (distance - cumulative[index]) / span : 0f;
            return Vector3.Lerp(points[index], points[index + 1], fraction);
        }

        /// <summary>Unit travel direction at a distance along the path.</summary>
        public Vector3 Forward(float distance)
        {
            var delta = Position(distance + ForwardProbeDistance) - Position(distance - ForwardProbeDistance);
            return delta.sqrMagnitude > Mathf.Epsilon ? delta.normalized : Vector3.forward;
        }

        private static BeltPath FromSpline(Spline spline)
        {
            var curveCount = spline.Closed ? spline.Count : spline.Count - 1;
            var sampleCount = Mathf.Max(MinSampleCount, curveCount * SamplesPerCurve);
            var points = new Vector3[sampleCount + 1];
            var cumulative = new float[sampleCount + 1];
            for (var i = 0; i <= sampleCount; i++)
            {
                points[i] = spline.EvaluatePosition(i / (float)sampleCount);
                if (i > 0)
                {
                    cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);
                }
            }

            return new BeltPath(spline, points, cumulative);
        }

        private float NormalizeDistance(float distance)
        {
            return IsClosed ? Mathf.Repeat(distance, Length) : Mathf.Clamp(distance, 0f, Length);
        }

        private int FindSegment(float distance)
        {
            var index = Array.BinarySearch(cumulative, distance);
            if (index < 0)
            {
                index = ~index - 1;
            }

            return Mathf.Clamp(index, 0, cumulative.Length - 2);
        }
    }
}
