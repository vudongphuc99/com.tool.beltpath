using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath.Tests
{
    public class BeltNodeSplineConverterTests
    {
        private const float Tolerance = 1e-3f;
        private const string SamplePath = "Packages/com.tool.beltpath/Tests/Editor/Data/figure_eight.json";

        private static readonly BeltShapeSettings Shape = new BeltShapeSettings(0.5f, 2f, 0.6f);
        private static readonly BeltShapeSettings UnitShape = new BeltShapeSettings(1f, 2f, 0f);

        [Test]
        public void PopulateThenToNodes_ReturnsTheOriginalNodes()
        {
            var data = LoadSample();
            var spline = new Spline();

            BeltNodeSplineConverter.Populate(spline, data.Nodes, data.IsClosed, Shape);
            var nodes = BeltNodeSplineConverter.ToNodes(spline, Shape);

            Assert.That(spline.Closed, Is.EqualTo(data.IsClosed));
            Assert.That(nodes.Length, Is.EqualTo(data.Nodes.Count));
            for (var i = 0; i < nodes.Length; i++)
            {
                AssertSameNode(data.Nodes[i], nodes[i], i);
            }
        }

        [Test]
        public void ToNodes_RotatesHandlesByTheKnotRotation()
        {
            var spline = new Spline();
            var knot = new BezierKnot(new float3(1f, 0f, 2f), new float3(0f, 0f, -1f), new float3(0f, 0f, 1f),
                quaternion.RotateY(math.radians(90f)));
            spline.Add(knot, TangentMode.Broken);

            var node = BeltNodeSplineConverter.ToNodes(spline, UnitShape)[0];

            // A 90 degree turn about Y sends local +z to world +x.
            AssertVector(node.GridPosition, new Vector2(1f, -2f));
            AssertVector(node.TangentOutGrid, new Vector2(2f, -2f));
            AssertVector(node.TangentInGrid, new Vector2(0f, -2f));
        }

        [TestCase(TangentMode.Broken, true)]
        [TestCase(TangentMode.Continuous, true)]
        [TestCase(TangentMode.Mirrored, true)]
        [TestCase(TangentMode.AutoSmooth, false)]
        [TestCase(TangentMode.Linear, false)]
        public void ToNodes_TreatsOnlyHandleModesAsCustom(TangentMode mode, bool expectedCustom)
        {
            var spline = new Spline();
            spline.Add(new BezierKnot(new float3(1f, 0f, 1f), new float3(0f, 0f, -1f), new float3(0f, 0f, 1f)), mode);

            var node = BeltNodeSplineConverter.ToNodes(spline, UnitShape)[0];

            Assert.That(node.HasCustomTangent, Is.EqualTo(expectedCustom));
        }

        private static BeltPathData LoadSample()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(SamplePath);
            Assert.That(file != null, Is.True, $"Missing {SamplePath}");
            return BeltPathData.FromJson(file.text);
        }

        private static void AssertSameNode(BeltNodeData expected, BeltNodeData actual, int index)
        {
            var label = $"node {index}";
            AssertVector(actual.GridPosition, expected.GridPosition, label);
            Assert.That(actual.Elevation, Is.EqualTo(expected.Elevation).Within(Tolerance), label);
            Assert.That(actual.HasCustomTangent, Is.EqualTo(expected.HasCustomTangent), label);
            if (!expected.HasCustomTangent)
            {
                return;
            }

            AssertVector(actual.TangentInGrid, expected.TangentInGrid, label);
            AssertVector(actual.TangentOutGrid, expected.TangentOutGrid, label);
            Assert.That(actual.ElevTangentIn, Is.EqualTo(expected.ElevTangentIn).Within(Tolerance), label);
            Assert.That(actual.ElevTangentOut, Is.EqualTo(expected.ElevTangentOut).Within(Tolerance), label);
        }

        private static void AssertVector(Vector2 actual, Vector2 expected, string label = "")
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), label);
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), label);
        }
    }
}
