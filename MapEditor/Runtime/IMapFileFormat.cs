namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// The file type of a map. A game returns a subclass of <see cref="BeltPathData"/> with extra serialized fields;
    /// the JSON stays flat. At most one extension may implement it.
    /// </summary>
    public interface IMapFileFormat
    {
        /// <summary>Wraps the belt of the editor into the file object to save. Extensions then add their data.</summary>
        BeltPathData Create(BeltPathData path);

        /// <summary>Reads a file; the result must be the same type that <see cref="Create"/> returns.</summary>
        BeltPathData Parse(string json);
    }
}
