# Changelog

## [1.4.0]

- The map editor is now one prefab in the package (`MapEditor/Prefabs/MapEditor.prefab`). `BeltPath/Create Map Editor
  Scene` makes a scene with a single instance of it, so package updates reach every game. Games that have a scene made
  by an older version run the menu again (it replaces the scene; extension components must be added again).
  `BeltPath/Dev/Rebuild Map Editor Prefab` rebuilds the prefab and works only on the embedded package.
- The map editor UI uses one stylesheet: black panel, white buttons and fields with black text, grey when disabled,
  blue for the selected tool.
- New `Open Folder` button opens the folder of the map files (`Output/Maps`), creating it when it does not exist.

- `Water Flow` is now opaque: `Geometry` queue, `ZWrite On`, no alpha blending (cheaper on mobile GPUs, early-Z
  works). The alpha of its `Tint` color no longer has any effect, and the channel floor no longer shows through the
  water, so the Outer/Inner/Center colors may need a small adjustment.

## [1.3.1]

- Effect toggles in `Water Flow` (center band, edge darkening, flow pattern, flow distortion, edge foam, inner current,
  flow arrows) and `Water Caustics` (softening noise, mask noise). A switched-off effect is removed from the shader
  variant (`shader_feature_local`), so it costs no GPU time. All toggles are on by default, and a material saved
  before this version keeps every effect. `BeltWater.mat` now has distortion, edge foam and arrows off, because their
  strength was already 0.
- Noise coordinates of the water shaders are built in the vertex shader and interpolated, instead of per pixel.
- `BeltPeriodicWeb` takes the square root twice instead of nine times (same result).
- `Belt Shadow` computes its color once per vertex instead of per pixel.
- To turn all caustics off, leave `Water Caustics Material` empty on `BeltView`.

## [1.2.1]

- Water shader optimization for mobile GPUs: `BeltHash` and `BeltHash2` no longer use `sin` (noise shape is kept, the
  random pattern is different), `Water Flow` shares one noise between the inner current and the edge foam (3 noise
  calls per pixel instead of 4), and color math in `Water Flow` and `Water Caustics` uses `half`.
- Shaders in a game that include `BeltWaterNoise.cginc` get the new hash automatically.

## [1.2.0]

- Fake belt shadow: `BeltShadow` shader (color, brightness, saturation, opacity, edge softness), a shadow strip
  mesh that follows the belt height, `BeltMeshSettings` shadow spread and X/Y/Z offset, and a `Shadow Material` slot on
  `BeltView` (empty = no shadow).

## [1.1.3]

- Fix: the `v1.1.2` tag shipped with `package.json` still at 1.1.1, so the Package Updater kept offering the update
  after installing it. Use 1.1.3 or later.
- The 3D view takes 40% of the screen and the tool panel 60%, which gives the top view more room.
- Compact map editor panel: smaller rows, gaps, fonts and elevation profile; image controls share the Save/Load row.
- Fix text contrast: panel text is white on the dark background; buttons, inputs and dropdowns keep dark text.
- Place is a toggle button at the start of the toolbar; while it is off, the Closed/Insert/Handles/Auto controls are hidden.
- Test: `PackageVersionTests` checks that `package.json` and the newest `CHANGELOG.md` entry have the same version.

## [1.1.1]

- `BeltPath/Package Updater` window: shows the installed and latest package version (newest `vX.Y.Z` git tag) and
  updates a git-installed project to the latest tag with one click.

## [1.1.0]

- Map editor extension API (one belt per map): `MapEditorExtension` components listed on `MapEditorController` can
  add UI sections, top-view layers and click tools, keep extra data that is covered by undo/redo and saved in the map
  file (`IMapDataExtension`, `IMapFileFormat` with a `BeltPathData` subclass), and handle keys. See "Extending the map
  editor" in the README. Scenes without extensions behave as in 1.0.0.
- `MapHistory` records extension state and has a public `RecordChange()`; `MapFileStore` takes an `IMapFileFormat`
  (the old constructor still works).
- Fix: `BeltView.BuiltData` is no longer serialized, so a script reload cannot restore it as an empty path (error
  "A belt path needs at least 2 nodes" after compiling).
- Fix: `MapEditorController.OnDisable` no longer touches a `BeltView` that was already destroyed.
- Test: `BeltPathTests.GameMatchTolerance` raised to 0.025 (float rounding exceeded 0.02).

## [1.0.0]

- First release as a Unity package: belt path data and spline builder, channel and water meshes, `BeltView`,
  `BeltRider`, camera fitter, scene authoring, and the standalone map editor.


