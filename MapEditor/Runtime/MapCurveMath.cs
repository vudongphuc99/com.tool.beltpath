using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Pure math for the editable curve handles. Points live in (ground x, elevation, ground y) cell space.
    /// </summary>
    public static class MapCurveMath
    {
        private const float MinLength = 1e-4f;
        private const float ModeTolerance = 1e-3f;

        /// <summary>The authored handles of a point, or the automatic ones when it has none.</summary>
        public static MapHandles GetHandles(IReadOnlyList<MapNode> nodes, int index, bool closed)
        {
            return nodes[index].HasHandles ? nodes[index].Handles : AutoHandles(nodes, index, closed);
        }

        /// <summary>The smooth tangent Unity's AutoSmooth knot gets, as a pair of opposite handles.</summary>
        public static MapHandles AutoHandles(IReadOnlyList<MapNode> nodes, int index, bool closed)
        {
            var current = nodes[index].Position;
            var previous = TryGetNeighbor(nodes, index - 1, closed, out var before) ? before.Position : current;
            var next = TryGetNeighbor(nodes, index + 1, closed, out var after) ? after.Position : current;
            Vector3 tangent = SplineUtility.GetAutoSmoothTangent(previous, current, next);
            return new MapHandles(-tangent, tangent);
        }

        /// <summary>False for the handle that has no segment to shape: the first In and last Out of an open belt.</summary>
        public static bool IsHandleUsed(int count, int index, bool isOut, bool closed)
        {
            if (closed)
            {
                return count >= 2;
            }

            return isOut ? index < count - 1 : index > 0;
        }

        /// <summary>
        /// Moves one handle on the ground plane (x, z of the offset; its elevation stays). The other handle follows
        /// <paramref name="mode"/>: Aligned turns it opposite keeping its own ground length, Mirrored mirrors it,
        /// Broken leaves it.
        /// </summary>
        public static MapHandles ApplyGroundDrag(MapHandles handles, bool dragOut, Vector2 newGroundOffset, MapHandleMode mode)
        {
            var dragged = handles.Get(dragOut);
            var result = handles.With(dragOut, new Vector3(newGroundOffset.x, dragged.y, newGroundOffset.y));
            if (mode == MapHandleMode.Mirrored)
            {
                return result.With(!dragOut, new Vector3(-newGroundOffset.x, -dragged.y, -newGroundOffset.y));
            }

            if (mode == MapHandleMode.Broken || newGroundOffset.magnitude < MinLength)
            {
                return result;
            }

            var other = handles.Get(!dragOut);
            var otherLength = new Vector2(other.x, other.z).magnitude;
            var opposite = -newGroundOffset.normalized * otherLength;
            return result.With(!dragOut, new Vector3(opposite.x, other.y, opposite.y));
        }

        /// <summary>
        /// Changes the elevation offset of one handle. The other handle follows <paramref name="mode"/>: Aligned gives
        /// it the opposite slope (elevation per ground length), Mirrored the opposite elevation, Broken leaves it.
        /// </summary>
        public static MapHandles ApplyElevationDrag(MapHandles handles, bool dragOut, float newElevationOffset, MapHandleMode mode)
        {
            var dragged = handles.Get(dragOut);
            var result = handles.With(dragOut, new Vector3(dragged.x, newElevationOffset, dragged.z));
            var other = handles.Get(!dragOut);
            if (mode == MapHandleMode.Mirrored)
            {
                return result.With(!dragOut, new Vector3(other.x, -newElevationOffset, other.z));
            }

            var draggedLength = new Vector2(dragged.x, dragged.z).magnitude;
            if (mode == MapHandleMode.Broken || draggedLength < MinLength)
            {
                return result;
            }

            var otherLength = new Vector2(other.x, other.z).magnitude;
            var slope = -newElevationOffset / draggedLength;
            return result.With(!dragOut, new Vector3(other.x, slope * otherLength, other.z));
        }

        /// <summary>Sets the whole offset of one handle; the other handle follows <paramref name="mode"/> in 3D.</summary>
        public static MapHandles ApplyOffset(MapHandles handles, bool isOut, Vector3 offset, MapHandleMode mode)
        {
            var result = handles.With(isOut, offset);
            if (mode == MapHandleMode.Mirrored)
            {
                return result.With(!isOut, -offset);
            }

            if (mode == MapHandleMode.Broken || offset.magnitude < MinLength)
            {
                return result;
            }

            var otherLength = handles.Get(!isOut).magnitude;
            return result.With(!isOut, -offset.normalized * otherLength);
        }

        /// <summary>Changes the length of one handle, keeping its direction. Mirrored mode changes both handles.</summary>
        public static MapHandles ApplyLength(MapHandles handles, bool isOut, float length, MapHandleMode mode)
        {
            var current = handles.Get(isOut);
            if (current.magnitude < MinLength || length < 0f)
            {
                return handles;
            }

            var scaled = current * (length / current.magnitude);
            var result = handles.With(isOut, scaled);
            return mode == MapHandleMode.Mirrored ? result.With(!isOut, -scaled) : result;
        }

        /// <summary>Makes the In handle fit the mode, keeping the Out handle (Aligned keeps the In length).</summary>
        public static MapHandles EnforceMode(MapHandles handles, MapHandleMode mode)
        {
            if (mode == MapHandleMode.Mirrored)
            {
                return new MapHandles(-handles.Out, handles.Out);
            }

            if (mode == MapHandleMode.Aligned && handles.Out.magnitude >= MinLength)
            {
                return new MapHandles(-handles.Out.normalized * handles.In.magnitude, handles.Out);
            }

            return handles;
        }

        /// <summary>The mode that describes a pair of handles read from a file.</summary>
        public static MapHandleMode InferMode(MapHandles handles)
        {
            if ((handles.In + handles.Out).magnitude <= ModeTolerance)
            {
                return MapHandleMode.Mirrored;
            }

            var lengths = handles.In.magnitude * handles.Out.magnitude;
            var opposite = Vector3.Dot(handles.In, handles.Out) < 0f;
            var straight = Vector3.Cross(handles.In, handles.Out).magnitude <= ModeTolerance * lengths;
            return lengths > MinLength * MinLength && opposite && straight ? MapHandleMode.Aligned : MapHandleMode.Broken;
        }

        private static bool TryGetNeighbor(IReadOnlyList<MapNode> nodes, int index, bool closed, out MapNode neighbor)
        {
            var count = nodes.Count;
            if (closed && count >= 2)
            {
                neighbor = nodes[(index + count) % count];
                return true;
            }

            var inRange = index >= 0 && index < count;
            neighbor = inRange ? nodes[index] : default;
            return inRange;
        }
    }
}
