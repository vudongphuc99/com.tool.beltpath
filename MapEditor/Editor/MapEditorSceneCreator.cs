using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tool.BeltPath.MapEditor.Editor
{
    /// <summary>Creates the MapEditor scene and its PanelSettings asset (menu: BeltPath/Create Map Editor Scene).</summary>
    public static class MapEditorSceneCreator
    {
        // The scene lives in the game project (the package folder is read-only when installed from git).
        public const string ScenePath = "Assets/MapEditor/MapEditor.unity";

        private const string ParentFolder = "Assets";
        private const string SceneFolderName = "MapEditor";
        private const string PanelSettingsPath = ParentFolder + "/" + SceneFolderName + "/MapEditorPanelSettings.asset";
        private const string DefaultThemeFilter = "UnityDefaultRuntimeTheme t:ThemeStyleSheet";
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

        [MenuItem("BeltPath/Create Map Editor Scene")]
        public static void Create()
        {
            EnsureSceneFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera();
            var fitter = camera.GetComponent<BeltCameraFitter>();
            var view = CreateBeltView(CreatePlaneRoot());
            var document = CreateUiDocument();
            CreateController(camera, fitter, view, document);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder($"{ParentFolder}/{SceneFolderName}"))
            {
                AssetDatabase.CreateFolder(ParentFolder, SceneFolderName);
            }
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject(CameraName) { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(90f, 0f, 0f));
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            go.AddComponent<BeltCameraFitter>();
            return camera;
        }

        private static Transform CreatePlaneRoot()
        {
            var go = new GameObject(PlaneRootName);
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

        private static UIDocument CreateUiDocument()
        {
            var go = new GameObject("MapEditorUI");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = LoadOrCreatePanelSettings();
            return document;
        }

        private static void CreateController(Camera camera, BeltCameraFitter fitter, BeltView view, UIDocument document)
        {
            var go = new GameObject("MapEditorController");
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
            if (existing != null)
            {
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            var themeGuids = AssetDatabase.FindAssets(DefaultThemeFilter);
            if (themeGuids.Length > 0)
            {
                settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(themeGuids[0]));
            }

            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            return settings;
        }
    }
}
