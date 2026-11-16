namespace Tool.BeltPath.MapEditor
{
    /// <summary>The default file format: a plain <see cref="BeltPathData"/>.</summary>
    public sealed class BeltPathFileFormat : IMapFileFormat
    {
        public BeltPathData Create(BeltPathData path)
        {
            return path;
        }

        public BeltPathData Parse(string json)
        {
            return BeltPathData.FromJson(json);
        }
    }
}
