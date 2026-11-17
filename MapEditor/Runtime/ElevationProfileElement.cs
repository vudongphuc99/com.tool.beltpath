using UnityEngine;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Side profile of the belt: horizontal = distance along the points, vertical = elevation. Dragging a point up or
    /// down changes its elevation (Shift = no snapping), so slopes are visible and editable.
    /// </summary>
    public sealed class ElevationProfileElement : VisualElement
    {
        private const float PaddingPixels = 18f;
        private const float NodeRadiusPixels = 6f;
        private const float HitRadiusPixels = 11f;
        private const float LineWidth = 3f;
        private const float GridLineWidth = 1f;
        private const float GridStepCells = 2f;
        private const float MinLengthCells = 0.0001f;
        private const float SelectedGrowPixels = 2f;
        private const float HandleRadiusPixels = 4.5f;
        private const float HandleHitRadiusPixels = 10f;
        private const float HandleLineWidth = 1.5f;
        private const float AutoHandleAlpha = 0.45f;

        private readonly MapDocument document;
        private readonly MapEditorState state;
        private readonly MapHistory history;
        private bool gestureOpen;
        private int draggingIndex = MapEditorState.NoSelection;
        private bool draggingHandle;
        private bool draggingHandleIsOut;

        public ElevationProfileElement(MapDocument document, MapEditorState state, MapHistory history)
        {
            this.history = history;
            this.document = document;
            this.state = state;
            style.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            style.overflow = Overflow.Hidden;
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(HandlePointerDown);
            RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            RegisterCallback<PointerUpEvent>(HandlePointerUp);
            RegisterCallback<PointerCaptureOutEvent>(evt => EndDrag());
        }

        public void Refresh()
        {
            MarkDirtyRepaint();
        }

        private float PlotHeight => Mathf.Max(1f, contentRect.height - 2f * PaddingPixels);

        private float ElevationToY(float elevation)
        {
            return contentRect.height - PaddingPixels - elevation / MapDocument.MaxElevationCells * PlotHeight;
        }

        private float YToElevation(float y)
        {
            return (contentRect.height - PaddingPixels - y) / PlotHeight * MapDocument.MaxElevationCells;
        }

        // Distance along the points, from the first point, one value per point.
        private float[] CumulativeDistances(out float total)
        {
            var result = new float[document.Nodes.Count];
            total = 0f;
            for (var i = 1; i < result.Length; i++)
            {
                total += Vector2.Distance(document.Nodes[i - 1].Ground, document.Nodes[i].Ground);
                result[i] = total;
            }

            if (document.IsClosed && result.Length > 1)
            {
                total += Vector2.Distance(document.Nodes[result.Length - 1].Ground, document.Nodes[0].Ground);
            }

            return result;
        }

        private float DistanceToX(float distance, float total)
        {
            var width = Mathf.Max(1f, contentRect.width - 2f * PaddingPixels);
            return total < MinLengthCells ? contentRect.width * 0.5f : PaddingPixels + distance / total * width;
        }

        private Vector2[] NodePixels()
        {
            var distances = CumulativeDistances(out var total);
            var pixels = new Vector2[distances.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Vector2(DistanceToX(distances[i], total), ElevationToY(document.Nodes[i].Elevation));
            }

            return pixels;
        }

        private int HitTest(Vector2 pixel)
        {
            var pixels = NodePixels();
            for (var i = pixels.Length - 1; i >= 0; i--)
            {
                if (Vector2.Distance(pixels[i], pixel) <= HitRadiusPixels)
                {
                    return i;
                }
            }

            return MapEditorState.NoSelection;
        }

        // Pixel of a handle of the selected point: x from the handle's ground length, y from its elevation.
        private bool TryGetHandlePixel(bool isOut, out Vector2 pixel)
        {
            pixel = default;
            var index = state.SelectedIndex;
            var closed = document.IsClosed;
            if (!state.HandlesVisible || index < 0 || index >= document.Nodes.Count
                || !MapCurveMath.IsHandleUsed(document.Nodes.Count, index, isOut, closed))
            {
                return false;
            }

            CumulativeDistances(out var total);
            var offset = MapCurveMath.GetHandles(document.Nodes, index, closed).Get(isOut);
            var node = NodePixels()[index];
            var pixelsPerCell = total < MinLengthCells ? 0f : (contentRect.width - 2f * PaddingPixels) / total;
            var length = new Vector2(offset.x, offset.z).magnitude * pixelsPerCell;
            pixel = new Vector2(node.x + (isOut ? length : -length), ElevationToY(document.Nodes[index].Elevation + offset.y));
            return true;
        }

        private bool HitTestHandle(Vector2 pixel, out bool isOut)
        {
            isOut = true;
            if (TryGetHandlePixel(true, out var outPixel) && Vector2.Distance(outPixel, pixel) <= HandleHitRadiusPixels)
            {
                return true;
            }

            isOut = false;
            return TryGetHandlePixel(false, out var inPixel) && Vector2.Distance(inPixel, pixel) <= HandleHitRadiusPixels;
        }

        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !state.PlaceEnabled)
            {
                return;
            }

            if (HitTestHandle(evt.localPosition, out var isOut))
            {
                draggingHandle = true;
                draggingHandleIsOut = isOut;
                draggingIndex = state.SelectedIndex;
                BeginDrag(evt.pointerId);
                return;
            }

            var hit = HitTest(evt.localPosition);
            if (hit == MapEditorState.NoSelection)
            {
                return;
            }

            draggingHandle = false;
            draggingIndex = hit;
            state.SelectedIndex = hit;
            BeginDrag(evt.pointerId);
        }

        private void HandlePointerMove(PointerMoveEvent evt)
        {
            if (draggingIndex == MapEditorState.NoSelection)
            {
                return;
            }

            if (draggingHandle)
            {
                DragHandle(evt.localPosition.y, evt.altKey);
                return;
            }

            var elevation = MapSnapper.SnapElevation(YToElevation(evt.localPosition.y), evt.shiftKey);
            document.SetElevation(draggingIndex, elevation);
        }

        // The handle follows the pointer's height (kept inside the plot); Alt moves it alone.
        private void DragHandle(float y, bool broken)
        {
            var nodes = document.Nodes;
            var absolute = Mathf.Clamp(YToElevation(y), 0f, MapDocument.MaxElevationCells);
            var offset = absolute - nodes[draggingIndex].Elevation;
            var handles = MapCurveMath.GetHandles(nodes, draggingIndex, document.IsClosed);
            var mode = broken ? MapHandleMode.Broken : nodes[draggingIndex].HandleMode;
            document.SetHandles(draggingIndex,
                MapCurveMath.ApplyElevationDrag(handles, draggingHandleIsOut, offset, mode));
        }

        // A drag is one undo step, however many pointer moves it takes.
        private void BeginDrag(int pointerId)
        {
            if (!gestureOpen)
            {
                history.BeginGesture();
                gestureOpen = true;
            }

            this.CapturePointer(pointerId);
        }

        private void EndDrag()
        {
            draggingIndex = MapEditorState.NoSelection;
            draggingHandle = false;
            if (gestureOpen)
            {
                gestureOpen = false;
                history.EndGesture();
            }
        }

        private void HandlePointerUp(PointerUpEvent evt)
        {
            if (draggingIndex == MapEditorState.NoSelection)
            {
                return;
            }

            this.ReleasePointer(evt.pointerId);
            EndDrag();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            DrawGrid(painter);
            var pixels = NodePixels();
            DrawLine(painter, pixels);
            for (var i = 0; i < pixels.Length; i++)
            {
                DrawNode(painter, pixels[i], i);
            }

            DrawHandles(painter, pixels);
        }

        private void DrawHandles(Painter2D painter, Vector2[] pixels)
        {
            var index = state.SelectedIndex;
            if (index < 0 || index >= pixels.Length)
            {
                return;
            }

            var color = MapElevationPalette.Selection;
            color.a = document.Nodes[index].HasHandles ? 1f : AutoHandleAlpha;
            DrawHandle(painter, pixels[index], true, color);
            DrawHandle(painter, pixels[index], false, color);
        }

        private void DrawHandle(Painter2D painter, Vector2 center, bool isOut, Color color)
        {
            if (!TryGetHandlePixel(isOut, out var pixel))
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = HandleLineWidth;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(pixel);
            painter.Stroke();
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(pixel, HandleRadiusPixels, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }

        private void DrawGrid(Painter2D painter)
        {
            painter.strokeColor = MapElevationPalette.GridLine;
            painter.lineWidth = GridLineWidth;
            painter.BeginPath();
            for (var e = 0f; e <= MapDocument.MaxElevationCells; e += GridStepCells)
            {
                var y = ElevationToY(e);
                painter.MoveTo(new Vector2(PaddingPixels, y));
                painter.LineTo(new Vector2(contentRect.width - PaddingPixels, y));
            }

            painter.Stroke();
        }

        private void DrawLine(Painter2D painter, Vector2[] pixels)
        {
            if (pixels.Length < 2)
            {
                return;
            }

            painter.strokeColor = MapElevationPalette.PathLine;
            painter.lineWidth = LineWidth;
            painter.BeginPath();
            painter.MoveTo(pixels[0]);
            for (var i = 1; i < pixels.Length; i++)
            {
                painter.LineTo(pixels[i]);
            }

            if (document.IsClosed)
            {
                painter.LineTo(new Vector2(DistanceToX(1f, 1f), pixels[0].y));
            }

            painter.Stroke();
        }

        private void DrawNode(Painter2D painter, Vector2 center, int index)
        {
            var selected = state.IsSelected(index);
            painter.fillColor = selected ? MapElevationPalette.Selection : MapElevationPalette.Evaluate(document.Nodes[index].Elevation);
            painter.BeginPath();
            painter.Arc(center, selected ? NodeRadiusPixels + SelectedGrowPixels : NodeRadiusPixels, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
