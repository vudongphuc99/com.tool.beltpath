# Changelog

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
  
