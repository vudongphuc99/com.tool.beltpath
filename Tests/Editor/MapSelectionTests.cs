using System.IO;
using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapSelectionTests
    {
        private static MapDocument ThreePoints()
        {
            var document = new MapDocument();
            document.Add(new Vector2(0f, 0f), 1f);
            document.Add(new Vector2(5f, 0f), 2f);
            document.Add(new Vector2(10f, 0f), 9.8f);
            return document;
        }

        [Test]
        public void IndicesInRect_ReturnsOnlyPointsInside_WithPanAndZoom()
        {
            var view = new MapViewTransform { PanPixels = new Vector2(50f, 50f) };
            view.ZoomAt(new Vector2(50f, 50f), 2f);
            var document = ThreePoints();
            var first = view.CellToPixel(document.Nodes[0].Ground);
            var second = view.CellToPixel(document.Nodes[1].Ground);
            var rect = Rect.MinMaxRect(first.x - 5f, first.y - 5f, second.x + 5f, second.y + 5f);

            var inside = MapSelection.IndicesInRect(document.Nodes, view, rect);

            Assert.That(inside, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void SelectedIndex_IsOnlySetWhenExactlyOnePointIsSelected()
        {
            var state = new MapEditorState();
            Assert.That(state.SelectedIndex, Is.EqualTo(MapEditorState.NoSelection));

            state.SelectOnly(2);
            Assert.That(state.SelectedIndex, Is.EqualTo(2));

            state.SelectMany(new[] { 2, 0 });
            Assert.That(state.SelectedIndex, Is.EqualTo(MapEditorState.NoSelection));
            Assert.That(state.SelectedIndices, Is.EqualTo(new[] { 0, 2 }));
            Assert.That(state.IsSelected(0), Is.True);
            Assert.That(state.IsSelected(1), Is.False);
        }

        [Test]
        public void Selection_RaisesChangedOnlyWhenItChanges()
        {
            var state = new MapEditorState();
            var changes = 0;
            state.Changed += () => changes++;

            state.SelectMany(new[] { 1, 3 });
            state.SelectMany(new[] { 3, 1 });
            state.ClearSelection();
            state.ClearSelection();

            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void HandlesVisible_IsOffByDefault_AndRaisesChangedOnlyOnChange()
        {
            var state = new MapEditorState();
            var changes = 0;
            state.Changed += () => changes++;
            Assert.That(state.HandlesVisible, Is.False);

            state.HandlesVisible = true;
            state.HandlesVisible = true;
            state.HandlesVisible = false;

            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void SetPosition_ChangesGroundAndElevationAtOnce_KeepingHandles()
        {
            var document = ThreePoints();
            document.SetHandles(1, new MapHandles(Vector3.left, Vector3.right));
            var changes = 0;
            document.Changed += () => changes++;

            document.SetPosition(1, new Vector2(7f, -3f), 12f);

            Assert.That(document.Nodes[1].Ground, Is.EqualTo(new Vector2(7f, -3f)));
            Assert.That(document.Nodes[1].Elevation, Is.EqualTo(MapDocument.MaxElevationCells));
            Assert.That(document.Nodes[1].HasHandles, Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void PruneSelection_DropsIndicesBeyondTheCount()
        {
            var state = new MapEditorState();
            state.SelectMany(new[] { 0, 4, 5 });

            state.PruneSelection(5);

            Assert.That(state.SelectedIndices, Is.EqualTo(new[] { 0, 4 }));
        }

        [Test]
        public void RemoveRange_RemovesEveryIndexWithOneChange()
        {
            var document = ThreePoints();
            var changes = 0;
            document.Changed += () => changes++;

            document.RemoveRange(new[] { 2, 0 });

            Assert.That(document.Nodes.Count, Is.EqualTo(1));
            Assert.That(document.Nodes[0].Ground, Is.EqualTo(new Vector2(5f, 0f)));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void MoveMany_ShiftsEveryPointFromItsStart_AndKeepsHandles()
        {
            var document = ThreePoints();
            document.SetHandles(1, new MapHandles(Vector3.left, Vector3.right));
            var starts = new[] { document.Nodes[0].Ground, document.Nodes[1].Ground };

            document.MoveMany(new[] { 0, 1 }, starts, new Vector2(2f, 3f));
            document.MoveMany(new[] { 0, 1 }, starts, new Vector2(1f, 1f));

            Assert.That(document.Nodes[0].Ground, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(document.Nodes[1].Ground, Is.EqualTo(new Vector2(6f, 1f)));
            Assert.That(document.Nodes[1].HasHandles, Is.True);
            Assert.That(document.Nodes[2].Ground, Is.EqualTo(new Vector2(10f, 0f)));
        }

        [Test]
        public void ElevateMany_ClampsEachPointToTheValidRange()
        {
            var document = ThreePoints();
            var starts = new[] { 1f, 9.8f };

            document.ElevateMany(new[] { 0, 2 }, starts, 1f);

            Assert.That(document.Nodes[0].Elevation, Is.EqualTo(2f));
            Assert.That(document.Nodes[2].Elevation, Is.EqualTo(MapDocument.MaxElevationCells));
        }

        [Test]
        public void ScaleMany_ScalesGroundAroundTheBoundingBoxCenter_AndLeavesOthersAlone()
        {
            var document = ThreePoints();

            document.ScaleMany(new[] { 0, 1 }, 2f, false);

            Assert.That(document.Nodes[0].Ground, Is.EqualTo(new Vector2(-2.5f, 0f)));
            Assert.That(document.Nodes[1].Ground, Is.EqualTo(new Vector2(7.5f, 0f)));
            Assert.That(document.Nodes[2].Ground, Is.EqualTo(new Vector2(10f, 0f)));
            Assert.That(document.Nodes[0].Elevation, Is.EqualTo(1f));
        }

        [Test]
        public void ScaleMany_UpThenDown_IsARoundTrip()
        {
            var document = ThreePoints();

            document.ScaleMany(new[] { 0, 1, 2 }, MapSnapper.ScaleStep, false);
            document.ScaleMany(new[] { 0, 1, 2 }, 1f / MapSnapper.ScaleStep, false);

            Assert.That(Vector2.Distance(document.Nodes[0].Ground, new Vector2(0f, 0f)), Is.LessThan(1e-4f));
            Assert.That(Vector2.Distance(document.Nodes[2].Ground, new Vector2(10f, 0f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void ScaleMany_ScalesGroundHandlesWithThePoints()
        {
            var document = ThreePoints();
            document.SetHandles(0, new MapHandles(new Vector3(-1f, 0.5f, 0f), new Vector3(1f, 0.5f, 2f)));

            document.ScaleMany(new[] { 0, 1 }, 3f, false);

            Assert.That(document.Nodes[0].Handles.Out, Is.EqualTo(new Vector3(3f, 0.5f, 6f)));
            Assert.That(document.Nodes[0].Handles.In, Is.EqualTo(new Vector3(-3f, 0.5f, 0f)));
            Assert.That(document.Nodes[1].HasHandles, Is.False);
        }

        [Test]
        public void ScaleMany_ElevationMode_ScalesAroundTheMiddle_KeepingGround()
        {
            var document = new MapDocument();
            document.Add(new Vector2(0f, 0f), 2f);
            document.Add(new Vector2(5f, 0f), 4f);
            document.Add(new Vector2(10f, 0f), 6f);

            document.ScaleMany(new[] { 0, 1, 2 }, 1.5f, true);

            Assert.That(document.Nodes[0].Elevation, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(document.Nodes[1].Elevation, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(document.Nodes[2].Elevation, Is.EqualTo(7f).Within(1e-4f));
            Assert.That(document.Nodes[1].Ground, Is.EqualTo(new Vector2(5f, 0f)));
        }

        [Test]
        public void ScaleMany_ElevationMode_ClampsToTheValidRange()
        {
            var document = ThreePoints();

            document.ScaleMany(new[] { 0, 2 }, 2f, true);

            Assert.That(document.Nodes[0].Elevation, Is.EqualTo(0f));
            Assert.That(document.Nodes[2].Elevation, Is.EqualTo(MapDocument.MaxElevationCells));
        }

        [Test]
        public void ScaleMany_NeedsTwoPointsAndAPositiveFactor()
        {
            var document = ThreePoints();
            var changes = 0;
            document.Changed += () => changes++;

            document.ScaleMany(new[] { 1 }, 2f, false);
            document.ScaleMany(new[] { 0, 1 }, 0f, false);
            document.ScaleMany(new[] { 0, 1 }, 2f, false);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void ImageStore_ListsOnlyImageFiles()
        {
            var folder = Path.Combine(Path.GetTempPath(), "BeltImageStoreTest_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new MapImageStore(folder);
                store.EnsureFolder();
                File.WriteAllBytes(Path.Combine(folder, "b.PNG"), new byte[1]);
                File.WriteAllBytes(Path.Combine(folder, "a.jpg"), new byte[1]);
                File.WriteAllText(Path.Combine(folder, "notes.txt"), "x");

                Assert.That(store.ListNames(), Is.EqualTo(new[] { "a.jpg", "b.PNG" }));
                Assert.That(store.TryLoad("notes.txt", out _, out var error), Is.False);
                Assert.That(error, Is.Not.Null);
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [Test]
        public void ImageStore_LoadsAPng()
        {
            var folder = Path.Combine(Path.GetTempPath(), "BeltImageStoreLoad_" + System.Guid.NewGuid().ToString("N"));
            Texture2D loaded = null;
            try
            {
                var source = new Texture2D(4, 2);
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "tiny.png"), ImageConversion.EncodeToPNG(source));
                Object.DestroyImmediate(source);

                Assert.That(new MapImageStore(folder).TryLoad("tiny.png", out loaded, out _), Is.True);

                Assert.That(loaded.width, Is.EqualTo(4));
                Assert.That(loaded.height, Is.EqualTo(2));
            }
            finally
            {
                if (loaded != null)
                {
                    Object.DestroyImmediate(loaded);
                }

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
    }
}
