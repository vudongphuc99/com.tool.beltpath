using NUnit.Framework;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class BeltMeshTests
    {
        private const float Tolerance = 1e-3f;

        private const string OpenLineJson =
            "{\"isClosed\":false,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":9,\"y\":0},{\"x\":18,\"y\":0}]}";

        private const string ClosedSquareJson =
            "{\"isClosed\":true,\"nodes\":[{\"x\":0,\"y\":0},{\"x\":10,\"y\":0},{\"x\":10,\"y\":10},{\"x\":0,\"y\":10}]}";

        private static readonly BeltShapeSettings Shape = new BeltShapeSettings(1f, 2f);
        private BeltMeshSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<BeltMeshSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ClosedBelt_WaterEndsOnAWholeNumberOfUvUnits()
        {
            var mesh = BeltSweepMeshBuilder.BuildWaterSurface(Build(ClosedSquareJson), Shape, settings);

            Assert.That(MaxV(mesh) % 1f, Is.EqualTo(0f).Within(Tolerance).Or.EqualTo(1f).Within(Tolerance));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void OpenBelt_WaterKeepsTheConfiguredUvLength()
        {
            var path = Build(OpenLineJson);
            var mesh = BeltSweepMeshBuilder.BuildWaterSurface(path, Shape, settings);

            var expected = path.Length / (settings.UvLengthCells * Shape.CellSize);

            Assert.That(MaxV(mesh), Is.EqualTo(expected).Within(Tolerance));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Channel_HasGeometryAndNormalsForEveryVertex()
        {
            var mesh = BeltSweepMeshBuilder.BuildChannel(Build(OpenLineJson), Shape, settings);

            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Shadow_SpansTheOuterWallsPlusSpread()
        {
            var mesh = BeltSweepMeshBuilder.BuildShadow(Build(OpenLineJson), Shape, settings);

            var expected = (settings.ChannelWidthCells * 0.5f + settings.WallThicknessCells
                + settings.ShadowSpreadCells) * Shape.CellSize;
            Assert.That(mesh.bounds.extents.z, Is.EqualTo(expected).Within(Tolerance));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Shadow_FollowsBeltElevation()
        {
            const string raisedJson = "{\"isClosed\":false,\"nodes\":[{\"x\":0,\"y\":0},"
                + "{\"x\":9,\"y\":0,\"elevation\":2},{\"x\":18,\"y\":0}]}";
            var path = Build(raisedJson);
            var mesh = BeltSweepMeshBuilder.BuildShadow(path, Shape, settings);

            var pathTop = 0f;
            foreach (var point in path.Points)
            {
                pathTop = Mathf.Max(pathTop, point.y);
            }

            Assert.That(pathTop, Is.GreaterThan(1f));
            Assert.That(mesh.bounds.max.y, Is.EqualTo(pathTop).Within(0.05f));
            Object.DestroyImmediate(mesh);
        }

        private static BeltPath Build(string json)
        {
            return BeltPath.Build(BeltPathData.FromJson(json), Shape);
        }

        private static float MaxV(Mesh mesh)
        {
            var max = 0f;
            foreach (var uv in mesh.uv)
            {
                max = Mathf.Max(max, uv.y);
            }

            return max;
        }
    }
}
