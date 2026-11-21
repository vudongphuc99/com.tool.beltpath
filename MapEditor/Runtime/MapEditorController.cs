using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Composition root of the map editor. The left 40% of the screen is the camera view of the belt (A); the right
    /// 60% is the tool panel (B). Every change of the map rebuilds the belt and redraws the panel.
    /// </summary>
    public sealed class MapEditorController : MonoBehaviour
    {
        private const float ViewWidthFraction = 0.4f;
        private const float PanelWidthPercent = 60f;
        private const int MinBeltNodes = 2;
        private const int MinScaleCount = 2;
        private const string OutputFolderName = "Output";
        private const string MapsFolderName = "Maps";
        private const string ImagesFolderName = "Images";
        private const string DefaultMapName = "map";

        [SerializeField] private BeltView beltView;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private BeltCameraFitter cameraFitter;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private MapEditorExtension[] extensions = System.Array.Empty<MapEditorExtension>();

        private readonly MapDocument document = new MapDocument();
        private readonly MapEditorState state = new MapEditorState();
        private readonly MapViewTransform viewTransform = new MapViewTransform();
        private readonly List<Vector3> pathGround = new List<Vector3>();

        private MapFileStore store;
        private MapHistory history;
        private MapImageStore imageStore;
        private Texture2D referenceTexture;
        private MapEditorPanel panel;
        private TopViewElement topView;
        private ElevationProfileElement profile;
        private MapEditorExtensionSet extensionSet;
        private MapEditorContext context;

        private void OnEnable()
        {
            viewCamera.rect = new Rect(0f, 0f, ViewWidthFraction, 1f);
            extensionSet = new MapEditorExtensionSet(extensions);
            store = new MapFileStore(ResolveOutputFolder(MapsFolderName), extensionSet.FileFormat);
            imageStore = new MapImageStore(ResolveOutputFolder(ImagesFolderName));
            imageStore.EnsureFolder();
            history = new MapHistory(document, extensionSet.DataExtensions);
            history.Changed += HandleHistoryChanged;
            BuildPanel();
            AttachExtensions();
            document.Changed += HandleDocumentChanged;
            state.Changed += HandleStateChanged;
            panel.SetMapNames(store.ListNames());
            panel.SetImageNames(imageStore.ListNames());
            topView.SetReferenceOpacity(panel.ImageOpacity);
            topView.SetReferenceVisible(panel.ImageVisible);
            RefreshAll();
        }

        private void OnDisable()
        {
            if (history == null)
            {
                // OnEnable failed before the editor was built (for example two file formats).
                return;
            }

            DetachExtensions();
            history.Changed -= HandleHistoryChanged;
            history.Detach();
            ReleaseReferenceImage();
            document.Changed -= HandleDocumentChanged;
            state.Changed -= HandleStateChanged;
            if (beltView != null)
            {
                // Closing the scene can destroy the view before this controller is disabled.
                beltView.Clear();
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || history == null)
            {
                return;
            }

            var typing = IsTypingInTextField();
            state.InsertHeld = !typing && state.PlaceEnabled && keyboard.cKey.isPressed;
            if (typing || extensionSet.HandleKeyboard(keyboard))
            {
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                ToggleMode();
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SetPlaceEnabled(!state.PlaceEnabled);
            }

            var scaleDirection = ReadScaleDirection(keyboard);
            if (scaleDirection != 0)
            {
                var step = keyboard.shiftKey.isPressed ? MapSnapper.ScaleFineStep : MapSnapper.ScaleStep;
                ScaleSelection(scaleDirection > 0 ? step : 1f / step);
            }

            if (state.PlaceEnabled)
            {
                HandlePlaceToolKeys(keyboard);
            }

            if (HandleUndoRedoKeys(keyboard))
            {
                return;
            }

            if (keyboard.sKey.wasPressedThisFrame && keyboard.ctrlKey.isPressed)
            {
                Save(panel.MapName);
            }
        }

        // Shortcuts that edit the belt: they only work while the place tool is on.
        private void HandlePlaceToolKeys(Keyboard keyboard)
        {
            if (keyboard.tKey.wasPressedThisFrame)
            {
                state.HandlesVisible = !state.HandlesVisible;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ClearSelectedHandles();
            }

            if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
            {
                DeleteSelected();
            }
        }

        private static string ResolveOutputFolder(string subFolder)
        {
            // Application.dataPath is <project>/Assets in the editor and <app>_Data in a build: its parent is the
            // project folder or the folder holding the .exe.
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, OutputFolderName, subFolder);
        }

        private void BuildPanel()
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            topView = new TopViewElement(document, state, viewTransform);
            topView.AddManipulator(new TopViewManipulator(document, state, topView, history));
            profile = new ElevationProfileElement(document, state, history);
            panel = new MapEditorPanel(topView, profile);
            panel.style.position = Position.Absolute;
            panel.style.right = 0f;
            panel.style.top = 0f;
            panel.style.bottom = 0f;
            panel.style.width = Length.Percent(PanelWidthPercent);
            panel.MapName = DefaultMapName;
            root.Add(panel);
            WirePanelEvents();
        }

        private void WirePanelEvents()
        {
            panel.NewClicked += HandleNewClicked;
            panel.ModeToggleClicked += ToggleMode;
            panel.AutoClicked += ClearSelectedHandles;
            panel.ScaleClicked += ScaleSelection;
            panel.InsertToggled += HandleInsertToggled;
            panel.ClosedChanged += document.SetClosed;
            panel.SaveClicked += Save;
            panel.LoadClicked += Load;
            panel.OpenFolderClicked += HandleOpenFolderClicked;
            panel.PlaceToggled += HandlePlaceToggled;
            panel.HandlesToggled += HandleHandlesToggled;
            panel.PointEdited += HandlePointEdited;
            panel.UndoClicked += PerformUndo;
            panel.RedoClicked += PerformRedo;
            panel.HandleOptionChosen += HandleOptionChosen;
            panel.HandleOffsetEdited += HandleOffsetEdited;
            panel.HandleLengthEdited += HandleLengthEdited;
            panel.ImageLoadClicked += LoadReferenceImage;
            panel.ImageClearClicked += ReleaseReferenceImage;
            panel.OpacityChanged += topView.SetReferenceOpacity;
            panel.ImageVisibleChanged += topView.SetReferenceVisible;
        }

        private void AttachExtensions()
        {
            context = new MapEditorContext(document, state, history, panel, topView, beltView);
            extensionSet.Attach(context);
            context.DataChanged += RefreshPanel;
        }

        private void DetachExtensions()
        {
            context.DataChanged -= RefreshPanel;
            state.ActiveTool = null;
            extensionSet.Detach();
        }

        // The extension data changes before the document: the document change refreshes the editor, which must see it.
        private void HandleOpenFolderClicked()
        {
            if (store.EnsureFolder())
            {
                Application.OpenURL(new System.Uri(store.Folder).AbsoluteUri);
            }
        }

        private void HandleNewClicked()
        {
            state.ClearSelection();
            history.RunAsOneStep(() =>
            {
                extensionSet.ClearData();
                document.Clear();
            });
            panel.SetStatus("New map.");
        }

        private void HandleDocumentChanged()
        {
            state.PruneSelection(document.Nodes.Count);
            RefreshAll();
        }

        private void HandleStateChanged()
        {
            RefreshPanel();
        }

        private void RefreshAll()
        {
            RebuildBelt();
            RefreshPanel();
        }

        private void RebuildBelt()
        {
            pathGround.Clear();
            if (document.Nodes.Count < MinBeltNodes)
            {
                beltView.Clear();
                return;
            }

            beltView.Build(document.ToPathData(beltView.Shape));
            CollectPathGround();
            FitCamera();
        }

        private void FitCamera()
        {
            if (cameraFitter != null && beltView.TryGetRenderBounds(out var bounds))
            {
                cameraFitter.SnapTo(bounds);
            }
        }

        // Ctrl or Cmd with Z undoes (with Shift: redoes) and with Y redoes; true when a key was handled.
        private bool HandleUndoRedoKeys(Keyboard keyboard)
        {
            var modifier = keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed
                           || keyboard.rightCommandKey.isPressed;
            if (!modifier)
            {
                return false;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                if (keyboard.shiftKey.isPressed)
                {
                    PerformRedo();
                }
                else
                {
                    PerformUndo();
                }

                return true;
            }

            if (!keyboard.yKey.wasPressedThisFrame)
            {
                return false;
            }

            PerformRedo();
            return true;
        }

        private void PerformUndo()
        {
            var done = history.Undo();
            state.ClearSelection();
            panel.SetStatus(done ? "Undo." : "Nothing to undo.");
        }

        private void PerformRedo()
        {
            var done = history.Redo();
            state.ClearSelection();
            panel.SetStatus(done ? "Redo." : "Nothing to redo.");
        }

        private void HandleHistoryChanged()
        {
            panel.SetUndoRedoEnabled(history.CanUndo, history.CanRedo);
        }

        private void HandleOptionChosen(MapHandleOption option)
        {
            var index = state.SelectedIndex;
            if (index == MapEditorState.NoSelection)
            {
                return;
            }

            if (option == MapHandleOption.Auto)
            {
                if (document.Nodes[index].HasHandles)
                {
                    document.ClearHandles(index);
                }

                return;
            }

            document.SetHandleMode(index, ToHandleMode(option));
        }

        private static MapHandleMode ToHandleMode(MapHandleOption option)
        {
            switch (option)
            {
                case MapHandleOption.Mirrored:
                    return MapHandleMode.Mirrored;
                case MapHandleOption.Broken:
                    return MapHandleMode.Broken;
                default:
                    return MapHandleMode.Aligned;
            }
        }

        private void HandleOffsetEdited(bool isOut, Vector3 offset)
        {
            EditSelectedHandles((handles, mode) => MapCurveMath.ApplyOffset(handles, isOut, offset, mode));
        }

        private void HandleLengthEdited(bool isOut, float length)
        {
            EditSelectedHandles((handles, mode) => MapCurveMath.ApplyLength(handles, isOut, length, mode));
        }

        // Applies an edit to the handles of the selected point, in the handle mode of that point.
        private void EditSelectedHandles(System.Func<MapHandles, MapHandleMode, MapHandles> edit)
        {
            var index = state.SelectedIndex;
            if (index == MapEditorState.NoSelection)
            {
                return;
            }

            var handles = MapCurveMath.GetHandles(document.Nodes, index, document.IsClosed);
            document.SetHandles(index, edit(handles, document.Nodes[index].HandleMode));
        }

        private void HandlePointEdited(Vector3 coordinates)
        {
            var index = state.SelectedIndex;
            if (index != MapEditorState.NoSelection)
            {
                document.SetPosition(index, new Vector2(coordinates.x, coordinates.z), coordinates.y);
            }
        }

        private void HandleHandlesToggled(bool on)
        {
            state.HandlesVisible = on;
        }

        private void HandlePlaceToggled(bool on)
        {
            SetPlaceEnabled(on);
        }

        // Turning the place tool off makes the views read-only: nothing stays selected and insert mode is dropped.
        private void SetPlaceEnabled(bool on)
        {
            state.PlaceEnabled = on;
            if (on)
            {
                return;
            }

            state.ClearSelection();
            state.InsertLatched = false;
        }

        private void LoadReferenceImage(string fileName)
        {
            if (!imageStore.TryLoad(fileName, out var texture, out var error))
            {
                panel.SetStatus($"Image failed: {error}");
                return;
            }

            ReleaseReferenceImage();
            referenceTexture = texture;
            topView.SetReferenceImage(texture);
            panel.SetStatus($"Image {fileName} loaded.");
        }

        // The texture is created by this controller, so it is destroyed here.
        private void ReleaseReferenceImage()
        {
            if (topView != null)
            {
                topView.SetReferenceImage(null);
            }

            BeltSceneUtility.DestroyObject(referenceTexture);
            referenceTexture = null;
        }

        private void HandleInsertToggled(bool on)
        {
            state.InsertLatched = on;
        }

        // Belt path points are in world units (x right, y = elevation, -z = ground y; see BeltGridSpace). The top
        // view gets (ground x, elevation, ground y) in cells.
        private void CollectPathGround()
        {
            var cellSize = beltView.Shape.CellSize;
            foreach (var point in beltView.Path.Points)
            {
                pathGround.Add(new Vector3(point.x, point.y, -point.z) / cellSize);
            }
        }

        private void RefreshPanel()
        {
            topView.SetPath(pathGround);
            topView.Refresh();
            profile.Refresh();
            panel.SetClosed(document.IsClosed);
            panel.SetPlaceEnabled(state.PlaceEnabled);
            panel.SetHandlesVisible(state.HandlesVisible);
            RefreshPointInspector();
            RefreshHandleOptions();
            extensionSet.Refresh();
            HandleHistoryChanged();
            panel.SetModeText(state.Mode == MapEditMode.Place ? "Mode: Place (H)" : "Mode: Height (H)");
        }

        private void RefreshPointInspector()
        {
            var index = state.SelectedIndex;
            if (index == MapEditorState.NoSelection || index >= document.Nodes.Count)
            {
                panel.HidePoint();
                return;
            }

            panel.ShowPoint(index, document.Nodes[index]);
        }

        private void RefreshHandleOptions()
        {
            var index = state.SelectedIndex;
            if (!state.HandlesVisible || index == MapEditorState.NoSelection || index >= document.Nodes.Count)
            {
                panel.HideHandleOptions();
                return;
            }

            var count = document.Nodes.Count;
            var closed = document.IsClosed;
            var handles = MapCurveMath.GetHandles(document.Nodes, index, closed);
            panel.ShowHandleOptions(document.Nodes[index], handles,
                MapCurveMath.IsHandleUsed(count, index, false, closed), MapCurveMath.IsHandleUsed(count, index, true, closed));
        }

        private void ToggleMode()
        {
            state.Mode = state.Mode == MapEditMode.Place ? MapEditMode.Height : MapEditMode.Place;
        }

        // +1 for scale up (= or numpad +), -1 for scale down (- or numpad -), 0 when none was pressed this frame.
        private static int ReadScaleDirection(Keyboard keyboard)
        {
            if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
            {
                return 1;
            }

            return keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame ? -1 : 0;
        }

        // Place mode scales the ground positions of the selection, Height mode scales its elevations.
        private void ScaleSelection(float factor)
        {
            var count = state.SelectedIndices.Count;
            if (count < MinScaleCount)
            {
                panel.SetStatus($"Select at least {MinScaleCount} points to scale.");
                return;
            }

            var height = state.Mode == MapEditMode.Height;
            document.ScaleMany(state.SelectedIndices, factor, height);
            panel.SetStatus($"Scaled {count} points x{factor:0.00} ({(height ? "height" : "ground")}).");
        }

        private void ClearSelectedHandles()
        {
            history.RunAsOneStep(() =>
            {
                foreach (var index in new List<int>(state.SelectedIndices))
                {
                    if (document.Nodes[index].HasHandles)
                    {
                        document.ClearHandles(index);
                    }
                }
            });
        }

        private void DeleteSelected()
        {
            var selected = new List<int>(state.SelectedIndices);
            state.ClearSelection();
            document.RemoveRange(selected);
        }

        private void Save(string mapName)
        {
            if (document.Nodes.Count < MinBeltNodes)
            {
                panel.SetStatus($"Need at least {MinBeltNodes} points to save.");
                return;
            }

            var file = extensionSet.FileFormat.Create(document.ToPathData(beltView.Shape));
            extensionSet.WriteTo(file);
            var saved = store.TrySave(mapName, file, out var error);
            panel.SetStatus(saved ? $"Saved Output/Maps/{mapName}.json" : $"Save failed: {error}");
            panel.SetMapNames(store.ListNames());
        }

        private void Load(string mapName)
        {
            if (!store.TryLoad(mapName, out var data, out var error))
            {
                panel.SetStatus($"Load failed: {error}");
                return;
            }

            state.ClearSelection();
            history.RunAsOneStep(() =>
            {
                extensionSet.ReadFrom(data);
                document.FromPathData(data, beltView.Shape);
            });
            panel.MapName = mapName;
            panel.SetStatus($"Loaded {mapName}.");
        }

        private bool IsTypingInTextField()
        {
            var focused = uiDocument.rootVisualElement.panel?.focusController?.focusedElement as VisualElement;
            return focused != null && panel.IsEditingText(focused);
        }
    }
}
