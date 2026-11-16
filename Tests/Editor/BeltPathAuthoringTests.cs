using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath.Tests
{
    public class BeltPathAuthoringTests
    {
        private const string SamplePath = "Packages/com.tool.beltpath/Tests/Editor/Data/figure_eight.json";

        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LoadPath_ThenToPathData_KeepsNodesAndClosedFlag()
        {
            var data = LoadSample();
            var authoring = CreateAuthoring();

            authoring.LoadPath(data);
            var result = authoring.ToPathData();

            Assert.That(result.Nodes.Count, Is.EqualTo(data.Nodes.Count));
            Assert.That(result.IsClosed, Is.EqualTo(data.IsClosed));
        }

        [Test]
        public void ToPathData_FollowsTheSplineClosedFlag()
        {
            var authoring = CreateAuthoring();
            authoring.LoadPath(LoadSample());

            host.GetComponent<SplineContainer>().Spline.Closed = false;

            Assert.That(authoring.ToPathData().IsClosed, Is.False);
        }

        [Test]
        public void ToPathData_FollowsKnotsAddedOrRemoved()
        {
            var authoring = CreateAuthoring();
            var data = LoadSample();
            authoring.LoadPath(data);

            host.GetComponent<SplineContainer>().Spline.RemoveAt(0);

            Assert.That(authoring.ToPathData().Nodes.Count, Is.EqualTo(data.Nodes.Count - 1));
        }

        [Test]
        public void ApplyPendingRebuild_BuildsThePathAfterASplineEdit()
        {
            var authoring = CreateAuthoring();
            authoring.LoadPath(LoadSample());
            var view = host.GetComponent<BeltView>();

            host.GetComponent<SplineContainer>().Spline.SetKnot(0, new BezierKnot(Vector3.zero));
            authoring.ApplyPendingRebuild();

            Assert.That(view.Path != null, Is.True);
            Assert.That(view.BuiltData.Nodes.Count, Is.EqualTo(authoring.ToPathData().Nodes.Count));
        }

        private BeltPathAuthoring CreateAuthoring()
        {
            // The view has no path file; the test only needs the components to exist.
            host = new GameObject("BeltPathAuthoringTest");
            return host.AddComponent<BeltPathAuthoring>();
        }

        private static BeltPathData LoadSample()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(SamplePath);
            Assert.That(file != null, Is.True, $"Missing {SamplePath}");
            return BeltPathData.FromJson(file.text);
        }
    }
}
