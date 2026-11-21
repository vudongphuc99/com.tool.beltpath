using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tool.BeltPath.MapEditor.Editor
{
    /// <summary>
    /// Creates the MapEditor scene in the game project: an empty scene holding one instance of the package's map
    /// editor prefab (menu: BeltPath/Create Map Editor Scene). Re-running it replaces the scene.
    /// </summary>
    public static class MapEditorSceneCreator
    {
        // The scene lives in the game project (the package folder is read-only when installed from git).
        public const string ScenePath = "Assets/MapEditor/MapEditor.unity";

        private const string ParentFolder = "Assets";
        private const string SceneFolderName = "MapEditor";

        [MenuItem("BeltPath/Create Map Editor Scene")]
        public static void Create()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapEditorPrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Map editor prefab not found at {MapEditorPrefabBuilder.PrefabPath}.");
            }

            EnsureSceneFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab, scene);
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
    }
}
