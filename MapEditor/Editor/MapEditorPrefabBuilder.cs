using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor.Editor
{
    /// <summary>
    /// Builds the map editor prefab and its PanelSettings inside the package (menu: BeltPath/Dev/Rebuild Map Editor
    /// Prefab). Only works on the embedded package; games install the package read-only and just use the prefab.
    /// </summary>
    public static class MapEditorPrefabBuilder
    {
        public const string PrefabPath = PrefabFolder + "/MapEditor.prefab";

        private const string PackageRoot = "Packages/com.tool.beltpath";
        private const string MapEditorFolder = PackageRoot + "/MapEditor";
        private const string PrefabFolderName = "Prefabs";
        private const string PrefabFolder = MapEditorFolder + "/" + PrefabFolderName;
        private const string PanelSettingsPath = PrefabFolder + "/MapEditorPanelSettings.asset";
        private const string ThemePath = PackageRoot + "/MapEditor/UI/MapEditorTheme.tss";
        private const string RootName = "MapEditor";
        private const string CameraName = "Main Camera";
        private const float CameraHeight = 30f;
        private const float OrthographicSize = 12f;
        private static readonly Vector3 CameraPosition = new Vector3(10f, CameraHeight, -8f);
        private static readonly Color CameraBackground = new Color(0.19f, 0.3f, 0.47f);

        // The reference game draws the board on a plane tilted 45 degrees about the screen X axis and stretched by
        // sqrt(2) along the screen Y axis, seen by a flat camera (its PlaneRoot).
        private const string PlaneRootName = "PlaneRoot";
        private const float PlaneTiltDegrees = 45f;
        private static readonly float PlaneDepthStretch = Mathf.Sqrt(2f);

        [MenuItem("BeltPath/Dev/Rebuild Map Editor Prefab")]
        public static void Rebuild()
        {
            EnsurePrefabFolder();
            var root = new GameObject(RootName);
            try
            {
                var camera = CreateCamera(root.transform);
                var view = CreateBeltView(CreatePlaneRoot(root.transform));
                var document = CreateUiDocument(root.transform);
                CreateController(root.transform, camera, camera.GetComponent<BeltCameraFitter>(), view, document);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("BeltPath/Dev/Rebuild Map Editor Prefab", true)]
        public static bool CanRebuild()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageRoot);
            return package != null && package.source == PackageSource.Embedded;
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder(MapEditorFolder, PrefabFolderName);
            }
        }

        private static Camera CreateCamera(Transform parent)
        {
            var go = new GameObject(CameraName) { tag = "MainCamera" };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(90f, 0f, 0f));
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            go.AddComponent<BeltCameraFitter>();
            return camera;
        }

        private static Transform CreatePlaneRoot(Transform parent)
        {
            var go = new GameObject(PlaneRootName);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(PlaneTiltDegrees, 0f, 0f);
            go.transform.localScale = new Vector3(1f, 1f, PlaneDepthStretch);
            return go.transform;
        }

        private static BeltView CreateBeltView(Transform parent)
        {
            var go = new GameObject("BeltView");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var view = go.AddComponent<BeltView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("buildOnEnable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);
            return view;
        }

        private static UIDocument CreateUiDocument(Transform parent)
        {
            var go = new GameObject("MapEditorUI");
            go.transform.SetParent(parent, false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = LoadOrCreatePanelSettings();
            return document;
        }

        private static void CreateController(Transform parent, Camera camera, BeltCameraFitter fitter, BeltView view, UIDocument document)
        {
            var go = new GameObject("MapEditorController");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var controller = go.AddComponent<MapEditorController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("beltView").objectReferenceValue = view;
            serialized.FindProperty("viewCamera").objectReferenceValue = camera;
            serialized.FindProperty("cameraFitter").objectReferenceValue = fitter;
            serialized.FindProperty("uiDocument").objectReferenceValue = document;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);
        }

        private static PanelSettings LoadOrCreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var settings = existing != null ? existing : ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }
            else
            {
                EditorUtility.SetDirty(settings);
            }

            return settings;
        }
    }
}
