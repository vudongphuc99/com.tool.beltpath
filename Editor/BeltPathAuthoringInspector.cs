using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tool.BeltPath.Editor
{
    /// <summary>Inspector for <see cref="BeltPathAuthoring"/> with the apply, save and reload buttons and usage notes.</summary>
    [CustomEditor(typeof(BeltPathAuthoring))]
    public sealed class BeltPathAuthoringInspector : UnityEditor.Editor
    {
        private const string HelpText =
            "Select this object and use the Spline tool to move knots and handles; drag a knot up or down to change its " +
            "height. Broken, Continuous and Mirrored knots keep their own handles; Auto Smooth and Linear knots are " +
            "plain nodes. Unity's own spline curve passes through the raw nodes. The green line is the path the channel " +
            "really follows, with rounded corners. Edits live in the scene and are lost when the path is reloaded from " +
            "its file, unless you save them with Save Path To File.";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(HelpText, MessageType.Info);

            var authoring = (BeltPathAuthoring)target;
            if (GUILayout.Button("Apply Spline Now"))
            {
                authoring.RebuildFromSpline();
            }

            if (GUILayout.Button("Save Path To File (overwrites)"))
            {
                SaveToFile(authoring);
            }

            if (GUILayout.Button("Reload Path From File (discards edits)"))
            {
                authoring.GetComponent<BeltView>().Build();
            }
        }

        private static void SaveToFile(BeltPathAuthoring authoring)
        {
            var file = authoring.GetComponent<BeltView>().PathFile;
            if (file == null)
            {
                Debug.LogException(new InvalidOperationException("The selected belt has no path file to save to."),
                    authoring);
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(file);
            var message = $"Overwrite {assetPath} with the edited path?\n" +
                          "The file keeps only 'isClosed' and 'nodes'; any other field in it is removed.";
            if (!EditorUtility.DisplayDialog("Save path", message, "Overwrite", "Cancel"))
            {
                return;
            }

            File.WriteAllText(assetPath, authoring.ToPathData().ToJson());
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}
