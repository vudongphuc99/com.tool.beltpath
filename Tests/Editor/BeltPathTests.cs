using System;
using NUnit.Framework;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class BeltPathTests
    {
        private const float Tolerance = 0.05f;
        private const float CornerLengthTolerance = 0.1f;
        private const float GameMatchTolerance = 0.02f;

        private const string OpenLineJson =
            "{\"isClosed\":false,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0},{\"x\":10,\"y\":0}]}";

        private const string ClosedSquareJson =
            "{\"isClosed\":true,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":10,\"y\":0},{\"x\":10,\"y\":10},{\"x\":0,\"y\":10}]}";

        private const string TinySquareJson =
            "{\"isClosed\":true,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":3,\"y\":0},{\"x\":3,\"y\":3},{\"x\":0,\"y\":3}]}";

        private const string RaisedNodeJson =
            "{\"isClosed\":false,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0,\"elevation\":4},{\"x\":10,\"y\":0}]}";

        private static readonly BeltShapeSettings UnitCells = new BeltShapeSettings(1f, 2f);

        [Test]
        public void OpenPath_HasStraightLength_AndClampsBeyondTheEnds()
        {
            var path = Build(OpenLineJson);

            Assert.That(path.IsClosed, Is.False);
            Assert.That(path.Length, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(path.Position(5f).x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(path.Position(99f).x, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(path.Position(-3f).x, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void OpenPath_ForwardPointsAlongTheTravelDirection()
        {
            Assert.That(Build(OpenLineJson).Forward(5f).x, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ClosedPath_WrapsAroundAfterOneLap()
        {
            var path = Build(ClosedSquareJson);

            var start = path.Position(1f);
            var oneLapLater = path.Position(1f + path.Length);

            Assert.That(Vector3.Distance(start, oneLapLater), Is.LessThan(Tolerance));
        }

        [Test]
        public void ClosedSquare_RoundsCornersWithQuarterArcs()
        {
            // 4 sides of (10 - 2*2) plus 4 quarter arcs of radius 2.
            const float expected = 4f * 6f + 4f * Mathf.PI;
            var path = Build(ClosedSquareJson);

            Assert.That(path.Spline.Closed, Is.True);
            Assert.That(path.Spline.Count, Is.EqualTo(8));
            Assert.That(path.Length, Is.EqualTo(expected).Within(CornerLengthTolerance));
        }

        [Test]
        public void ShortSides_ClampCornerRadiusToHalfTheSide()
        {
            // Radius clamps to 1.5, so the four arcs form a full circle.
            const float expected = 2f * Mathf.PI * 1.5f;

            Assert.That(Build(TinySquareJson).Length, Is.EqualTo(expected).Within(CornerLengthTolerance));
        }

        [Test]
        public void NodeElevation_BecomesWorldHeight()
        {
            var path = Build(RaisedNodeJson);

            Assert.That(path.Position(path.Length * 0.5f).y, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Points_StartAtTheFirstNodeAndEndAtTheLast()
        {
            var path = Build(OpenLineJson);

            Assert.That(path.Points[0].x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(path.Points[path.Points.Count - 1].x, Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void DefaultSettings_RoundCornersLikeTheReferenceGame()
        {
            // Level 1 of the reference game: its baked path starts at grid (0, 14.5), the corner node (0, 17) pulled
            // back by the game's corner radius of 2.5 cells, and its arc ends at (2.5, 17).
            const string level1Json = "{\"isClosed\":true,\"nodes\":[{\"x\":0,\"y\":17},{\"x\":16,\"y\":17}," +
                                      "{\"x\":16,\"y\":1},{\"x\":0,\"y\":1}]}";
            var shape = new BeltShapeSettings();
            var path = BeltPath.Build(BeltPathData.FromJson(level1Json), shape);

            AssertPathPassesThrough(path, BeltGridSpace.ToWorld(new Vector2(0f, 14.5f), 0f, shape));
            AssertPathPassesThrough(path, BeltGridSpace.ToWorld(new Vector2(2.5f, 17f), 0f, shape));
        }

        [Test]
        public void Build_WithFewerThanTwoNodes_Throws()
        {
            var data = BeltPathData.FromJson("{\"isClosed\":false,\"nodes\":[{\"x\":0,\"y\":0}]}");

            Assert.Throws<ArgumentException>(() => BeltPath.Build(data, UnitCells));
        }

        private static BeltPath Build(string json)
        {
            return BeltPath.Build(BeltPathData.FromJson(json), UnitCells);
        }

        private static void AssertPathPassesThrough(BeltPath path, Vector3 expected)
        {
            var nearest = float.MaxValue;
            foreach (var point in path.Points)
            {
                nearest = Mathf.Min(nearest, Vector3.Distance(point, expected));
            }

            Assert.That(nearest, Is.LessThan(GameMatchTolerance), $"path never comes near {expected}");
        }
    }
}
