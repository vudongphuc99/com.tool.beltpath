using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Pan and zoom of the top view: converts ground cells to element pixels and back.</summary>
    public sealed class MapViewTransform
    {
        private const float DefaultPixelsPerCell = 24f;
        private const float MinPixelsPerCell = 4f;
        private const float MaxPixelsPerCell = 120f;

        /// <summary>Ground cell shown at the pixel origin offset <see cref="PanPixels"/>.</summary>
        public Vector2 PanPixels { get; set; }

        public float PixelsPerCell { get; private set; } = DefaultPixelsPerCell;

        public Vector2 CellToPixel(Vector2 cell)
        {
            return cell * PixelsPerCell + PanPixels;
        }

        public Vector2 PixelToCell(Vector2 pixel)
        {
            return (pixel - PanPixels) / PixelsPerCell;
        }

        /// <summary>Zooms by a factor while the cell under <paramref name="pivotPixel"/> stays put.</summary>
        public void ZoomAt(Vector2 pivotPixel, float factor)
        {
            var cellUnderPivot = PixelToCell(pivotPixel);
            PixelsPerCell = Mathf.Clamp(PixelsPerCell * factor, MinPixelsPerCell, MaxPixelsPerCell);
            PanPixels = pivotPixel - cellUnderPivot * PixelsPerCell;
        }
    }
}
