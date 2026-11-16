using System;
using System.IO;
using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapExtensionTests
    {
        private string folder;

        [SetUp]
        public void CreateFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), $"beltpath-tests-{Guid.NewGuid():N}");
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void Undo_RestoresTheExtensionStateTogetherWithThePoints()
        {
            var document = new MapDocument();
            var extension = new CounterExtension();
            var history = new MapHistory(document, new IMapDataExtension[] { extension });

            history.RunAsOneStep(() =>
            {
                extension.Value = 7;
                document.Add(Vector2.zero, 0f);
            });
            history.Undo();

            Assert.That(extension.Value, Is.EqualTo(0));
            Assert.That(document.Nodes.Count, Is.EqualTo(0));

            history.Redo();

            Assert.That(extension.Value, Is.EqualTo(7));
            Assert.That(document.Nodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void RecordChange_IsOneStep_AndUndoOnlyRestoresTheExtension()
        {
            var document = new MapDocument();
            var extension = new CounterExtension();
            var history = new MapHistory(document, new IMapDataExtension[] { extension });
            document.Add(Vector2.zero, 0f);

            extension.Value = 3;
            history.RecordChange();
            history.Undo();

            Assert.That(extension.Value, Is.EqualTo(0));
            Assert.That(document.Nodes.Count, Is.EqualTo(1));
            Assert.That(history.CanUndo, Is.True);
            Assert.That(history.Redo(), Is.True);
            Assert.That(extension.Value, Is.EqualTo(3));
        }

        [Test]
        public void RecordChange_InsideAGesture_JoinsTheDocumentChanges()
        {
            var document = new MapDocument();
            var extension = new CounterExtension();
            var history = new MapHistory(document, new IMapDataExtension[] { extension });

            history.RunAsOneStep(() =>
            {
                document.Add(Vector2.zero, 0f);
                extension.Value = 1;
                history.RecordChange();
                extension.Value = 2;
                history.RecordChange();
            });
            history.Undo();

            Assert.That(history.CanUndo, Is.False);
            Assert.That(extension.Value, Is.EqualTo(0));
            Assert.That(document.Nodes.Count, Is.EqualTo(0));
        }

        [Test]
        public void Undo_RestoresTheExtension_BeforeTheDocumentRaisesChanged()
        {
            var document = new MapDocument();
            var extension = new CounterExtension();
            var history = new MapHistory(document, new IMapDataExtension[] { extension });
            extension.Value = 4;
            history.RecordChange();
            var valueSeenByRefresh = -1;
            document.Changed += () => valueSeenByRefresh = extension.Value;

            history.Undo();

            Assert.That(valueSeenByRefresh, Is.EqualTo(0));
        }

        [Test]
        public void FileFormat_KeepsTheExtraFieldsAndTheNodes()
        {
            var format = new TestLevelFormat();
            var path = new BeltPathData(new[] { BeltNodeData.Smooth(Vector2.zero), BeltNodeData.Smooth(Vector2.one) }, true);
            var file = (TestLevelFile)format.Create(path);
            file.Counter = 9;

            var loaded = (TestLevelFile)format.Parse(file.ToJson());

            Assert.That(loaded.Counter, Is.EqualTo(9));
            Assert.That(loaded.Nodes.Count, Is.EqualTo(2));
            Assert.That(loaded.IsClosed, Is.True);
        }

        [Test]
        public void FileStore_WithAFormat_LoadsTheSubclassItSaved()
        {
            var store = new MapFileStore(folder, new TestLevelFormat());
            var file = new TestLevelFile(new BeltPathData(new[] { BeltNodeData.Smooth(Vector2.zero) }, false));
            file.Counter = 5;

            var saved = store.TrySave("level", file, out _);
            var loaded = store.TryLoad("level", out var data, out _);

            Assert.That(saved, Is.True);
            Assert.That(loaded, Is.True);
            Assert.That(data, Is.TypeOf<TestLevelFile>());
            Assert.That(((TestLevelFile)data).Counter, Is.EqualTo(5));
        }

        [Test]
        public void ExtensionSet_WithoutAFormat_UsesTheDefaultOne()
        {
            var set = new MapEditorExtensionSet(Array.Empty<MapEditorExtension>());

            Assert.That(set.FileFormat, Is.TypeOf<BeltPathFileFormat>());
            Assert.That(set.DataExtensions, Is.Empty);
        }

        [Test]
        public void ResolveFileFormat_ReturnsTheOnlyFormat()
        {
            var format = new TestLevelFormat();

            Assert.That(MapEditorExtensionSet.ResolveFileFormat(new object[] { new object(), format }), Is.SameAs(format));
        }

        [Test]
        public void ResolveFileFormat_WithTwoFormats_Throws()
        {
            var formats = new object[] { new TestLevelFormat(), new TestLevelFormat() };

            Assert.Throws<InvalidOperationException>(() => MapEditorExtensionSet.ResolveFileFormat(formats));
        }

        private sealed class CounterExtension : IMapDataExtension
        {
            public int Value { get; set; }

            public string CaptureState()
            {
                return Value.ToString();
            }

            public void RestoreState(string state)
            {
                Value = int.Parse(state);
            }

            public void WriteTo(BeltPathData file)
            {
                ((TestLevelFile)file).Counter = Value;
            }

            public void ReadFrom(BeltPathData file)
            {
                Value = ((TestLevelFile)file).Counter;
            }

            public void ClearData()
            {
                Value = 0;
            }
        }

        private sealed class TestLevelFormat : IMapFileFormat
        {
            public BeltPathData Create(BeltPathData path)
            {
                return new TestLevelFile(path);
            }

            public BeltPathData Parse(string json)
            {
                return JsonUtility.FromJson<TestLevelFile>(json);
            }
        }

        [Serializable]
        private sealed class TestLevelFile : BeltPathData
        {
            [SerializeField] private int counter;

            // JsonUtility needs a default constructor.
            public TestLevelFile()
            {
            }

            public TestLevelFile(BeltPathData path)
                : base(path.Nodes, path.IsClosed)
            {
            }

            public int Counter
            {
                get => counter;
                set => counter = value;
            }
        }
    }
}
