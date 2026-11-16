using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapCurveTests
    {
        private const float Shift = 1.3f;
        private const float Tolerance = 1e-4f;
        private static readonly BeltShapeSettings Shape = new BeltShapeSettings(0.5f, 2f, Shift);

        private static MapNode[] Line(params Vector2[] points)
        {
            var nodes = new MapNode[points.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new MapNode(points[i], 0f);
            }

            return nodes;
        }

        [Test]
        public void Handles_RoundTripThroughPathData_WithPlaneShift()
        {
            var document = new MapDocument();
            document.Add(new Vector2(4f, 10f), 2f);
            document.Add(new Vector2(12f, 10f), 0f);
            var handles = new MapHandles(new Vector3(-1f, -0.5f, -2f), new Vector3(1f, 0.5f, 2f));
            document.SetHandles(0, handles);

            var data = document.ToPathData(Shape);
            var node = data.Nodes[0];

            Assert.That(node.HasCustomTangent, Is.True);
            Assert.That(node.ElevTangentOut, Is.EqualTo(2.5f).Within(Tolerance));
            Assert.That(node.TangentOutGrid.x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(node.TangentOutGrid.y, Is.EqualTo(12f - Shift * 2.5f).Within(Tolerance));
            Assert.That(data.Nodes[1].HasCustomTangent, Is.False);

            var copy = new MapDocument();
            copy.FromPathData(data, Shape);

            Assert.That(copy.Nodes[0].HasHandles, Is.True);
            Assert.That(Vector3.Distance(copy.Nodes[0].Handles.In, handles.In), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(copy.Nodes[0].Handles.Out, handles.Out), Is.LessThan(Tolerance));
            Assert.That(copy.Nodes[1].HasHandles, Is.False);
        }

        [Test]
        public void Insert_PutsPointAtIndex_AndMoveKeepsHandles()
        {
            var document = new MapDocument();
            document.Add(Vector2.zero, 0f);
            document.Add(new Vector2(10f, 0f), 0f);
            document.SetHandles(0, new MapHandles(Vector3.left, Vector3.right));

            document.Insert(1, new Vector2(5f, 0f), 1f);
            document.Move(0, Vector2.one);

            Assert.That(document.Nodes.Count, Is.EqualTo(3));
            Assert.That(document.Nodes[1].Ground, Is.EqualTo(new Vector2(5f, 0f)));
            Assert.That(document.Nodes[0].HasHandles, Is.True);
            Assert.That(document.Nodes[2].Ground, Is.EqualTo(new Vector2(10f, 0f)));
        }

        [Test]
        public void ClearHandles_ReturnsPointToAuto()
        {
            var document = new MapDocument();
            document.Add(Vector2.zero, 0f);
            document.SetHandles(0, new MapHandles(Vector3.left, Vector3.right));

            document.ClearHandles(0);

            Assert.That(document.Nodes[0].HasHandles, Is.False);
        }

        [Test]
        public void AutoHandles_OnStraightLine_AreOppositeAndAlongTheLine()
        {
            var nodes = Line(Vector2.zero, new Vector2(5f, 0f), new Vector2(10f, 0f));

            var handles = MapCurveMath.AutoHandles(nodes, 1, false);

            Assert.That(handles.Out.x, Is.GreaterThan(0f));
            Assert.That(handles.Out.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(handles.Out.z, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(Vector3.Distance(handles.In, -handles.Out), Is.LessThan(Tolerance));
        }

        [Test]
        public void GroundDrag_Mirrored_MirrorsTheOtherHandle()
        {
            var handles = new MapHandles(new Vector3(-2f, -1f, 0f), new Vector3(3f, 1f, 0f));

            var result = MapCurveMath.ApplyGroundDrag(handles, true, new Vector2(0f, 6f), MapHandleMode.Mirrored);

            Assert.That(result.Out, Is.EqualTo(new Vector3(0f, 1f, 6f)));
            Assert.That(result.In, Is.EqualTo(new Vector3(0f, -1f, -6f)));
        }

        [Test]
        public void ElevationDrag_Mirrored_GivesTheOtherHandleTheOppositeElevation()
        {
            var handles = new MapHandles(new Vector3(-2f, -1f, 0f), new Vector3(4f, 2f, 0f));

            var result = MapCurveMath.ApplyElevationDrag(handles, true, 3f, MapHandleMode.Mirrored);

            Assert.That(result.In, Is.EqualTo(new Vector3(-2f, -3f, 0f)));
        }

        [Test]
        public void ApplyOffset_FollowsTheMode()
        {
            var handles = new MapHandles(new Vector3(-2f, 0f, 0f), new Vector3(4f, 0f, 0f));
            var offset = new Vector3(0f, 3f, 4f);

            var mirrored = MapCurveMath.ApplyOffset(handles, true, offset, MapHandleMode.Mirrored);
            var aligned = MapCurveMath.ApplyOffset(handles, true, offset, MapHandleMode.Aligned);
            var broken = MapCurveMath.ApplyOffset(handles, true, offset, MapHandleMode.Broken);

            Assert.That(mirrored.In, Is.EqualTo(-offset));
            Assert.That(aligned.Out, Is.EqualTo(offset));
            Assert.That(Vector3.Distance(aligned.In, -offset.normalized * 2f), Is.LessThan(Tolerance));
            Assert.That(broken.In, Is.EqualTo(handles.In));
        }

        [Test]
        public void ApplyLength_KeepsTheDirection_AndMirroredChangesBoth()
        {
            var handles = new MapHandles(new Vector3(-2f, 0f, 0f), new Vector3(0f, 3f, 4f));

            var aligned = MapCurveMath.ApplyLength(handles, true, 10f, MapHandleMode.Aligned);
            var mirrored = MapCurveMath.ApplyLength(handles, true, 10f, MapHandleMode.Mirrored);

            Assert.That(aligned.Out.magnitude, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(Vector3.Distance(aligned.Out.normalized, handles.Out.normalized), Is.LessThan(Tolerance));
            Assert.That(aligned.In, Is.EqualTo(handles.In));
            Assert.That(Vector3.Distance(mirrored.In, -mirrored.Out), Is.LessThan(Tolerance));
        }

        [Test]
        public void ApplyLength_OfAZeroLengthHandle_ChangesNothing()
        {
            var handles = new MapHandles(Vector3.left, Vector3.zero);

            var result = MapCurveMath.ApplyLength(handles, true, 5f, MapHandleMode.Aligned);

            Assert.That(result.Out, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void EnforceMode_KeepsOut_AndFitsIn()
        {
            var handles = new MapHandles(new Vector3(0f, 2f, 0f), new Vector3(3f, 0f, 0f));

            var mirrored = MapCurveMath.EnforceMode(handles, MapHandleMode.Mirrored);
            var aligned = MapCurveMath.EnforceMode(handles, MapHandleMode.Aligned);
            var broken = MapCurveMath.EnforceMode(handles, MapHandleMode.Broken);

            Assert.That(mirrored.In, Is.EqualTo(new Vector3(-3f, 0f, 0f)));
            Assert.That(Vector3.Distance(aligned.In, new Vector3(-2f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(broken.In, Is.EqualTo(handles.In));
        }

        [Test]
        public void InferMode_ReadsTheModeFromTheHandles()
        {
            Assert.That(MapCurveMath.InferMode(new MapHandles(new Vector3(-1f, 0.5f, 2f), new Vector3(1f, -0.5f, -2f))),
                Is.EqualTo(MapHandleMode.Mirrored));
            Assert.That(MapCurveMath.InferMode(new MapHandles(new Vector3(-2f, 0f, 0f), new Vector3(5f, 0f, 0f))),
                Is.EqualTo(MapHandleMode.Aligned));
            Assert.That(MapCurveMath.InferMode(new MapHandles(new Vector3(0f, 0f, -2f), new Vector3(5f, 0f, 0f))),
                Is.EqualTo(MapHandleMode.Broken));
        }

        [Test]
        public void SetHandleMode_FromAuto_MakesCustomHandlesInThatMode()
        {
            var document = new MapDocument();
            document.Add(Vector2.zero, 0f);
            document.Add(new Vector2(5f, 0f), 0f);
            document.Add(new Vector2(10f, 5f), 0f);

            document.SetHandleMode(1, MapHandleMode.Mirrored);

            var node = document.Nodes[1];
            Assert.That(node.HasHandles, Is.True);
            Assert.That(node.HandleMode, Is.EqualTo(MapHandleMode.Mirrored));
            Assert.That(Vector3.Distance(node.Handles.In, -node.Handles.Out), Is.LessThan(Tolerance));
        }

        [Test]
        public void ClearHandles_ResetsTheModeToAligned()
        {
            var document = new MapDocument();
            document.Add(Vector2.zero, 0f);
            document.SetHandleMode(0, MapHandleMode.Broken);

            document.ClearHandles(0);

            Assert.That(document.Nodes[0].HandleMode, Is.EqualTo(MapHandleMode.Aligned));
        }

        [Test]
        public void FromPathData_InfersTheHandleMode()
        {
            var document = new MapDocument();
            document.Add(new Vector2(4f, 10f), 0f);
            document.Add(new Vector2(12f, 10f), 0f);
            document.SetHandles(0, new MapHandles(new Vector3(-1f, 0f, -2f), new Vector3(1f, 0f, 2f)));

            var copy = new MapDocument();
            copy.FromPathData(document.ToPathData(Shape), Shape);

            Assert.That(copy.Nodes[0].HandleMode, Is.EqualTo(MapHandleMode.Mirrored));
        }

        [Test]
        public void IsHandleUsed_OpenBeltEnds_HaveOneHandle()
        {
            Assert.That(MapCurveMath.IsHandleUsed(3, 0, false, false), Is.False);
            Assert.That(MapCurveMath.IsHandleUsed(3, 0, true, false), Is.True);
            Assert.That(MapCurveMath.IsHandleUsed(3, 2, true, false), Is.False);
            Assert.That(MapCurveMath.IsHandleUsed(3, 0, false, true), Is.True);
        }

        [Test]
        public void GroundDrag_Continuous_KeepsOtherHandleOppositeWithItsOwnLength()
        {
            var handles = new MapHandles(new Vector3(-2f, 0f, 0f), new Vector3(3f, 0f, 0f));

            var result = MapCurveMath.ApplyGroundDrag(handles, true, new Vector2(0f, 6f), MapHandleMode.Aligned);

            Assert.That(result.Out, Is.EqualTo(new Vector3(0f, 0f, 6f)));
            Assert.That(Vector3.Distance(result.In, new Vector3(0f, 0f, -2f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void GroundDrag_Broken_ChangesOnlyTheDraggedHandle()
        {
            var handles = new MapHandles(new Vector3(-2f, 0f, 0f), new Vector3(3f, 0f, 0f));

            var result = MapCurveMath.ApplyGroundDrag(handles, true, new Vector2(0f, 6f), MapHandleMode.Broken);

            Assert.That(result.In, Is.EqualTo(handles.In));
        }

        [Test]
        public void ElevationDrag_Continuous_GivesTheOtherHandleTheOppositeSlope()
        {
            var handles = new MapHandles(new Vector3(-2f, -1f, 0f), new Vector3(4f, 2f, 0f));

            var result = MapCurveMath.ApplyElevationDrag(handles, true, 4f, MapHandleMode.Aligned);

            Assert.That(result.Out.y, Is.EqualTo(4f));
            Assert.That(result.In.y, Is.EqualTo(-2f).Within(Tolerance));
        }

        [Test]
        public void ElevationDrag_Broken_ChangesOnlyTheDraggedHandle()
        {
            var handles = new MapHandles(new Vector3(-2f, -1f, 0f), new Vector3(4f, 2f, 0f));

            var result = MapCurveMath.ApplyElevationDrag(handles, false, -3f, MapHandleMode.Broken);

            Assert.That(result.In.y, Is.EqualTo(-3f));
            Assert.That(result.Out, Is.EqualTo(handles.Out));
        }
    }
}
