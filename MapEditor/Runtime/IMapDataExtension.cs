namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Extra map data owned by an extension. The editor stores it in undo/redo steps and in the map file, and clears
    /// it on New. A <see cref="MapEditorExtension"/> opts in by implementing this interface.
    /// </summary>
    public interface IMapDataExtension
    {
        /// <summary>The current data as text; it is passed back to <see cref="RestoreState"/> by undo and redo.</summary>
        string CaptureState();

        void RestoreState(string state);

        /// <summary>Copies the data into the file made by the <see cref="IMapFileFormat"/> before it is saved.</summary>
        void WriteTo(BeltPathData file);

        /// <summary>Replaces the data with the content of a file parsed by the <see cref="IMapFileFormat"/>.</summary>
        void ReadFrom(BeltPathData file);

        /// <summary>Removes all data (New map).</summary>
        void ClearData();
    }
}
