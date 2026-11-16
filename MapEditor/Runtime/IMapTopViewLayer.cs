using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Extra drawing on the top view, painted after the belt path and before the points and handles.</summary>
    public interface IMapTopViewLayer
    {
        void Draw(Painter2D painter, MapViewTransform view);
    }
}
