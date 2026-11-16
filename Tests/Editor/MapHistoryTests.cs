using NUnit.Framework;
using Tool.BeltPath.MapEditor;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class MapHistoryTests
    {
        private static MapDocument Document(out MapHistory history)
        {
            var document = new MapDocument();
            history = new MapHistory(document);
            return document;
        }

        [Test]
        public void Undo_RestoresEachStep_AndRedoReplaysIt()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);
            document.Add(Vector2.one, 0f);

            Assert.That(history.Undo(), Is.True);
            Assert.That(document.Nodes.Count, Is.EqualTo(1));
            Assert.That(history.Undo(), Is.True);
            Assert.That(document.Nodes.Count, Is.EqualTo(0));
            Assert.That(history.CanUndo, Is.False);

            Assert.That(history.Redo(), Is.True);
            Assert.That(history.Redo(), Is.True);
            Assert.That(document.Nodes.Count, Is.EqualTo(2));
            Assert.That(history.CanRedo, Is.False);
        }

        [Test]
        public void ANewChange_ClearsTheRedoSteps()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);
            history.Undo();
            Assert.That(history.CanRedo, Is.True);

            document.Add(Vector2.one, 0f);

            Assert.That(history.CanRedo, Is.False);
        }

        [Test]
        public void AGesture_IsOneStep_AndKeepsTheFinalState()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);

            history.BeginGesture();
            document.Move(0, new Vector2(1f, 0f));
            document.Move(0, new Vector2(2f, 0f));
            document.Move(0, new Vector2(3f, 0f));
            history.EndGesture();

            history.Undo();
            Assert.That(document.Nodes[0].Ground, Is.EqualTo(Vector2.zero));
            history.Redo();
            Assert.That(document.Nodes[0].Ground, Is.EqualTo(new Vector2(3f, 0f)));
        }

        [Test]
        public void AGestureWithoutChanges_RecordsNothing()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);
            history.Undo();
            history.Redo();

            history.BeginGesture();
            history.EndGesture();

            Assert.That(history.CanUndo, Is.True);
            Assert.That(history.CanRedo, Is.False);
            Assert.That(document.Nodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void RunAsOneStep_GroupsSeveralChanges()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);
            document.Add(Vector2.one, 0f);

            history.RunAsOneStep(() =>
            {
                document.SetHandleMode(0, MapHandleMode.Broken);
                document.SetHandleMode(1, MapHandleMode.Broken);
            });
            history.Undo();

            Assert.That(document.Nodes[0].HasHandles, Is.False);
            Assert.That(document.Nodes[1].HasHandles, Is.False);
            Assert.That(document.Nodes.Count, Is.EqualTo(2));
        }

        [Test]
        public void Restoring_DoesNotRecordAStepOfItsOwn()
        {
            var document = Document(out var history);
            document.Add(Vector2.zero, 0f);

            history.Undo();

            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.CanRedo, Is.True);
        }

        [Test]
        public void History_KeepsOnlyTheLastSteps()
        {
            var document = Document(out var history);
            for (var i = 0; i < MapHistory.MaxSteps + 20; i++)
            {
                document.Add(new Vector2(i, 0f), 0f);
            }

            var undone = 0;
            while (history.Undo())
            {
                undone++;
            }

            Assert.That(undone, Is.EqualTo(MapHistory.MaxSteps));
            Assert.That(document.Nodes.Count, Is.EqualTo(20));
        }

        [Test]
        public void Undo_AlsoRestoresTheClosedFlag()
        {
            var document = Document(out var history);
            document.SetClosed(true);

            history.Undo();

            Assert.That(document.IsClosed, Is.False);
        }

        [Test]
        public void Detach_StopsRecording()
        {
            var document = Document(out var history);
            history.Detach();

            document.Add(Vector2.zero, 0f);

            Assert.That(history.CanUndo, Is.False);
        }
    }
}
