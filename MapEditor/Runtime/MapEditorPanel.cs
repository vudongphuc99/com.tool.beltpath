using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>The tool half of the screen (B): toolbar, top view, elevation profile, status line. Layout only; it raises events and the controller decides what they do.</summary>
    public sealed class MapEditorPanel : VisualElement
    {
        private const float ToolbarGapPixels = 6f;
        private const float RowHeightPixels = 28f;
        private const float ProfileHeightPixels = 170f;
        private const float NameFieldWidthPixels = 160f;
        private const int FontSize = 13;
        private const float CoordinateFieldWidthPixels = 150f;
        private const float CoordinateLabelWidthPixels = 62f;
        private const float HandleFieldWidthPixels = 96f;
        private const float HandleLabelWidthPixels = 30f;
        private const float HandleRowTitleWidthPixels = 56f;
        private const float DefaultImageOpacity = 0.5f;
        private const float OpacitySliderWidthPixels = 110f;
        private const string HelpText =
            "Click: add/select   Drag empty area: box select   1: place tool on/off   Hold C / Insert: click a belt to insert a point   Drag: move   Drag handle: curve (Alt: one side)   Ctrl/Cmd+Z / Y: undo / redo   Right click: delete   Middle drag: pan   Wheel: zoom   Shift: free (no snap)   H: height mode   T: show/hide handles   R: auto curve   =/-: scale selection (Shift: fine)   Del: delete   Ctrl+S: save";

        private readonly TextField nameField = new TextField();
        private readonly DropdownField loadDropdown = new DropdownField();
        private readonly Toggle closedToggle = new Toggle("Closed");
        private readonly Toggle insertToggle = new Toggle("Insert (C)");
        private readonly Button modeButton = new Button();
        private readonly Label statusLabel = new Label();
        private readonly Toggle placeToggle = new Toggle("Place (1)") { value = true };
        private readonly Toggle handlesToggle = new Toggle("Handles (T)");
        private readonly Label pointLabel = new Label();
        private readonly FloatField xField = new FloatField("X") { isDelayed = true };
        private readonly FloatField yField = new FloatField("Y (height)") { isDelayed = true };
        private readonly FloatField zField = new FloatField("Z") { isDelayed = true };
        private VisualElement pointRow;
        private readonly Button undoButton = new Button { text = "Undo (Ctrl+Z)" };
        private readonly Button redoButton = new Button { text = "Redo (Ctrl+Y)" };
        private readonly DropdownField handleModeDropdown =
            new DropdownField(new List<string>(Enum.GetNames(typeof(MapHandleOption))), 0);
        private readonly FloatField[] inFields = CreateOffsetFields();
        private readonly FloatField[] outFields = CreateOffsetFields();
        private readonly FloatField inLengthField = new FloatField("Len") { isDelayed = true };
        private readonly FloatField outLengthField = new FloatField("Len") { isDelayed = true };
        private readonly List<VisualElement> textEditors = new List<VisualElement>();
        private VisualElement handleModeRow;
        private VisualElement handleInRow;
        private VisualElement handleOutRow;
        private readonly DropdownField imageDropdown = new DropdownField();
        private readonly Slider opacitySlider = new Slider(0f, 1f) { value = DefaultImageOpacity };
        private readonly Toggle showImageToggle = new Toggle("Show") { value = true };

        public MapEditorPanel(TopViewElement topView, ElevationProfileElement profile)
        {
            TopView = topView;
            Profile = profile;
            style.flexDirection = FlexDirection.Column;
            style.backgroundColor = new Color(0.14f, 0.15f, 0.17f);
            Add(CreateToolbar());
            Add(CreateFileRow());
            Add(CreateImageRow());
            Add(CreatePointRow());
            Add(CreateHandleRows());
            textEditors.Add(nameField);
            topView.style.flexGrow = 1f;
            Add(topView);
            profile.style.height = ProfileHeightPixels;
            Add(profile);
            Add(statusLabel);
            Add(CreateHelp());
            StyleLabel(statusLabel);
        }

        public event Action NewClicked;
        public event Action ModeToggleClicked;
        public event Action AutoClicked;
        public event Action<float> ScaleClicked;
        public event Action<bool> ClosedChanged;
        public event Action<bool> InsertToggled;
        public event Action<bool> PlaceToggled;
        public event Action<bool> HandlesToggled;
        public event Action UndoClicked;
        public event Action RedoClicked;
        public event Action<MapHandleOption> HandleOptionChosen;

        /// <summary>An offset field of a handle was edited: (ground x, elevation, ground y) from the point.</summary>
        public event Action<bool, Vector3> HandleOffsetEdited;
        public event Action<bool, float> HandleLengthEdited;

        /// <summary>The coordinates of the selected point were edited: x = ground x, y = elevation, z = ground y.</summary>
        public event Action<Vector3> PointEdited;
        public event Action<string> ImageLoadClicked;
        public event Action ImageClearClicked;
        public event Action<float> OpacityChanged;
        public event Action<bool> ImageVisibleChanged;
        public event Action<string> SaveClicked;
        public event Action<string> LoadClicked;

        public TopViewElement TopView { get; }

        public ElevationProfileElement Profile { get; }

        public float ImageOpacity => opacitySlider.value;

        public bool ImageVisible => showImageToggle.value;

        public string MapName
        {
            get => nameField.value;
            set => nameField.SetValueWithoutNotify(value);
        }

        /// <summary>True when the element is a text or number field, or inside one (typing must not trigger shortcuts).</summary>
        public bool IsEditingText(VisualElement element)
        {
            foreach (var editor in textEditors)
            {
                if (IsInside(editor, element))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetUndoRedoEnabled(bool canUndo, bool canRedo)
        {
            undoButton.SetEnabled(canUndo);
            redoButton.SetEnabled(canRedo);
        }

        /// <summary>Shows the handle options (mode, offsets, lengths) of the selected point.</summary>
        public void ShowHandleOptions(MapNode node, MapHandles handles, bool inUsed, bool outUsed)
        {
            var option = node.HasHandles ? node.HandleMode.ToString() : MapHandleOption.Auto.ToString();
            handleModeDropdown.SetValueWithoutNotify(option);
            SetOffsetFields(inFields, inLengthField, handles.In);
            SetOffsetFields(outFields, outLengthField, handles.Out);
            handleInRow.SetEnabled(inUsed);
            handleOutRow.SetEnabled(outUsed);
            SetHandleRowsDisplay(DisplayStyle.Flex);
        }

        public void HideHandleOptions()
        {
            SetHandleRowsDisplay(DisplayStyle.None);
        }

        /// <summary>Shows the coordinates of the selected point in the inspector row.</summary>
        public void ShowPoint(int index, MapNode node)
        {
            pointLabel.text = $"Point #{index + 1}";
            xField.SetValueWithoutNotify(node.Ground.x);
            yField.SetValueWithoutNotify(node.Elevation);
            zField.SetValueWithoutNotify(node.Ground.y);
            pointRow.style.display = DisplayStyle.Flex;
        }

        public void HidePoint()
        {
            pointRow.style.display = DisplayStyle.None;
        }

        public void SetStatus(string text)
        {
            statusLabel.text = text;
        }

        public void SetModeText(string text)
        {
            modeButton.text = text;
        }

        public void SetClosed(bool closed)
        {
            closedToggle.SetValueWithoutNotify(closed);
        }

        public void SetHandlesVisible(bool visible)
        {
            handlesToggle.SetValueWithoutNotify(visible);
        }

        public void SetPlaceEnabled(bool enabled)
        {
            placeToggle.SetValueWithoutNotify(enabled);
        }

        public void SetImageNames(IReadOnlyList<string> names)
        {
            imageDropdown.choices = new List<string>(names);
            imageDropdown.SetValueWithoutNotify(names.Count > 0 ? names[0] : string.Empty);
        }

        public void SetMapNames(IReadOnlyList<string> names)
        {
            loadDropdown.choices = new List<string>(names);
            loadDropdown.SetValueWithoutNotify(names.Count > 0 ? names[0] : string.Empty);
        }

        private VisualElement CreateToolbar()
        {
            var row = CreateRow();
            row.Add(CreateButton("New", () => NewClicked?.Invoke()));
            undoButton.clicked += () => UndoClicked?.Invoke();
            undoButton.style.marginRight = ToolbarGapPixels;
            row.Add(undoButton);
            redoButton.clicked += () => RedoClicked?.Invoke();
            redoButton.style.marginRight = ToolbarGapPixels;
            row.Add(redoButton);
            closedToggle.RegisterValueChangedCallback(evt => ClosedChanged?.Invoke(evt.newValue));
            row.Add(closedToggle);
            insertToggle.RegisterValueChangedCallback(evt => InsertToggled?.Invoke(evt.newValue));
            row.Add(insertToggle);
            placeToggle.RegisterValueChangedCallback(evt => PlaceToggled?.Invoke(evt.newValue));
            row.Add(placeToggle);
            handlesToggle.RegisterValueChangedCallback(evt => HandlesToggled?.Invoke(evt.newValue));
            row.Add(handlesToggle);
            modeButton.clicked += () => ModeToggleClicked?.Invoke();
            row.Add(modeButton);
            row.Add(CreateButton("Auto (R)", () => AutoClicked?.Invoke()));
            row.Add(CreateButton("Scale - (-)", () => ScaleClicked?.Invoke(1f / MapSnapper.ScaleStep)));
            row.Add(CreateButton("Scale + (=)", () => ScaleClicked?.Invoke(MapSnapper.ScaleStep)));
            return row;
        }

        private VisualElement CreateFileRow()
        {
            var row = CreateRow();
            nameField.style.width = NameFieldWidthPixels;
            row.Add(nameField);
            row.Add(CreateButton("Save", () => SaveClicked?.Invoke(nameField.value)));
            loadDropdown.style.width = NameFieldWidthPixels;
            row.Add(loadDropdown);
            row.Add(CreateButton("Load", () => LoadClicked?.Invoke(loadDropdown.value)));
            return row;
        }

        private VisualElement CreateImageRow()
        {
            var row = CreateRow();
            imageDropdown.style.width = NameFieldWidthPixels;
            row.Add(imageDropdown);
            row.Add(CreateButton("Load Image", () => ImageLoadClicked?.Invoke(imageDropdown.value)));
            row.Add(CreateButton("Clear", () => ImageClearClicked?.Invoke()));
            opacitySlider.style.width = OpacitySliderWidthPixels;
            opacitySlider.RegisterValueChangedCallback(evt => OpacityChanged?.Invoke(evt.newValue));
            row.Add(opacitySlider);
            showImageToggle.RegisterValueChangedCallback(evt => ImageVisibleChanged?.Invoke(evt.newValue));
            row.Add(showImageToggle);
            return row;
        }

        private VisualElement CreatePointRow()
        {
            pointRow = CreateRow();
            pointRow.style.display = DisplayStyle.None;
            StyleLabel(pointLabel);
            pointRow.Add(pointLabel);
            foreach (var field in new[] { xField, yField, zField })
            {
                field.style.width = CoordinateFieldWidthPixels;
                field.labelElement.style.minWidth = 0f;
                field.labelElement.style.width = CoordinateLabelWidthPixels;
                field.RegisterValueChangedCallback(evt => RaisePointEdited());
                pointRow.Add(field);
                textEditors.Add(field);
            }

            return pointRow;
        }

        private VisualElement CreateHandleRows()
        {
            var container = new VisualElement();
            handleModeRow = CreateRow();
            var title = new Label("Handle");
            StyleLabel(title);
            handleModeRow.Add(title);
            handleModeDropdown.style.width = NameFieldWidthPixels;
            handleModeDropdown.RegisterValueChangedCallback(evt => HandleOptionChosen?.Invoke(ParseOption(evt.newValue)));
            handleModeRow.Add(handleModeDropdown);
            handleInRow = CreateHandleOffsetRow("In", false, inFields, inLengthField);
            handleOutRow = CreateHandleOffsetRow("Out", true, outFields, outLengthField);
            container.Add(handleModeRow);
            container.Add(handleInRow);
            container.Add(handleOutRow);
            SetHandleRowsDisplay(DisplayStyle.None);
            return container;
        }

        private VisualElement CreateHandleOffsetRow(string title, bool isOut, FloatField[] offsets, FloatField length)
        {
            var row = CreateRow();
            var label = new Label(title);
            StyleLabel(label);
            label.style.width = HandleRowTitleWidthPixels;
            row.Add(label);
            foreach (var field in offsets)
            {
                AddHandleField(row, field);
                field.RegisterValueChangedCallback(evt => HandleOffsetEdited?.Invoke(isOut, ReadOffset(offsets)));
            }

            AddHandleField(row, length);
            length.RegisterValueChangedCallback(evt => HandleLengthEdited?.Invoke(isOut, evt.newValue));
            return row;
        }

        private void AddHandleField(VisualElement row, FloatField field)
        {
            field.style.width = HandleFieldWidthPixels;
            field.labelElement.style.minWidth = 0f;
            field.labelElement.style.width = HandleLabelWidthPixels;
            row.Add(field);
            textEditors.Add(field);
        }

        private void SetHandleRowsDisplay(DisplayStyle display)
        {
            handleModeRow.style.display = display;
            handleInRow.style.display = display;
            handleOutRow.style.display = display;
        }

        private static FloatField[] CreateOffsetFields()
        {
            return new[]
            {
                new FloatField("X") { isDelayed = true },
                new FloatField("Y") { isDelayed = true },
                new FloatField("Z") { isDelayed = true }
            };
        }

        private static Vector3 ReadOffset(FloatField[] fields)
        {
            return new Vector3(fields[0].value, fields[1].value, fields[2].value);
        }

        private static void SetOffsetFields(FloatField[] fields, FloatField lengthField, Vector3 offset)
        {
            fields[0].SetValueWithoutNotify(offset.x);
            fields[1].SetValueWithoutNotify(offset.y);
            fields[2].SetValueWithoutNotify(offset.z);
            lengthField.SetValueWithoutNotify(offset.magnitude);
        }

        private static MapHandleOption ParseOption(string name)
        {
            return (MapHandleOption)Enum.Parse(typeof(MapHandleOption), name);
        }

        private void RaisePointEdited()
        {
            PointEdited?.Invoke(new Vector3(xField.value, yField.value, zField.value));
        }

        private static bool IsInside(VisualElement field, VisualElement element)
        {
            return element == field || field.Contains(element);
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = RowHeightPixels;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginLeft = ToolbarGapPixels;
            return row;
        }

        private static Button CreateButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.marginRight = ToolbarGapPixels;
            return button;
        }

        private static Label CreateHelp()
        {
            var help = new Label(HelpText);
            StyleLabel(help);
            help.style.whiteSpace = WhiteSpace.Normal;
            help.style.opacity = 0.6f;
            return help;
        }

        private static void StyleLabel(Label label)
        {
            label.style.fontSize = FontSize;
            label.style.color = Color.white;
            label.style.marginLeft = ToolbarGapPixels;
        }
    }
}
