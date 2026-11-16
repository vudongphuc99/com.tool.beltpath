using System.IO;
using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapEditorModelTests
    {
        private const float Shift = 1.3f;
        private static readonly BeltShapeSettings Shape = new BeltShapeSettings(0.5f, 2f, Shift);

        [Test]
        public void ToPathData_ThenFromPathData_KeepsGroundAndElevation()
        {
            var document = new MapDocument();
            document.Add(new Vector2(2f, 3f), 0f);
            document.Add(new Vector2(8f, 6.5f), 2f);
            document.SetClosed(true);

            var copy = new MapDocument();
            copy.FromPathData(document.ToPathData(Shape), Shape);

            Assert.That(copy.IsClosed, Is.True);
            Assert.That(copy.Nodes.Count, Is.EqualTo(2));
            Assert.That(copy.Nodes[1].Ground.x, Is.EqualTo(8f).Within(1e-4f));
            Assert.That(copy.Nodes[1].Ground.y, Is.EqualTo(6.5f).Within(1e-4f));
            Assert.That(copy.Nodes[1].Elevation, Is.EqualTo(2f));
        }

        [Test]
        public void ToPathData_RaisedNode_ShiftsGridYBackFromGround()
        {
            var document = new MapDocument();
            document.Add(new Vector2(4f, 10f), 2f);

            var grid = document.ToPathData(Shape).Nodes[0].GridPosition;

            Assert.That(grid.y, Is.EqualTo(10f - Shift * 2f).Within(1e-4f));
        }

        [Test]
        public void Remove_And_Move_RaiseChanged()
        {
            var document = new MapDocument();
            var changes = 0;
            document.Changed += () => changes++;

            document.Add(Vector2.zero, 0f);
            document.Move(0, Vector2.one);
            document.Remove(0);

            Assert.That(changes, Is.EqualTo(3));
            Assert.That(document.Nodes.Count, Is.EqualTo(0));
        }

        [Test]
        public void SnapGround_RoundsToCell_UnlessFree()
        {
            Assert.That(MapSnapper.SnapGround(new Vector2(2.4f, 3.6f), false), Is.EqualTo(new Vector2(2f, 4f)));
            Assert.That(MapSnapper.SnapGround(new Vector2(2.4f, 3.6f), true), Is.EqualTo(new Vector2(2.4f, 3.6f)));
        }

        [Test]
        public void SnapElevation_RoundsToHalfCell_UnlessFree()
        {
            Assert.That(MapSnapper.SnapElevation(1.3f, false), Is.EqualTo(1.5f));
            Assert.That(MapSnapper.SnapElevation(1.3f, true), Is.EqualTo(1.3f));
        }

        [Test]
        public void ViewTransform_RoundTripsAfterPanAndZoom()
        {
            var view = new MapViewTransform { PanPixels = new Vector2(30f, -10f) };
            view.ZoomAt(new Vector2(100f, 80f), 1.5f);
            var cell = new Vector2(3.5f, -2f);

            var back = view.PixelToCell(view.CellToPixel(cell));

            Assert.That(Vector2.Distance(back, cell), Is.LessThan(1e-4f));
        }

        [Test]
        public void ViewTransform_ZoomAt_KeepsPivotCellFixed()
        {
            var view = new MapViewTransform();
            var pivot = new Vector2(120f, 60f);
            var before = view.PixelToCell(pivot);

            view.ZoomAt(pivot, 2f);

            Assert.That(Vector2.Distance(view.PixelToCell(pivot), before), Is.LessThan(1e-4f));
        }

        [Test]
        public void FileStore_SaveThenLoad_RoundTrips_AndLists()
        {
            var folder = Path.Combine(Path.GetTempPath(), "BeltMapStoreTest_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new MapFileStore(folder);
                var data = new BeltPathData(new[] { BeltNodeData.Smooth(Vector2.zero), BeltNodeData.Smooth(Vector2.one, 1f) }, true);

                Assert.That(store.TrySave("alpha", data, out _), Is.True);
                Assert.That(store.TryLoad("alpha", out var loaded, out _), Is.True);

                Assert.That(loaded.Nodes.Count, Is.EqualTo(2));
                Assert.That(loaded.IsClosed, Is.True);
                Assert.That(store.ListNames(), Is.EqualTo(new[] { "alpha" }));
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
        public void FileStore_RejectsBadName_AndMissingFile()
        {
            var store = new MapFileStore(Path.Combine(Path.GetTempPath(), "BeltMapStoreMissing"));

            Assert.That(store.TrySave("a/b", new BeltPathData(), out var error), Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(store.TryLoad("nope", out _, out _), Is.False);
        }
    }
}
