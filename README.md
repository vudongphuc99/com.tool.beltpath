# BeltPath

Turns a list of waypoints into a smooth belt line that objects can travel along, and builds the channel and water
meshes for it. Namespace `Tool.BeltPath`. Requires the `com.unity.splines` package (2.x) and Built-in RP shaders.

## Install

Unity Package Manager package `com.tool.beltpath` (Unity 6000.3+). Its dependencies (`com.unity.splines`,
`com.unity.inputsystem`) are installed automatically.

From git, add one line to the game's `Packages/manifest.json` (pin a tag so the game does not break when the package changes):

```json
"com.tool.beltpath": "https://github.com/vudongphuc99/com.tool.beltpath.git#v1.0.0"
```

If the repo is private, use the SSH form `git@github.com:vudongphuc99/com.tool.beltpath.git#v1.0.0` (Unity cannot ask for a password).

### Updating

Open `BeltPath/Package Updater`. It shows the installed version and the newest `vX.Y.Z` tag of the git repository; when a
newer tag exists, **Update** changes the game's `manifest.json` to that tag and Unity reinstalls the package. It needs
`git` on the PATH and only works for projects that install the package from git (not embedded or local copies).

## Developing the package

The source of truth is this folder, embedded in the `BeltPathDev` Unity project (`BeltPathDev/Packages/com.tool.beltpath`).
Edit and test it there (add `"testables": ["com.tool.beltpath"]` to that project's `Packages/manifest.json` to run the
package tests in Test Runner). Games install a tagged copy from git and never edit it.

To release a change: raise `version` in `package.json`, add an entry to `CHANGELOG.md`, commit, create an annotated tag
`vX.Y.Z` and push both.

Materials and `BeltMeshSettings` ship with the pack; `BeltView` fills them in when you add the component.

## Input

JSON:

```json
{ "isClosed": false, "nodes": [ { "x": 0, "y": 0 }, { "x": 20, "y": 0, "elevation": 2 }, { "x": 20, "y": 10 } ] }
```

- `x`, `y`: grid cells, `y` grows downwards. `elevation`: height in cells.
- Optional custom Bezier handles per node: `hasCustomTangent`, `tangentIn`, `tangentOut` (absolute grid positions),
  `elevTangentIn`, `elevTangentOut` (absolute elevations).

## Output and usage

```csharp
var data = BeltPathData.FromJson(jsonText);   // or new BeltPathData(nodes, closed)
var path = BeltPath.Build(data, new BeltShapeSettings());   // throws ArgumentException with < 2 nodes

transform.localPosition = path.Position(distance);   // distance in world units; wraps if closed, clamps if open
transform.forward       = path.Forward(distance);
// path.Length, path.IsClosed, path.Points (polyline), path.Spline

var channel = BeltSweepMeshBuilder.BuildChannel(path, shape, meshSettings);
var water   = BeltSweepMeshBuilder.BuildWaterSurface(path, shape, meshSettings);
```

Or add **BeltView** to a GameObject, assign a JSON `TextAsset`, and read `view.Path`. Path positions are in the
local space of the BeltView object.

## Editing in the Scene view

Add **BeltPathAuthoring** to the BeltView object (it also adds a `SplineContainer`). The spline gets one knot per path
node; select the object and use Unity's Spline tool to move knots and handles (drag a knot up or down to change its
height). The channel and water rebuild while you drag, in Edit Mode, and after undo/redo. The green gizmo is the path
the channel really follows, with rounded corners.

- Broken, Continuous and Mirrored knots become nodes with custom handles; Auto Smooth and Linear knots are plain nodes.
- `Live Edit` off: rebuild only when you press **Apply Spline Now**.
- **Save Path To File (overwrites)** writes `isClosed` and `nodes` into the BeltView's JSON file (other fields are
  removed). **Reload Path From File** discards the edits.
- Edits are stored in the scene's spline and win over the file when the scene is opened.
- In code: `authoring.ToPathData()` returns the edited `BeltPathData`.

## Map Editor (standalone tool for level designers)

Menu `BeltPath/Create Map Editor Scene` creates `Assets/MapEditor/MapEditor.unity` in the game project (the package folder is read-only when installed from git); `BeltPath/Build Map Editor (Windows)`
builds `Builds/MapEditor/BeltMapEditor.exe`. The screen is split in two:

- **A (left)**: display only, the belt (channel + water) under a tilted `PlaneRoot` (see Camera framing), rebuilt after every edit.
- **B (right)**: the tool. Top view of the ground plane (x, z in cells), an elevation profile below it, and a toolbar.

| Input | Action |
|---|---|
| Left click on empty space | Add a point at the end of the belt (snapped to the grid); with the place tool off (`1`) it only clears the selection |
| Left drag on empty space | Box select: every point inside the box is selected |
| Drag a selected point | Move the whole selection (Height mode: change all their elevations); a click selects only that point |
| `1` / **Place (1)** | Turn the place tool (adding points on click) on or off |
| Hold `C` (or the **Insert (C)** toggle), then click | A red ghost follows the nearest spot of the drawn belt; the click inserts a point exactly there (elevation taken from the belt) |
| `Ctrl`/`Cmd` + `Z`, `Ctrl`/`Cmd` + `Y` (or `Shift`+`Z`), **Undo / Redo** | Undo and redo (up to 100 steps). A whole mouse drag is one step; New and Load can be undone too |
| `T` / **Handles (T)** | Show or hide the curve handles and, for the selected point, the handle options (off by default; hiding only affects display and editing, not the belt) |
| Drag a handle (selected point) | Shape the curve; the other handle stays opposite. Hold `Alt` to move one side only |
| `=` / `-` (or **Scale + / Scale -**) | Scale the selected points (2 or more) up / down by 10% around the center of the selection (`Shift`: 1%). Place mode scales the ground positions, Height mode scales the elevations |
| `R` / **Auto (R)** | Return the selected points to automatic smoothing |
| Left drag a point | Move it (Place mode) or change its elevation (Height mode) |
| Right click a point / `Del` | Delete it (all selected points when several are selected) |
| Hold `Shift` | Free placement, no snapping (ground step 1 cell, elevation step 0.5 cell) |
| `H` | Toggle Place / Height mode |
| Drag a point up or down in the profile | Change its elevation along the whole belt |
| Drag a handle in the profile | Change the slope at the point (`Alt`: one side only) |
| Middle drag / wheel | Pan / zoom the top view |
| `Ctrl+S` | Save |

Click a single point and the **Point #N** row under the toolbar shows its X, Y (height) and Z; type new values
and press Enter to apply them exactly (no snapping, height clamped to 0-10). Shortcuts are ignored while a field has focus.
Points are colored by elevation (blue low, orange high) and labelled `index e=height`. Points are stored as ground
positions; saving converts them to level grid coordinates with the plane shift of the scene's `BeltView` shape
(`grid.y = ground.y - shift * elevation`). Maps are JSON files in `Output/Maps` next to the .exe (the project folder
in the editor), in the same format as `BeltPathData`. Reference image: copy .png/.jpg files into `Output/Images` (created next to the .exe), pick one in the third toolbar
row and press **Load Image**. It is drawn under the grid of the top view only, from ground (0, 0), 40 cells wide, and
follows pan and zoom; **Opacity** and **Show** adjust it, **Clear** removes it. Handle options (three rows under the point coordinates, shown with the handles): a **mode** dropdown - `Auto` (smoothed
automatically), `Aligned` (handles on one line, own lengths), `Mirrored` (opposite and equally long), `Broken`
(independent; holding `Alt` while dragging acts as Broken) - the **X/Y/Z offset** of the In and Out handle from the
point, and the **Len**gth of each handle (direction kept). The mode is not stored in the file; it is worked out from
the handles when a map is loaded. Curve handles are shown when exactly one point is selected and **Handles (T)** is on (faded while the point is automatic; dragging one makes the point
custom) and are saved as `hasCustomTangent` with `tangentIn/Out` and `elevTangentIn/Out`. Out of scope:
several belts per map. The image, tool toggles and view are not part of undo.

## Extending the map editor

A game adds its own parts to the map editor without changing the package: add components that derive from
`MapEditorExtension` to the Map Editor scene and drag them into the `extensions` list of `MapEditorController`. With
an empty list the editor behaves as before. The map is still one belt (`MapDocument`).

| Need | API |
|---|---|
| Own UI | `MapEditorExtension.Attach(MapEditorContext)`: `context.Panel.AddSection(element)` puts a row above the top view; `Panel.RegisterTextEditor(field)` stops the editor shortcuts while that field has focus |
| Draw on the top view | Implement `IMapTopViewLayer`, then `context.TopView.AddLayer(this)` (remove it in `Detach`). Layers are painted after the belt path and before the points and handles |
| Click tool | Implement `IMapTopViewTool` and set `context.State.ActiveTool`. While it is set, left and right clicks go to it (`MapPointer`: pixel, cell, button, `Shift`, `Alt`); pan (middle drag) and zoom (wheel) stay with the editor. `PointerMove` is called only while a button is held |
| Own keys | Override `HandleKeyboard(Keyboard)`; return true when a key was used and the editor skips its own shortcuts that frame. It is not called while the user types in a text field |
| Extra data with undo | Implement `IMapDataExtension` (`CaptureState` / `RestoreState`, `WriteTo` / `ReadFrom`, `ClearData`). After changing the data call `context.NotifyDataChanged()`: it makes one undo step (or joins the open gesture, see `History.BeginGesture`) and refreshes the editor. New, Load, Save, Undo and Redo call the interface for you |
| Extra data in the map file | Implement `IMapFileFormat` (at most one extension; more than one throws in `OnEnable`). `Create` wraps the belt in a subclass of `BeltPathData` with extra `[SerializeField]` fields, `Parse` reads it back. The JSON stays flat: `{isClosed, nodes, ...your fields}` |
| React to changes | `Refresh()` runs after every change of the map, the editor state or the extension data |

Minimal extension with a toggle that turns on a click tool:

```csharp
public sealed class PinExtension : MapEditorExtension, IMapTopViewTool
{
    private MapEditorContext context;

    public override void Attach(MapEditorContext editorContext)
    {
        context = editorContext;
        var toggle = new Toggle("Pin tool");
        toggle.RegisterValueChangedCallback(evt => context.State.ActiveTool = evt.newValue ? this : null);
        context.Panel.AddSection(toggle);
    }

    public void PointerDown(MapPointer pointer) { context.SetStatus($"Clicked cell {pointer.Cell}"); }
    public void PointerMove(MapPointer pointer) { }
    public void PointerUp(MapPointer pointer) { }
}
```

### Using it in a game

1. Install version 1.1.0 or newer (see Install). Run the menu `BeltPath/Create Map Editor Scene` to create the editor
   scene in the game (`Assets/MapEditor/`).
2. Write the extension in the game's own code. If that code is in an assembly definition, add the references
   `Tool.BeltPath`, `Tool.BeltPath.MapEditor` and `Unity.InputSystem` to it; code without an assembly definition needs
   nothing.
3. In the Map Editor scene, add the extension component to any GameObject (for example a new `Extensions` object) and
   drag it into the `extensions` list of `MapEditorController`. Add one component per extension; the order of the list
   is the order of `Attach`, `Refresh`, key handling and layer drawing.
4. Press Play. The game's UI row shows above the top view, and Save, Load, New, Undo and Redo include the extension
   data.

Extra data in the map file needs three parts: a file class, a format, and the data extension. The file class derives
from `BeltPathData`, is `[Serializable]`, has a parameterless constructor, and passes the belt on in a second
constructor. One component can implement both `IMapFileFormat` and `IMapDataExtension`:

```csharp
[Serializable]
public sealed class QuackLevelFile : BeltPathData
{
    [SerializeField] private int duckCount;

    public QuackLevelFile() { }                                   // JsonUtility needs it
    public QuackLevelFile(BeltPathData path) : base(path.Nodes, path.IsClosed) { }

    public int DuckCount { get => duckCount; set => duckCount = value; }
}

public sealed class DuckExtension : MapEditorExtension, IMapFileFormat, IMapDataExtension
{
    private MapEditorContext context;
    private int duckCount;

    public override void Attach(MapEditorContext editorContext) { context = editorContext; /* build UI */ }

    // IMapFileFormat: the file type of the map
    public BeltPathData Create(BeltPathData path) { return new QuackLevelFile(path); }
    public BeltPathData Parse(string json) { return JsonUtility.FromJson<QuackLevelFile>(json); }

    // IMapDataExtension: undo/redo, save, load, New
    public string CaptureState() { return duckCount.ToString(); }
    public void RestoreState(string state) { duckCount = int.Parse(state); }
    public void WriteTo(BeltPathData file) { ((QuackLevelFile)file).DuckCount = duckCount; }
    public void ReadFrom(BeltPathData file) { duckCount = ((QuackLevelFile)file).DuckCount; }
    public void ClearData() { duckCount = 0; }
}
```

When the UI changes `duckCount`, set the field first and then call `context.NotifyDataChanged()`. Maps saved before
the extension existed have no extra fields; `JsonUtility` leaves them at their defaults, so `ReadFrom` must accept that.
Only one extension per editor may implement `IMapFileFormat`.

Undo and redo restore the extension data before the editor refreshes, so `Refresh()` always sees the restored data.
A complete example with a click tool, a layer, data and a file format is `MarkerExtension.cs` in the BeltPathDev
development project (`Assets/DevExtensions/`); it is not shipped with the package.

## Camera framing

**Matching the reference game.** Its camera is flat and orthographic. The board lies on a plane tilted 45 degrees about
the screen X axis and stretched by sqrt(2) along the screen Y axis (`PlaneRoot`, rotation X 45, scale Y 1.414); a
raised point moves along the plane's normal, which lifts it by 0.7071 cells up the screen, and walls and ducks stand
along that normal, so their sides stay visible. Reproduce it with a parent of the `BeltView` that has rotation X 45 and
scale Z sqrt(2), a camera looking straight down and `ElevationPlaneShift = 0`. The map editor scene is built that way.

Add **BeltCameraFitter** to the camera (top-down, orthographic) and call
`fitter.SnapTo(bounds)` with the belt's world bounds (`BeltView.TryGetRenderBounds`). It picks the smallest
orthographic size and the camera position that keep the whole belt on screen with `Padding` (world units) and never
below `Min Ortho Size`; it solves again when the screen size changes. The bounds are framed by their world `x` and `z`
and the camera is centered on that rectangle. The math is in `BeltCameraFitMath`. The map editor refits after every edit.

## Objects on the belt (ducks)

Add **BeltRider** to an object, assign the `BeltView`, and set `Speed Cells Per Second` (default 24 = the reference game's `BeltMoveCellsPerSecond`; 0 = stand still). It
follows `BeltView.Path` and faces the travel direction in the space of the belt. Under a tilted `PlaneRoot` the duck
inherits the tilt of the reference game, so `Tilt Degrees` stays 0; with a flat BeltView set it to 45
(`BeltCameraFacing.TryGetRotation`). Put the model on a child; a model whose front is -Z (`yazi_01`, beak at -Z) needs
a local rotation of Y = 180 on that child.

How the reference game does it (read from the game's code and prefabs): its camera is flat and orthographic. Ducks
stand on a plane tilted 45 degrees about the screen X axis (`PlaneRoot`, rotation X 45, scale Y 1.414), the duck's
up axis is that plane's normal, and its yaw is set from the travel direction on the screen (the beak points along it,
no extra stretch). In this package's axes (ground XZ, screen up = +Z, camera looking down -Y) the pose is
`Rx(45 degrees) * LookRotation(direction on the ground)`, which the tilted parent supplies. A duck that stands still
in a pool or slot only needs rotation X = 45. The game's non-uniform plane scale (which slightly shears ducks heading diagonally) is not
reproduced; ducks use a uniform scale.

## Algorithm

1. **Grid to world** (`BeltGridSpace`): `world = (x, e, -(y + shift * e)) * cellSize`; `y` grows downwards and `e` is
   the height above the board (positive = raised). `shift` (`ElevationPlaneShift`, default 0) is only for source data
   that stores nodes already displaced by their elevation; set it to 0 for plain data and for the reference game.
   The reference game's `y` grows UP and its elevation is raised when positive, so convert its levels with
   `y' = maxY - y` (handle y too) and keep the elevation (`belts_beltpath/` was exported that way). The game only
   applies a node's custom handles when their mode is 1; mode 0 handles are ignored. Handle offsets carry no shift.
2. **Spline** (`BeltSplineBuilder`):
   - a node with custom handles becomes a `Broken` Bezier knot;
   - a node without handles that forms a right angle (`|dot(in, out)| < 0.09`) becomes two knots forming a circular
     arc: `cut = min(radius, legIn, legOut)` (a leg shared with another corner is split in half), handle length
     `cut * 0.5523`. The default radius is 2.5 cells, the value baked into the reference game's belt paths; this
     formula reproduces the game's baked paths of levels 1-100 to within 0.015 cells (the game also extends an open belt
     by one cell at a portal, which is not built here);
   - a node lying exactly where a neighbouring arc starts or ends is dropped (no zero-length curve);
   - every other node is an `AutoSmooth` knot.
3. **Arc-length table** (`BeltPath`): 48 samples per curve, cumulative distance, binary search. `Position(d)` lerps
   between samples; `Forward(d)` is the normalized difference of positions at `d +/- 0.05`.
4. **Meshes** (`BeltSweepMeshBuilder`): a 2D profile (right, up) is swept along the path every `SampleStepCells`.
   The channel profile (`BeltChannelProfile`) is a U with round wall crests and filleted wall feet; the water is a flat
   strip at `WaterDepthCells`. The default dimensions (water 2.3, wall 0.5 thick and 0.5 high, water surface 0.18 above
   the floor, in cells) are those of the reference game's channel prefab; the game's walls are boxes with flat tops,
   this package keeps round crests. Normals are smoothed across bends under 40 degrees. `UV.v` is the path distance
   (closed belts round the repeat count so tiling has no seam); `UV2.x` is the height inside the profile
   (0 floor, 1 crest), used by the channel shader.

## Contents

| Folder | Purpose |
|---|---|
| `Runtime/` | Data, spline builder, `BeltPath`, mesh builders, `BeltView`, `BeltPathAuthoring`, `BeltNodeSplineConverter`, `BeltCameraFitter`, `BeltCameraFitMath`, `BeltRider`, `BeltCameraFacing` (assembly `Tool.BeltPath`) |
| `Editor/` | Authoring inspector, edit-mode rebuild updater, and the package updater window (`PackageUpdaterWindow`, `GitTagReader`, `InstalledPackageReader`) (assembly `Tool.BeltPath.Editor`) |
| `Shaders/` | `BeltPath/Water Flow`, `BeltPath/Water Caustics`, `BeltPath/Channel Wall` |
| `Materials/` | Channel, water and caustics materials using the pack shaders |
| `Settings/` | `BeltMeshSettings.asset`, the default channel and water dimensions |
| `MapEditor/Runtime/` | Standalone map editor: `MapDocument`, `MapEditorController`, top view and profile elements, `MapFileStore`, and the extension API (`MapEditorExtension`, `MapEditorContext`, `IMapDataExtension`, `IMapFileFormat`, `IMapTopViewLayer`, `IMapTopViewTool`) (assembly `Tool.BeltPath.MapEditor`) |
| `MapEditor/Editor/` | Scene creator and the Windows build menu (assembly `Tool.BeltPath.MapEditor.Editor`) |
| `Tests/Editor/` | EditMode tests (assembly `Tool.BeltPath.Tests`); `Data/figure_eight.json` is the test sample (closed figure-eight belt, 7 custom-handle nodes) |

## Notes

- `BeltView` falls back to flat unlit materials and default `BeltMeshSettings` if a slot is cleared.
- Editing `BeltMeshSettings` in the inspector rebuilds every enabled `BeltView` that uses it (editor only).
- Inside a namespace that is a child of `Tool`, the name `BeltPath` can resolve to the namespace; qualify it as
  `Tool.BeltPath.BeltPath` there.
