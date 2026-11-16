using System.Collections.Generic;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>One undo step: the belt plus the state text of every <see cref="IMapDataExtension"/>, in the same order.</summary>
    public readonly struct MapEditorSnapshot
    {
        public MapEditorSnapshot(MapSnapshot map, IReadOnlyList<string> extensionStates)
        {
            Map = map;
            ExtensionStates = extensionStates;
        }

        public MapSnapshot Map { get; }

        public IReadOnlyList<string> ExtensionStates { get; }
    }
}
