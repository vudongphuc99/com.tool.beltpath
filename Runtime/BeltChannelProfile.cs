using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>
    /// Cross-section (right, up) of the channel: a U with round wall crests and filleted wall feet.
    /// The outline is closed and runs so its left side faces the water and the outside.
    /// </summary>
    public static class BeltChannelProfile
    {
        private const float Half = 0.5f;
        private const float MergeDistance = 1e-5f;
        private const int MinFilletSegments = 2;
        private const float QuarterTurn = Mathf.PI * 0.5f;

        /// <param name="halfWidth">Half of the water width; the inner wall faces sit at +/- this value.</param>
        /// <param name="wallHeight">Height of the wall crest above the floor.</param>
        /// <param name="thickness">Wall thickness; also the depth of the shell below the floor.</param>
        /// <param name="floorFillet">Radius of the curve where a wall meets the floor.</param>
        /// <param name="crestSegments">Segments of the half circle over each wall; fillets use half as many.</param>
        public static Vector2[] Build(float halfWidth, float wallHeight, float thickness, float floorFillet,
            int crestSegments)
        {
            var crestRadius = thickness * Half;
            var crestBase = Mathf.Max(0f, wallHeight - crestRadius);
            var fillet = Mathf.Clamp(floorFillet, 0f, crestBase);
            var crestCount = Mathf.Max(1, crestSegments);
            var filletCount = Mathf.Max(MinFilletSegments, crestCount / 2);
            var outerLeft = -halfWidth - thickness;
            var outerRight = halfWidth + thickness;

            var points = new List<Vector2>();
            AddPoint(points, new Vector2(outerLeft, -thickness));
            AddPoint(points, new Vector2(outerLeft, crestBase));
            AppendArc(points, new Vector2(-halfWidth - crestRadius, crestBase), crestRadius, Mathf.PI, 0f, crestCount);
            AddPoint(points, new Vector2(-halfWidth, fillet));
            AppendArc(points, new Vector2(-halfWidth + fillet, fillet), fillet, Mathf.PI, Mathf.PI + QuarterTurn,
                filletCount);
            AppendArc(points, new Vector2(halfWidth - fillet, fillet), fillet, Mathf.PI + QuarterTurn, 2f * Mathf.PI,
                filletCount);
            AddPoint(points, new Vector2(halfWidth, crestBase));
            AppendArc(points, new Vector2(halfWidth + crestRadius, crestBase), crestRadius, Mathf.PI, 0f, crestCount);
            AddPoint(points, new Vector2(outerRight, -thickness));
            AddPoint(points, new Vector2(outerLeft, -thickness));
            return points.ToArray();
        }

        // Radians, counter-clockwise from +right; start and end may run in either direction.
        private static void AppendArc(List<Vector2> points, Vector2 center, float radius, float startAngle,
            float endAngle, int segments)
        {
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.Lerp(startAngle, endAngle, (float)i / segments);
                AddPoint(points, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        // Dropping repeated points keeps every profile segment non-zero, so its normal is defined.
        private static void AddPoint(List<Vector2> points, Vector2 point)
        {
            if (points.Count > 0 && (points[points.Count - 1] - point).sqrMagnitude < MergeDistance * MergeDistance)
            {
                return;
            }

            points.Add(point);
        }
    }
}
