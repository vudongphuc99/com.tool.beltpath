namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// A click tool for the top view. While it is <see cref="MapEditorState.ActiveTool"/>, the left and right buttons
    /// go to it instead of editing the belt; the middle button (pan) and the wheel (zoom) stay with the editor.
    /// <see cref="PointerMove"/> is called only while a button is held.
    /// </summary>
    public interface IMapTopViewTool
    {
        void PointerDown(MapPointer pointer);

        void PointerMove(MapPointer pointer);

        void PointerUp(MapPointer pointer);
    }
}
