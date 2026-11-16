using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>The ground plane: grid, belt path, and the points colored by elevation. Draws only; input is in <see cref="TopViewManipulator"/>.</summary>
    public sealed class TopViewElement : VisualElement
    {
        private const float NodeRadiusPixels = 7f;
        private const float SelectedRingPixels = 3f;
        private const float HitRadiusPixels = 11f;
        private const float MinGridSpacingPixels = 10f;
        private const int CoarseGridFactor = 5;
        private const float LabelOffsetX = 10f;
        private const float LabelOffsetY = -20f;
        private const int LabelFontSize = 10;
        private const float PathLineWidth = 3f;
        private const float GridLineWidth = 1f;
        private const float HandleRadiusPixels = 5f;
        private const float HandleHitRadiusPixels = 10f;
        private const float HandleLineWidth = 1.5f;
        private const float AutoHandleAlpha = 0.45f;
        private const float GhostHaloGrowPixels = 5f;
        private const float MarqueeLineWidth = 1f;
        private const float ImageWidthCells = 40f;
        private const float DefaultImageOpacity = 0.5f;
        private const float GhostHaloAlpha = 0.3f;
        private static readonly Vector2 InitialPanPixels = new Vector2(40f, 40f);

        private readonly MapDocument document;
        private readonly MapEditorState state;
        private readonly List<Label> labels = new List<Label>();
        private IReadOnlyList<Vector3> pathGround = System.Array.Empty<Vector3>();
        private Vector2 hoverPixel;
        private bool hasHover;
        private Rect marquee;
        private bool hasMarquee;
        private Texture2D referenceImage;
        private float referenceOpacity = DefaultImageOpacity;
        private bool referenceVisible = true;

        public TopViewElement(MapDocument document, MapEditorState state, MapViewTransform view)
        {
            this.document = document;
            this.state = state;
            View = view;
            style.backgroundColor = new Color(0.1f, 0.11f, 0.13f);
            style.overflow = Overflow.Hidden;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(evt => Refresh());
            RegisterCallback<PointerMoveEvent>(HandleHoverMoved);
            RegisterCallback<PointerLeaveEvent>(HandleHoverLeft);
            View.PanPixels = InitialPanPixels;
        }

        public MapViewTransform View { get; }

        /// <summary>The built belt path as (ground x, elevation, ground y) samples in cells; empty when no belt is built.</summary>
        public void SetPath(IReadOnlyList<Vector3> groundPoints)
        {
            pathGround = groundPoints;
        }

        /// <summary>Index of the point under a pixel, or <see cref="MapEditorState.NoSelection"/>.</summary>
        public int HitTestNode(Vector2 pixel)
        {
            for (var i = document.Nodes.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(View.CellToPixel(document.Nodes[i].Ground), pixel) <= HitRadiusPixels)
                {
                    return i;
                }
            }

            return MapEditorState.NoSelection;
        }

        /// <summary>Pixel of a handle of the selected point; false when nothing is selected or the handle is unused.</summary>
        public bool TryGetHandlePixel(bool isOut, out Vector2 pixel)
        {
            pixel = default;
            var index = state.SelectedIndex;
            var closed = document.IsClosed;
            if (!state.HandlesVisible || index < 0 || index >= document.Nodes.Count
                || !MapCurveMath.IsHandleUsed(document.Nodes.Count, index, isOut, closed))
            {
                return false;
            }

            var offset = MapCurveMath.GetHandles(document.Nodes, index, closed).Get(isOut);
            pixel = View.CellToPixel(document.Nodes[index].Ground + new Vector2(offset.x, offset.z));
            return true;
        }

        /// <summary>Which handle of the selected point is under a pixel; false when none.</summary>
        public bool HitTestHandle(Vector2 pixel, out bool isOut)
        {
            isOut = true;
            if (TryGetHandlePixel(true, out var outPixel) && Vector2.Distance(outPixel, pixel) <= HandleHitRadiusPixels)
            {
                return true;
            }

            isOut = false;
            return TryGetHandlePixel(false, out var inPixel) && Vector2.Distance(inPixel, pixel) <= HandleHitRadiusPixels;
        }

        /// <summary>Shows a picture under the grid, from ground (0, 0), <see cref="ImageWidthCells"/> cells wide. Null removes it.</summary>
        public void SetReferenceImage(Texture2D image)
        {
            referenceImage = image;
            Refresh();
        }

        public void SetReferenceOpacity(float opacity)
        {
            referenceOpacity = opacity;
            Refresh();
        }

        public void SetReferenceVisible(bool visible)
        {
            referenceVisible = visible;
            Refresh();
        }

        /// <summary>Shows the selection box (pixels of this element) while it is dragged.</summary>
        public void SetMarquee(Rect pixelRect)
        {
            marquee = pixelRect;
            hasMarquee = true;
            MarkDirtyRepaint();
        }

        public void ClearMarquee()
        {
            hasMarquee = false;
            MarkDirtyRepaint();
        }

        /// <summary>The spot on the belt a click at this pixel would insert a point at.</summary>
        public bool TryProbeInsertion(Vector2 pixel, out MapInsertion insertion)
        {
            return MapPathProbe.TryProbe(pathGround, document.Nodes, document.IsClosed, View.PixelToCell(pixel),
                out insertion);
        }

        public void Refresh()
        {
            ApplyReferenceImage();
            RefreshLabels();
            MarkDirtyRepaint();
        }

        // The picture is the element's own background, so it is drawn below the grid, belt and points.
        private void ApplyReferenceImage()
        {
            if (referenceImage == null || !referenceVisible)
            {
                style.backgroundImage = StyleKeyword.None;
                return;
            }

            var width = ImageWidthCells * View.PixelsPerCell;
            var height = width * referenceImage.height / referenceImage.width;
            var origin = View.CellToPixel(Vector2.zero);
            style.backgroundImage = new StyleBackground(referenceImage);
            style.backgroundSize = new BackgroundSize(width, height);
            style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, origin.x);
            style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, origin.y);
            style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, referenceOpacity);
        }

        private void HandleHoverMoved(PointerMoveEvent evt)
        {
            hoverPixel = evt.localPosition;
            hasHover = true;
            if (state.IsInserting)
            {
                MarkDirtyRepaint();
            }
        }

        private void HandleHoverLeft(PointerLeaveEvent evt)
        {
            hasHover = false;
            MarkDirtyRepaint();
        }

        private void RefreshLabels()
        {
            while (labels.Count < document.Nodes.Count)
            {
                labels.Add(CreateLabel());
            }

            for (var i = 0; i < labels.Count; i++)
            {
                UpdateLabel(labels[i], i);
            }
        }

        private Label CreateLabel()
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.fontSize = LabelFontSize;
            label.style.color = Color.white;
            Add(label);
            return label;
        }

        private void UpdateLabel(Label label, int index)
        {
            var visible = index < document.Nodes.Count;
            label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                return;
            }

            var node = document.Nodes[index];
            var pixel = View.CellToPixel(node.Ground);
            label.text = $"{index + 1}  e={node.Elevation:0.#}";
            label.style.left = pixel.x + LabelOffsetX;
            label.style.top = pixel.y + LabelOffsetY;
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            DrawGrid(painter);
            DrawPath(painter);
            DrawNodes(painter);
            DrawHandles(painter);
            DrawInsertionGhost(painter);
            DrawMarquee(painter);
        }

        private void DrawGrid(Painter2D painter)
        {
            var factor = View.PixelsPerCell < MinGridSpacingPixels ? CoarseGridFactor : 1;
            var min = View.PixelToCell(Vector2.zero);
            var max = View.PixelToCell(contentRect.size);
            painter.strokeColor = MapElevationPalette.GridLine;
            painter.lineWidth = GridLineWidth;
            painter.BeginPath();
            for (var x = Mathf.CeilToInt(min.x / factor) * factor; x <= max.x; x += factor)
            {
                var pixelX = View.CellToPixel(new Vector2(x, 0f)).x;
                painter.MoveTo(new Vector2(pixelX, 0f));
                painter.LineTo(new Vector2(pixelX, contentRect.height));
            }

            for (var y = Mathf.CeilToInt(min.y / factor) * factor; y <= max.y; y += factor)
            {
                var pixelY = View.CellToPixel(new Vector2(0f, y)).y;
                painter.MoveTo(new Vector2(0f, pixelY));
                painter.LineTo(new Vector2(contentRect.width, pixelY));
            }

            painter.Stroke();
        }

        private void DrawPath(Painter2D painter)
        {
            if (pathGround.Count < 2)
            {
                return;
            }

            painter.strokeColor = MapElevationPalette.PathLine;
            painter.lineWidth = PathLineWidth;
            painter.BeginPath();
            painter.MoveTo(GroundPixel(pathGround[0]));
            for (var i = 1; i < pathGround.Count; i++)
            {
                painter.LineTo(GroundPixel(pathGround[i]));
            }

            painter.Stroke();
        }

        private void DrawNodes(Painter2D painter)
        {
            for (var i = 0; i < document.Nodes.Count; i++)
            {
                var node = document.Nodes[i];
                var center = View.CellToPixel(node.Ground);
                if (state.IsSelected(i))
                {
                    FillCircle(painter, center, NodeRadiusPixels + SelectedRingPixels, MapElevationPalette.Selection);
                }

                FillCircle(painter, center, NodeRadiusPixels, MapElevationPalette.Evaluate(node.Elevation));
            }
        }

        private void DrawMarquee(Painter2D painter)
        {
            if (!hasMarquee)
            {
                return;
            }

            painter.BeginPath();
            painter.MoveTo(marquee.min);
            painter.LineTo(new Vector2(marquee.xMax, marquee.yMin));
            painter.LineTo(marquee.max);
            painter.LineTo(new Vector2(marquee.xMin, marquee.yMax));
            painter.ClosePath();
            painter.fillColor = MapElevationPalette.MarqueeFill;
            painter.Fill();
            painter.strokeColor = MapElevationPalette.MarqueeLine;
            painter.lineWidth = MarqueeLineWidth;
            painter.Stroke();
        }

        private Vector2 GroundPixel(Vector3 sample)
        {
            return View.CellToPixel(new Vector2(sample.x, sample.z));
        }

        // Red marker on the belt where a click would insert a point.
        private void DrawInsertionGhost(Painter2D painter)
        {
            if (!state.IsInserting || !hasHover || !TryProbeInsertion(hoverPixel, out var insertion))
            {
                return;
            }

            var center = View.CellToPixel(insertion.Ground);
            var halo = MapElevationPalette.Ghost;
            halo.a = GhostHaloAlpha;
            FillCircle(painter, center, NodeRadiusPixels + GhostHaloGrowPixels, halo);
            FillCircle(painter, center, NodeRadiusPixels, MapElevationPalette.Ghost);
        }

        private void DrawHandles(Painter2D painter)
        {
            var index = state.SelectedIndex;
            if (index < 0 || index >= document.Nodes.Count)
            {
                return;
            }

            var center = View.CellToPixel(document.Nodes[index].Ground);
            var color = MapElevationPalette.Selection;
            color.a = document.Nodes[index].HasHandles ? 1f : AutoHandleAlpha;
            DrawHandle(painter, center, true, color);
            DrawHandle(painter, center, false, color);
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
            FillCircle(painter, pixel, HandleRadiusPixels, color);
        }

        private static void FillCircle(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
