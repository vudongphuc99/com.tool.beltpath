using System;
using System.Collections.Generic;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Undo and redo for a <see cref="MapDocument"/>. Every change of the document is one step, except inside a
    /// gesture (a mouse drag): all changes between <see cref="BeginGesture"/> and <see cref="EndGesture"/> are one
    /// step, and none is recorded when nothing changed. Each step also holds the state of the
    /// <see cref="IMapDataExtension"/>s; they report their own changes through <see cref="RecordChange"/>.
    /// </summary>
    public sealed class MapHistory
    {
        public const int MaxSteps = 100;

        private readonly MapDocument document;
        private readonly IReadOnlyList<IMapDataExtension> dataExtensions;
        private readonly List<MapEditorSnapshot> undoSteps = new List<MapEditorSnapshot>();
        private readonly List<MapEditorSnapshot> redoSteps = new List<MapEditorSnapshot>();

        // The map as it was after the last recorded change; it is what the next step goes back to.
        private MapEditorSnapshot current;
        private bool currentIsStale;
        private int gestureDepth;
        private bool gestureRecorded;
        private bool applying;

        public MapHistory(MapDocument document)
            : this(document, Array.Empty<IMapDataExtension>())
        {
        }

        public MapHistory(MapDocument document, IReadOnlyList<IMapDataExtension> dataExtensions)
        {
            this.document = document;
            this.dataExtensions = dataExtensions;
            current = CaptureAll();
            document.Changed += RecordChange;
        }

        /// <summary>Raised when the undo or redo availability may have changed.</summary>
        public event Action Changed;

        public bool CanUndo => undoSteps.Count > 0;

        public bool CanRedo => redoSteps.Count > 0;

        /// <summary>Stops listening to the document.</summary>
        public void Detach()
        {
            document.Changed -= RecordChange;
        }

        public void BeginGesture()
        {
            if (gestureDepth == 0)
            {
                gestureRecorded = false;
            }

            gestureDepth++;
        }

        public void EndGesture()
        {
            if (gestureDepth == 0)
            {
                return;
            }

            gestureDepth--;
            if (gestureDepth == 0)
            {
                RefreshCurrentIfStale();
            }
        }

        /// <summary>Runs an action whose changes count as a single step.</summary>
        public void RunAsOneStep(Action action)
        {
            BeginGesture();
            try
            {
                action();
            }
            finally
            {
                EndGesture();
            }
        }

        public bool Undo()
        {
            if (!CanUndo)
            {
                return false;
            }

            RefreshCurrentIfStale();
            var target = Pop(undoSteps);
            redoSteps.Add(current);
            Apply(target);
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo)
            {
                return false;
            }

            RefreshCurrentIfStale();
            var target = Pop(redoSteps);
            Push(undoSteps, current);
            Apply(target);
            return true;
        }

        /// <summary>
        /// Records the change that just happened as an undo step (or as part of the open gesture). The document calls
        /// it by itself; an extension calls it after changing its data.
        /// </summary>
        public void RecordChange()
        {
            if (applying)
            {
                return;
            }

            if (gestureDepth > 0 && gestureRecorded)
            {
                currentIsStale = true;
                return;
            }

            Push(undoSteps, current);
            redoSteps.Clear();
            if (gestureDepth > 0)
            {
                gestureRecorded = true;
                currentIsStale = true;
            }
            else
            {
                current = CaptureAll();
            }

            Changed?.Invoke();
        }

        private void Apply(MapEditorSnapshot target)
        {
            applying = true;
            try
            {
                // The extensions go first: restoring the document refreshes the editor, which must see their data.
                RestoreExtensions(target.ExtensionStates);
                document.Restore(target.Map);
            }
            finally
            {
                applying = false;
            }

            current = target;
            currentIsStale = false;
            Changed?.Invoke();
        }

        // During a drag the map changes on every pointer move; it is captured once, when the drag ends.
        private void RefreshCurrentIfStale()
        {
            if (currentIsStale)
            {
                current = CaptureAll();
                currentIsStale = false;
            }
        }

        private MapEditorSnapshot CaptureAll()
        {
            var states = new string[dataExtensions.Count];
            for (var i = 0; i < states.Length; i++)
            {
                states[i] = dataExtensions[i].CaptureState();
            }

            return new MapEditorSnapshot(document.Capture(), states);
        }

        private void RestoreExtensions(IReadOnlyList<string> states)
        {
            for (var i = 0; i < states.Count; i++)
            {
                dataExtensions[i].RestoreState(states[i]);
            }
        }

        private static void Push(List<MapEditorSnapshot> steps, MapEditorSnapshot snapshot)
        {
            steps.Add(snapshot);
            if (steps.Count > MaxSteps)
            {
                steps.RemoveAt(0);
            }
        }

        private static MapEditorSnapshot Pop(List<MapEditorSnapshot> steps)
        {
            var last = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);
            return last;
        }
    }
}
