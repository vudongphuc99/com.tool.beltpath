using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Mouse input of the top view. Left drag on empty space selects the points inside the box; a left click there
    /// adds a point (when the place tool is on) or clears the selection. Dragging a selected point moves the whole
    /// selection (Place mode) or changes its elevation (Height mode); a click on a point selects only it. While the
    /// insert mode is on, a click puts a point on the belt. Right click removes, middle drag pans, the wheel zooms,
    /// and holding Shift turns snapping off. While an extension tool is active, the left and right buttons go to it
    /// instead; pan and zoom stay here.
    /// </summary>
    public sealed class TopViewManipulator : PointerManipulator
    {
        private const int LeftButton = 0;
        private const int RightButton = 1;
        private const int MiddleButton = 2;
        private const float PixelsPerElevationCell = 24f;
        private const float ZoomPerWheelUnit = 1.05f;
        private const float MinNodeGapCells = 0.01f;
        private const float DragThresholdPixels = 4f;

        private enum DragKind
        {
            None,
            Nodes,
            Handle,
            Pan,
            Marquee,
            Tool
        }

        private readonly MapDocument document;
        private readonly MapEditorState state;
        private readonly TopViewElement topView;
        private readonly MapHistory history;

        private DragKind drag;
        private bool gestureOpen;
        private int dragPointerId;
        private Vector2 downPixel;
        private Vector2 lastPointer;
        private bool dragMoved;
        private bool draggedHandleIsOut;
        private int toolButton;

        // Snapshot of the points being dragged, taken when the drag starts.
        private readonly List<int> dragIndices = new List<int>();
        private readonly List<Vector2> dragStartGrounds = new List<Vector2>();
        private readonly List<float> dragStartElevations = new List<float>();
        private int dragAnchor;
        private bool dragWasGroup;

        public TopViewManipulator(MapDocument document, MapEditorState state, TopViewElement topView, MapHistory history)
        {
            this.history = history;
            this.document = document;
            this.state = state;
            this.topView = topView;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            target.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            target.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(HandleCaptureOut);
            target.RegisterCallback<WheelEvent>(HandleWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            target.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            target.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(HandleCaptureOut);
            target.UnregisterCallback<WheelEvent>(HandleWheel);
        }

        private void HandlePointerDown(PointerDownEvent evt)
        {
            var pixel = (Vector2)evt.localPosition;
            if (state.ActiveTool != null && evt.button != MiddleButton)
            {
                BeginToolDrag(evt);
                return;
            }

            switch (evt.button)
            {
                case LeftButton:
                    HandleLeftDown(pixel, evt);
                    break;
                case RightButton:
                    RemoveNodeAt(pixel);
                    break;
                case MiddleButton:
                    BeginDrag(DragKind.Pan, evt);
                    break;
            }
        }

        private void BeginToolDrag(PointerDownEvent evt)
        {
            toolButton = evt.button;
            BeginDrag(DragKind.Tool, evt);
            state.ActiveTool.PointerDown(ToPointer(evt.localPosition, evt.shiftKey, evt.altKey));
        }

        private MapPointer ToPointer(Vector2 pixel, bool shift, bool alt)
        {
            return new MapPointer(pixel, topView.View.PixelToCell(pixel), toolButton, shift, alt);
        }

        private void HandleLeftDown(Vector2 pixel, PointerDownEvent evt)
        {
            if (state.IsInserting)
            {
                InsertAtPointer(pixel);
                return;
            }

            if (topView.HitTestHandle(pixel, out var isOut))
            {
                draggedHandleIsOut = isOut;
                BeginDrag(DragKind.Handle, evt);
                return;
            }

            var hit = topView.HitTestNode(pixel);
            if (hit == MapEditorState.NoSelection)
            {
                BeginDrag(DragKind.Marquee, evt);
                return;
            }

            BeginNodeDrag(hit, evt);
        }

        // Puts a point exactly where the red ghost is, on the belt.
        private void InsertAtPointer(Vector2 pixel)
        {
            if (!topView.TryProbeInsertion(pixel, out var insertion))
            {
                return;
            }

            document.Insert(insertion.InsertIndex, insertion.Ground, insertion.Elevation);
            state.SelectOnly(insertion.InsertIndex);
        }

        private int AddNode(Vector2 pixel, bool free)
        {
            var ground = MapSnapper.SnapGround(topView.View.PixelToCell(pixel), free);
            var count = document.Nodes.Count;
            if (count > 0 && Vector2.Distance(document.Nodes[count - 1].Ground, ground) < MinNodeGapCells)
            {
                return count - 1;
            }

            return document.Add(ground, 0f);
        }

        private void RemoveNodeAt(Vector2 pixel)
        {
            var hit = topView.HitTestNode(pixel);
            if (hit == MapEditorState.NoSelection)
            {
                return;
            }

            var removeGroup = state.IsSelected(hit) && state.SelectedIndices.Count > 1;
            var removed = removeGroup ? new List<int>(state.SelectedIndices) : new List<int> { hit };
            state.ClearSelection();
            document.RemoveRange(removed);
        }

        private void BeginDrag(DragKind kind, PointerDownEvent evt)
        {
            drag = kind;
            dragPointerId = evt.pointerId;
            downPixel = evt.localPosition;
            lastPointer = evt.localPosition;
            dragMoved = false;
            if (kind == DragKind.Nodes || kind == DragKind.Handle)
            {
                OpenGesture();
            }

            target.CapturePointer(evt.pointerId);
        }

        // A drag is one undo step, however many pointer moves it takes.
        private void OpenGesture()
        {
            if (!gestureOpen)
            {
                history.BeginGesture();
                gestureOpen = true;
            }
        }

        private void CloseGesture()
        {
            if (gestureOpen)
            {
                gestureOpen = false;
                history.EndGesture();
            }
        }

        private void HandleCaptureOut(PointerCaptureOutEvent evt)
        {
            drag = DragKind.None;
            topView.ClearMarquee();
            CloseGesture();
        }

        // A point outside the selection becomes the only selected point; a point inside a group drags the group.
        private void BeginNodeDrag(int hit, PointerDownEvent evt)
        {
            dragWasGroup = state.IsSelected(hit) && state.SelectedIndices.Count > 1;
            if (!state.IsSelected(hit))
            {
                state.SelectOnly(hit);
            }

            dragAnchor = hit;
            SnapshotSelection();
            BeginDrag(DragKind.Nodes, evt);
        }

        private void SnapshotSelection()
        {
            dragIndices.Clear();
            dragStartGrounds.Clear();
            dragStartElevations.Clear();
            foreach (var index in state.SelectedIndices)
            {
                dragIndices.Add(index);
                dragStartGrounds.Add(document.Nodes[index].Ground);
                dragStartElevations.Add(document.Nodes[index].Elevation);
            }
        }

        private void HandlePointerMove(PointerMoveEvent evt)
        {
            if (drag == DragKind.None || evt.pointerId != dragPointerId)
            {
                return;
            }

            var pixel = (Vector2)evt.localPosition;
            switch (drag)
            {
                case DragKind.Pan:
                    topView.View.PanPixels += pixel - lastPointer;
                    lastPointer = pixel;
                    topView.Refresh();
                    break;
                case DragKind.Handle:
                    DragHandle(pixel, evt.altKey);
                    break;
                case DragKind.Nodes:
                    DragNodes(pixel, evt.shiftKey);
                    break;
                case DragKind.Marquee:
                    DragMarquee(pixel);
                    break;
                case DragKind.Tool:
                    state.ActiveTool?.PointerMove(ToPointer(pixel, evt.shiftKey, evt.altKey));
                    break;
            }
        }

        private bool HasPassedThreshold(Vector2 pixel)
        {
            dragMoved = dragMoved || Vector2.Distance(pixel, downPixel) >= DragThresholdPixels;
            return dragMoved;
        }

        private void DragHandle(Vector2 pixel, bool broken)
        {
            var index = state.SelectedIndex;
            if (index == MapEditorState.NoSelection)
            {
                return;
            }

            var nodes = document.Nodes;
            var offset = topView.View.PixelToCell(pixel) - nodes[index].Ground;
            var handles = MapCurveMath.GetHandles(nodes, index, document.IsClosed);
            var mode = broken ? MapHandleMode.Broken : nodes[index].HandleMode;
            document.SetHandles(index, MapCurveMath.ApplyGroundDrag(handles, draggedHandleIsOut, offset, mode));
        }

        // The grabbed point follows the pointer (snapped); every selected point moves by the same amount.
        private void DragNodes(Vector2 pixel, bool free)
        {
            if (!HasPassedThreshold(pixel))
            {
                return;
            }

            var anchorSlot = dragIndices.IndexOf(dragAnchor);
            if (state.Mode == MapEditMode.Height)
            {
                var start = dragStartElevations[anchorSlot];
                var target = MapSnapper.SnapElevation(start + (downPixel.y - pixel.y) / PixelsPerElevationCell, free);
                document.ElevateMany(dragIndices, dragStartElevations, target - start);
                return;
            }

            var anchorStart = dragStartGrounds[anchorSlot];
            var cursorDelta = topView.View.PixelToCell(pixel) - topView.View.PixelToCell(downPixel);
            var snapped = MapSnapper.SnapGround(anchorStart + cursorDelta, free);
            document.MoveMany(dragIndices, dragStartGrounds, snapped - anchorStart);
        }

        private void DragMarquee(Vector2 pixel)
        {
            if (!HasPassedThreshold(pixel))
            {
                return;
            }

            topView.SetMarquee(MarqueeRect(pixel));
        }

        private Rect MarqueeRect(Vector2 pixel)
        {
            return Rect.MinMaxRect(Mathf.Min(downPixel.x, pixel.x), Mathf.Min(downPixel.y, pixel.y),
                Mathf.Max(downPixel.x, pixel.x), Mathf.Max(downPixel.y, pixel.y));
        }

        private void HandlePointerUp(PointerUpEvent evt)
        {
            if (drag == DragKind.None || evt.pointerId != dragPointerId)
            {
                return;
            }

            var kind = drag;
            drag = DragKind.None;
            target.ReleasePointer(evt.pointerId);
            CloseGesture();
            if (kind == DragKind.Tool)
            {
                state.ActiveTool?.PointerUp(ToPointer(evt.localPosition, evt.shiftKey, evt.altKey));
            }
            else if (kind == DragKind.Marquee)
            {
                FinishMarquee(evt);
            }
            else if (kind == DragKind.Nodes && !dragMoved && dragWasGroup)
            {
                state.SelectOnly(dragAnchor);
            }
        }

        private void FinishMarquee(PointerUpEvent evt)
        {
            topView.ClearMarquee();
            if (dragMoved)
            {
                var rect = MarqueeRect(evt.localPosition);
                state.SelectMany(MapSelection.IndicesInRect(document.Nodes, topView.View, rect));
                return;
            }

            if (state.PlaceEnabled && state.Mode == MapEditMode.Place)
            {
                state.SelectOnly(AddNode(downPixel, evt.shiftKey));
                return;
            }

            state.ClearSelection();
        }

        private void HandleWheel(WheelEvent evt)
        {
            topView.View.ZoomAt(evt.localMousePosition, Mathf.Pow(ZoomPerWheelUnit, -evt.delta.y));
            topView.Refresh();
            evt.StopPropagation();
        }
    }
}
