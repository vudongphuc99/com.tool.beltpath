using System;
using System.Collections.Generic;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Selection and tool state of the editor window.</summary>
    public sealed class MapEditorState
    {
        public const int NoSelection = -1;

        private readonly List<int> selection = new List<int>();
        private MapEditMode mode = MapEditMode.Place;
        private bool placeEnabled = true;
        private bool handlesVisible;
        private bool insertHeld;
        private bool insertLatched;

        public event Action Changed;

        /// <summary>Indices of the selected points, ascending.</summary>
        public IReadOnlyList<int> SelectedIndices => selection;

        /// <summary>
        /// The selected point when exactly one is selected, otherwise <see cref="NoSelection"/>. Handles and single
        /// point edits work on this one.
        /// </summary>
        public int SelectedIndex
        {
            get => selection.Count == 1 ? selection[0] : NoSelection;
            set
            {
                if (value == NoSelection)
                {
                    ClearSelection();
                    return;
                }

                SelectOnly(value);
            }
        }

        /// <summary>Shows the Bezier handles of the selected point (top view and profile); off by default.</summary>
        public bool HandlesVisible
        {
            get => handlesVisible;
            set
            {
                if (handlesVisible == value)
                {
                    return;
                }

                handlesVisible = value;
                Changed?.Invoke();
            }
        }

        /// <summary>When on, a click on empty space adds a point (in Place mode); when off it only clears the selection.</summary>
        public bool PlaceEnabled
        {
            get => placeEnabled;
            set
            {
                if (placeEnabled == value)
                {
                    return;
                }

                placeEnabled = value;
                Changed?.Invoke();
            }
        }

        /// <summary>True while the insert key is held down.</summary>
        public bool InsertHeld
        {
            get => insertHeld;
            set
            {
                if (insertHeld == value)
                {
                    return;
                }

                insertHeld = value;
                Changed?.Invoke();
            }
        }

        /// <summary>True while the insert toggle button is on.</summary>
        public bool InsertLatched
        {
            get => insertLatched;
            set
            {
                if (insertLatched == value)
                {
                    return;
                }

                insertLatched = value;
                Changed?.Invoke();
            }
        }

        /// <summary>Clicks insert a point on the belt instead of adding one at the end.</summary>
        public bool IsInserting => insertHeld || insertLatched;

        public MapEditMode Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                {
                    return;
                }

                mode = value;
                Changed?.Invoke();
            }
        }

        public bool IsSelected(int index)
        {
            return selection.Contains(index);
        }

        public void SelectOnly(int index)
        {
            if (selection.Count == 1 && selection[0] == index)
            {
                return;
            }

            selection.Clear();
            selection.Add(index);
            Changed?.Invoke();
        }

        public void SelectMany(IEnumerable<int> indices)
        {
            var next = new List<int>(indices);
            next.Sort();
            if (IsSameSelection(next))
            {
                return;
            }

            selection.Clear();
            selection.AddRange(next);
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            if (selection.Count == 0)
            {
                return;
            }

            selection.Clear();
            Changed?.Invoke();
        }

        /// <summary>Drops selected indices that no longer exist after the points changed.</summary>
        public void PruneSelection(int pointCount)
        {
            if (selection.RemoveAll(index => index >= pointCount) > 0)
            {
                Changed?.Invoke();
            }
        }

        private bool IsSameSelection(List<int> other)
        {
            if (other.Count != selection.Count)
            {
                return false;
            }

            for (var i = 0; i < other.Count; i++)
            {
                if (other[i] != selection[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
