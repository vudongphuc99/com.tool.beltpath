using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Colors shared by the top view and the profile: low points blue, high points orange.</summary>
    public static class MapElevationPalette
    {
        public static readonly Color GridLine = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color PathLine = new Color(0.55f, 0.85f, 1f, 0.9f);
        public static readonly Color Selection = Color.white;
        public static readonly Color MarqueeFill = new Color(0.3f, 0.6f, 1f, 0.2f);
        public static readonly Color MarqueeLine = new Color(0.4f, 0.7f, 1f, 0.9f);
        public static readonly Color Ghost = new Color(1f, 0.2f, 0.2f, 0.9f);

        private static readonly Color Low = new Color(0.25f, 0.6f, 1f);
        private static readonly Color High = new Color(1f, 0.55f, 0.15f);

        public static Color Evaluate(float elevationCells)
        {
            return Color.Lerp(Low, High, Mathf.InverseLerp(0f, MapDocument.MaxElevationCells, elevationCells));
        }
    }
}
