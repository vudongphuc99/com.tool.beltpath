using NUnit.Framework;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class BeltPathDataTests
    {
        private const string LevelJsonWithExtraFields =
            "{\"levelId\":7,\"capacity\":12,\"isClosed\":true,\"pools\":[{\"id\":1,\"x\":1,\"y\":1}],"
            + "\"nodes\":[{\"x\":1,\"y\":2,\"elevation\":3},{\"x\":4,\"y\":5}]}";

        [Test]
        public void FromJson_IgnoresUnknownFields_AndReadsNodes()
        {
            var data = BeltPathData.FromJson(LevelJsonWithExtraFields);

            Assert.That(data.IsClosed, Is.True);
            Assert.That(data.Nodes.Count, Is.EqualTo(2));
            Assert.That(data.Nodes[0].GridPosition, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(data.Nodes[0].Elevation, Is.EqualTo(3f));
        }

        [Test]
        public void ToJson_ThenFromJson_KeepsCustomTangents()
        {
            var node = BeltNodeData.Custom(new Vector2(5f, 0f), 2f, new Vector2(4f, 0f), new Vector2(6f, 0f), 1f, 3f);
            var original = new BeltPathData(new[] { BeltNodeData.Smooth(Vector2.zero), node }, false);

            var copy = BeltPathData.FromJson(original.ToJson());

            Assert.That(copy.IsClosed, Is.False);
            Assert.That(copy.Nodes[1].HasCustomTangent, Is.True);
            Assert.That(copy.Nodes[1].TangentOutGrid, Is.EqualTo(new Vector2(6f, 0f)));
            Assert.That(copy.Nodes[1].ElevTangentOut, Is.EqualTo(3f));
        }

        [Test]
        public void FigureEightSample_BuildsAClosedPath()
        {
            var sample = LoadSample();

            var path = BeltPath.Build(BeltPathData.FromJson(sample.text), new BeltShapeSettings());

            Assert.That(path.IsClosed, Is.True);
            Assert.That(path.Length, Is.GreaterThan(0f));
        }

        private static TextAsset LoadSample()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.tool.beltpath/Tests/Editor/Data/figure_eight.json");
        }
    }
}
