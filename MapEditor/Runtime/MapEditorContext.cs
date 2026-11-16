using System;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>What an extension gets when it is attached: the parts of the editor it may read and change.</summary>
    public sealed class MapEditorContext
    {
        public MapEditorContext(MapDocument document, MapEditorState state, MapHistory history, MapEditorPanel panel,
            TopViewElement topView, BeltView beltView)
        {
            Document = document;
            State = state;
            History = history;
            Panel = panel;
            TopView = topView;
            BeltView = beltView;
        }

        /// <summary>Raised after an extension reported a change of its data; the editor refreshes itself.</summary>
        public event Action DataChanged;

        public MapDocument Document { get; }

        public MapEditorState State { get; }

        public MapHistory History { get; }

        public MapEditorPanel Panel { get; }

        public TopViewElement TopView { get; }

        public BeltView BeltView { get; }

        public void SetStatus(string text)
        {
            Panel.SetStatus(text);
        }

        /// <summary>
        /// Call after changing the data of an <see cref="IMapDataExtension"/>: makes one undo step (or joins the open
        /// gesture) and refreshes the editor. Changes of the belt itself do not need it.
        /// </summary>
        public void NotifyDataChanged()
        {
            History.RecordChange();
            DataChanged?.Invoke();
        }
    }
}
