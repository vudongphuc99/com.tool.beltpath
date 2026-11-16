using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// The two Bezier handles of a point, as offsets from the point. Each offset is (ground x, elevation, ground y)
    /// in cells. <see cref="In"/> points back along the belt, <see cref="Out"/> forward.
    /// </summary>
    public readonly struct MapHandles
    {
        public MapHandles(Vector3 inOffset, Vector3 outOffset)
        {
            In = inOffset;
            Out = outOffset;
        }

        public Vector3 In { get; }

        public Vector3 Out { get; }

        public Vector3 Get(bool isOut)
        {
            return isOut ? Out : In;
        }

        public MapHandles With(bool isOut, Vector3 offset)
        {
            return isOut ? new MapHandles(In, offset) : new MapHandles(offset, Out);
        }
    }
}
