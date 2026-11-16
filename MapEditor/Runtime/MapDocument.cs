using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// The map being edited: an ordered list of points and whether the belt loops. Points are kept as ground
    /// positions; <see cref="ToPathData"/> and <see cref="FromPathData"/> convert to the level grid, where a raised
    /// point is stored shifted by the plane shift of the shape settings.
    /// </summary>
    public sealed class MapDocument
    {
        public const float MaxElevationCells = 10f;

        private readonly List<MapNode> nodes = new List<MapNode>();

        /// <summary>Raised after any change to the points or the closed flag.</summary>
        public event Action Changed;

        public IReadOnlyList<MapNode> Nodes => nodes;

        public bool IsClosed { get; private set; }

        /// <summary>Appends a point and returns its index.</summary>
        public int Add(Vector2 ground, float elevation)
        {
            nodes.Add(new MapNode(ground, ClampElevation(elevation)));
            Changed?.Invoke();
            return nodes.Count - 1;
        }

        /// <summary>Inserts a point at an index (the old point at that index moves one place later).</summary>
        public void Insert(int index, Vector2 ground, float elevation)
        {
            nodes.Insert(index, new MapNode(ground, ClampElevation(elevation)));
            Changed?.Invoke();
        }

        public void SetHandles(int index, MapHandles handles)
        {
            nodes[index] = nodes[index].WithHandles(handles);
            Changed?.Invoke();
        }

        /// <summary>Gives a point authored handles in the mode (starting from its current or automatic handles).</summary>
        public void SetHandleMode(int index, MapHandleMode mode)
        {
            var handles = MapCurveMath.EnforceMode(MapCurveMath.GetHandles(nodes, index, IsClosed), mode);
            nodes[index] = nodes[index].WithHandles(handles).WithHandleMode(mode);
            Changed?.Invoke();
        }

        /// <summary>Returns a point to automatic smoothing.</summary>
        public void ClearHandles(int index)
        {
            nodes[index] = nodes[index].WithoutHandles();
            Changed?.Invoke();
        }

        public void Move(int index, Vector2 ground)
        {
            nodes[index] = nodes[index].WithGround(ground);
            Changed?.Invoke();
        }

        /// <summary>Sets the ground position and the elevation of a point at once (one change event); keeps its handles.</summary>
        public void SetPosition(int index, Vector2 ground, float elevation)
        {
            nodes[index] = nodes[index].WithGround(ground).WithElevation(ClampElevation(elevation));
            Changed?.Invoke();
        }

        public void SetElevation(int index, float elevation)
        {
            nodes[index] = nodes[index].WithElevation(ClampElevation(elevation));
            Changed?.Invoke();
        }

        public void Remove(int index)
        {
            nodes.RemoveAt(index);
            Changed?.Invoke();
        }

        /// <summary>Removes several points at once (one change event).</summary>
        public void RemoveRange(IReadOnlyList<int> indices)
        {
            if (indices.Count == 0)
            {
                return;
            }

            var sorted = new List<int>(indices);
            sorted.Sort();
            for (var i = sorted.Count - 1; i >= 0; i--)
            {
                nodes.RemoveAt(sorted[i]);
            }

            Changed?.Invoke();
        }

        /// <summary>Puts each point at its start ground position plus <paramref name="delta"/> (one change event).</summary>
        public void MoveMany(IReadOnlyList<int> indices, IReadOnlyList<Vector2> startGrounds, Vector2 delta)
        {
            for (var k = 0; k < indices.Count; k++)
            {
                nodes[indices[k]] = nodes[indices[k]].WithGround(startGrounds[k] + delta);
            }

            Changed?.Invoke();
        }

        /// <summary>Puts each point at its start elevation plus <paramref name="delta"/> (one change event).</summary>
        public void ElevateMany(IReadOnlyList<int> indices, IReadOnlyList<float> startElevations, float delta)
        {
            for (var k = 0; k < indices.Count; k++)
            {
                nodes[indices[k]] = nodes[indices[k]].WithElevation(ClampElevation(startElevations[k] + delta));
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Scales a group of points around its center (one change event): their ground positions around the center of
        /// their bounding box, or their elevations around the middle of their range. Handles scale with the points.
        /// Does nothing for fewer than two points or a factor that is not positive.
        /// </summary>
        public void ScaleMany(IReadOnlyList<int> indices, float factor, bool scaleElevation)
        {
            if (indices.Count < 2 || factor <= 0f)
            {
                return;
            }

            if (scaleElevation)
            {
                ScaleElevations(indices, factor);
            }
            else
            {
                ScaleGrounds(indices, factor);
            }

            Changed?.Invoke();
        }

        public void SetClosed(bool closed)
        {
            IsClosed = closed;
            Changed?.Invoke();
        }

        public void Clear()
        {
            nodes.Clear();
            IsClosed = false;
            Changed?.Invoke();
        }

        public MapSnapshot Capture()
        {
            return new MapSnapshot(nodes.ToArray(), IsClosed);
        }

        /// <summary>Replaces the whole map with a snapshot taken earlier.</summary>
        public void Restore(MapSnapshot snapshot)
        {
            nodes.Clear();
            nodes.AddRange(snapshot.Nodes);
            IsClosed = snapshot.IsClosed;
            Changed?.Invoke();
        }

        /// <summary>Replaces the whole map.</summary>
        public void FromPathData(BeltPathData data, BeltShapeSettings shape)
        {
            nodes.Clear();
            foreach (var node in data.Nodes)
            {
                nodes.Add(ToMapNode(node, shape));
            }

            IsClosed = data.IsClosed;
            Changed?.Invoke();
        }

        public BeltPathData ToPathData(BeltShapeSettings shape)
        {
            var result = new List<BeltNodeData>(nodes.Count);
            foreach (var node in nodes)
            {
                result.Add(ToNodeData(node, shape));
            }

            return new BeltPathData(result, IsClosed);
        }

        // A raised point is stored shifted: grid y = ground y - shift * elevation (handles follow the same rule).
        private static Vector2 GroundToGrid(Vector2 ground, float elevation, BeltShapeSettings shape)
        {
            return new Vector2(ground.x, ground.y - shape.ElevationPlaneShift * elevation);
        }

        private static Vector2 GridToGround(Vector2 grid, float elevation, BeltShapeSettings shape)
        {
            return new Vector2(grid.x, grid.y + shape.ElevationPlaneShift * elevation);
        }

        private static BeltNodeData ToNodeData(MapNode node, BeltShapeSettings shape)
        {
            var grid = GroundToGrid(node.Ground, node.Elevation, shape);
            if (!node.HasHandles)
            {
                return BeltNodeData.Smooth(grid, node.Elevation);
            }

            var handleIn = node.Position + node.Handles.In;
            var handleOut = node.Position + node.Handles.Out;
            return BeltNodeData.Custom(grid, node.Elevation,
                GroundToGrid(new Vector2(handleIn.x, handleIn.z), handleIn.y, shape),
                GroundToGrid(new Vector2(handleOut.x, handleOut.z), handleOut.y, shape),
                handleIn.y, handleOut.y);
        }

        private static MapNode ToMapNode(BeltNodeData data, BeltShapeSettings shape)
        {
            var ground = GridToGround(data.GridPosition, data.Elevation, shape);
            var node = new MapNode(ground, data.Elevation);
            if (!data.HasCustomTangent)
            {
                return node;
            }

            var position = node.Position;
            var groundIn = GridToGround(data.TangentInGrid, data.ElevTangentIn, shape);
            var groundOut = GridToGround(data.TangentOutGrid, data.ElevTangentOut, shape);
            var handleIn = new Vector3(groundIn.x, data.ElevTangentIn, groundIn.y) - position;
            var handleOut = new Vector3(groundOut.x, data.ElevTangentOut, groundOut.y) - position;
            var handles = new MapHandles(handleIn, handleOut);
            return node.WithHandles(handles).WithHandleMode(MapCurveMath.InferMode(handles));
        }

        private void ScaleGrounds(IReadOnlyList<int> indices, float factor)
        {
            var min = nodes[indices[0]].Ground;
            var max = min;
            foreach (var index in indices)
            {
                min = Vector2.Min(min, nodes[index].Ground);
                max = Vector2.Max(max, nodes[index].Ground);
            }

            var center = (min + max) * 0.5f;
            var handleScale = new Vector3(factor, 1f, factor);
            foreach (var index in indices)
            {
                var node = nodes[index].WithGround(center + (nodes[index].Ground - center) * factor);
                nodes[index] = ScaleHandlesOf(node, handleScale);
            }
        }

        private void ScaleElevations(IReadOnlyList<int> indices, float factor)
        {
            var min = nodes[indices[0]].Elevation;
            var max = min;
            foreach (var index in indices)
            {
                min = Mathf.Min(min, nodes[index].Elevation);
                max = Mathf.Max(max, nodes[index].Elevation);
            }

            var center = (min + max) * 0.5f;
            var handleScale = new Vector3(1f, factor, 1f);
            foreach (var index in indices)
            {
                var elevation = ClampElevation(center + (nodes[index].Elevation - center) * factor);
                nodes[index] = ScaleHandlesOf(nodes[index].WithElevation(elevation), handleScale);
            }
        }

        // Only authored handles are scaled; a point that is smoothed automatically has none.
        private static MapNode ScaleHandlesOf(MapNode node, Vector3 scale)
        {
            if (!node.HasHandles)
            {
                return node;
            }

            var handles = node.Handles;
            return node.WithHandles(new MapHandles(Vector3.Scale(handles.In, scale), Vector3.Scale(handles.Out, scale)));
        }

        private static float ClampElevation(float elevation)
        {
            return Mathf.Clamp(elevation, 0f, MaxElevationCells);
        }
    }
}
