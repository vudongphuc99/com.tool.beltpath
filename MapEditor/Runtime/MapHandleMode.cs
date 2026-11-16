namespace Tool.BeltPath.MapEditor
{
    /// <summary>How the two handles of a point with authored handles follow each other.</summary>
    public enum MapHandleMode
    {
        /// <summary>The handles stay on one line, pointing opposite ways; each keeps its own length.</summary>
        Aligned,

        /// <summary>The handles are mirror images: opposite and equally long.</summary>
        Mirrored,

        /// <summary>Each handle moves on its own.</summary>
        Broken
    }

    /// <summary>The choices of the handle mode dropdown: automatic smoothing or one of the handle modes.</summary>
    public enum MapHandleOption
    {
        Auto,
        Aligned,
        Mirrored,
        Broken
    }
}
