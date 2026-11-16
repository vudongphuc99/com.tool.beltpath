using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tool.BeltPath.MapEditor.Editor
{
    /// <summary>Builds the map editor as a Windows .exe (menu: BeltPath/Build Map Editor (Windows)).</summary>
    public static class MapEditorBuildMenu
    {
        private const string OutputFolder = "Builds/MapEditor";
        private const string ExeName = "BeltMapEditor.exe";

        [MenuItem("BeltPath/Build Map Editor (Windows)")]
        public static void Build()
        {
            if (!File.Exists(MapEditorSceneCreator.ScenePath))
            {
                MapEditorSceneCreator.Create();
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MapEditorSceneCreator.ScenePath },
                locationPathName = Path.Combine(OutputFolder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogException(new System.InvalidOperationException($"Map editor build {report.summary.result}."));
            }
        }
    }
}
