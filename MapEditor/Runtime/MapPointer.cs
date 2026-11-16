using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>A mouse event on the top view, handed to the active <see cref="IMapTopViewTool"/>.</summary>
    public readonly struct MapPointer
    {
        public MapPointer(Vector2 pixel, Vector2 cell, int button, bool shift, bool alt)
        {
            Pixel = pixel;
            Cell = cell;
            Button = button;
            Shift = shift;
            Alt = alt;
        }

        /// <summary>Position in pixels of the top view.</summary>
        public Vector2 Pixel { get; }

        /// <summary>Position on the ground in cells (unsnapped).</summary>
        public Vector2 Cell { get; }

        /// <summary>0 = left, 1 = right.</summary>
        public int Button { get; }

        public bool Shift { get; }

        public bool Alt { get; }
    }
}
