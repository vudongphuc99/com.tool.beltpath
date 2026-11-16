using System.Collections.Generic;
using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapPathProbeTests
    {
        private const float Tolerance = 1e-3f;

        private static MapNode[] Nodes(params Vector2[] points)
        {
            var nodes = new MapNode[points.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new MapNode(points[i], 0f);
            }

            return nodes;
        }

        // Samples every cell along the corners, elevation rising by 'rise' over the whole length.
        private static List<Vector3> Path(float rise, params Vector2[] corners)
        {
            var total = 0f;
            for (var i = 1; i < corners.Length; i++)
            {
                total += Vector2.Distance(corners[i - 1], corners[i]);
            }

            var samples = new List<Vector3>();
            var walked = 0f;
            for (var i = 1; i < corners.Length; i++)
            {
                var length = Vector2.Distance(corners[i - 1], corners[i]);
                for (var step = 0; step < (int)length; step++)
                {
                    var point = Vector2.Lerp(corners[i - 1], corners[i], step / length);
                    samples.Add(new Vector3(point.x, rise * (walked + step) / total, point.y));
                }

                walked += length;
            }

            samples.Add(new Vector3(corners[corners.Length - 1].x, rise, corners[corners.Length - 1].y));
            return samples;
        }

        [Test]
        public void Probe_LShape_PicksTheLegAndStaysOnTheLine()
        {
            var corners = new[] { Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f) };
            var path = Path(0f, corners);
            var nodes = Nodes(corners);

            Assert.That(MapPathProbe.TryProbe(path, nodes, false, new Vector2(5f, 0.7f), out var first), Is.True);
            Assert.That(first.InsertIndex, Is.EqualTo(1));
            Assert.That(first.Ground.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(first.Ground.x, Is.EqualTo(5f).Within(Tolerance));

            Assert.That(MapPathProbe.TryProbe(path, nodes, false, new Vector2(10.4f, 6f), out var second), Is.True);
            Assert.That(second.InsertIndex, Is.EqualTo(2));
            Assert.That(second.Ground, Is.EqualTo(new Vector2(10f, 6f)));
        }

        [Test]
        public void Probe_ClosedBelt_ClosingLegInsertsAtTheEnd()
        {
            var corners = new[]
            {
                Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f), new Vector2(0f, 10f), Vector2.zero
            };
            var path = Path(0f, corners);
            var nodes = Nodes(corners[0], corners[1], corners[2], corners[3]);

            Assert.That(MapPathProbe.TryProbe(path, nodes, true, new Vector2(-0.4f, 5f), out var insertion), Is.True);

            Assert.That(insertion.InsertIndex, Is.EqualTo(4));
            Assert.That(insertion.Ground.x, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Probe_InterpolatesElevationAlongThePath()
        {
            var corners = new[] { Vector2.zero, new Vector2(10f, 0f) };
            var path = Path(4f, corners);

            Assert.That(MapPathProbe.TryProbe(path, Nodes(corners), false, new Vector2(5f, 1f), out var insertion), Is.True);

            Assert.That(insertion.Elevation, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Probe_RoundedCorner_StillAssignsTheRightLeg()
        {
            // The drawn path cuts the corner (10, 0): it leaves the first leg at x = 8 and joins the second at y = 2.
            var path = new List<Vector3>();
            for (var x = 0; x <= 8; x++)
            {
                path.Add(new Vector3(x, 0f, 0f));
            }

            path.Add(new Vector3(9.4f, 0f, 0.6f));
            for (var y = 2; y <= 10; y++)
            {
                path.Add(new Vector3(10f, 0f, y));
            }

            var nodes = Nodes(Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f));

            Assert.That(MapPathProbe.TryProbe(path, nodes, false, new Vector2(4f, 0.5f), out var firstLeg), Is.True);
            Assert.That(firstLeg.InsertIndex, Is.EqualTo(1));
            Assert.That(MapPathProbe.TryProbe(path, nodes, false, new Vector2(10.3f, 7f), out var secondLeg), Is.True);
            Assert.That(secondLeg.InsertIndex, Is.EqualTo(2));
        }

        [Test]
        public void Probe_RejectsASpotOnAnExistingPoint()
        {
            var corners = new[] { Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f) };

            var found = MapPathProbe.TryProbe(Path(0f, corners), Nodes(corners), false, new Vector2(10.05f, 0.1f), out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void Probe_WithoutAPath_ReturnsFalse()
        {
            var nodes = Nodes(Vector2.zero, Vector2.one);

            Assert.That(MapPathProbe.TryProbe(new List<Vector3>(), nodes, false, Vector2.zero, out _), Is.False);
        }
    }
}
