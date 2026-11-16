using System;
using System.Collections.Generic;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Undo and redo for a <see cref="MapDocument"/>. Every change of the document is one step, except inside a
    /// gesture (a mouse drag): all changes between <see cref="BeginGesture"/> and <see cref="EndGesture"/> are one
    /// step, and none is recorded when nothing changed.
    /// </summary>
    public sealed class MapHistory
    {
        public const int MaxSteps = 100;

        private readonly MapDocument document;
        private readonly List<MapSnapshot> undoSteps = new List<MapSnapshot>();
        private readonly List<MapSnapshot> redoSteps = new List<MapSnapshot>();

        // The map as it was after the last recorded change; it is what the next step goes back to.
        private MapSnapshot current;
        private bool currentIsStale;
        private int gestureDepth;
        private bool gestureRecorded;
        private bool applying;

        public MapHistory(MapDocument document)
        {
            this.document = document;
            current = document.Capture();
            document.Changed += HandleDocumentChanged;
        }

        /// <summary>Raised when the undo or redo availability may have changed.</summary>
        public event Action Changed;

        public bool CanUndo => undoSteps.Count > 0;

        public bool CanRedo => redoSteps.Count > 0;

        /// <summary>Stops listening to the document.</summary>
        public void Detach()
        {
            document.Changed -= HandleDocumentChanged;
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

        private void HandleDocumentChanged()
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
                current = document.Capture();
            }

            Changed?.Invoke();
        }

        private void Apply(MapSnapshot target)
        {
            applying = true;
            try
            {
                document.Restore(target);
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
                current = document.Capture();
                currentIsStale = false;
            }
        }

        private static void Push(List<MapSnapshot> steps, MapSnapshot snapshot)
        {
            steps.Add(snapshot);
            if (steps.Count > MaxSteps)
            {
                steps.RemoveAt(0);
            }
        }

        private static MapSnapshot Pop(List<MapSnapshot> steps)
        {
            var last = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);
            return last;
        }
    }
}
