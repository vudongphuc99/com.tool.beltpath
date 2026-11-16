using UnityEditor;

namespace Tool.BeltPath.Editor
{
    /// <summary>
    /// Rebuilds edited belts from the editor loop, so the channel follows the knots while they are dragged
    /// whether or not the scene's <c>Update</c> happens to run, and again after undo and redo.
    /// </summary>
    [InitializeOnLoad]
    public static class BeltPathAuthoringUpdater
    {
        static BeltPathAuthoringUpdater()
        {
            EditorApplication.update += ApplyPendingRebuilds;
            Undo.undoRedoPerformed += RequestRebuilds;
        }

        private static void ApplyPendingRebuilds()
        {
            var instances = BeltPathAuthoring.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                instances[i].ApplyPendingRebuild();
            }
        }

        private static void RequestRebuilds()
        {
            var instances = BeltPathAuthoring.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                instances[i].RequestRebuild();
            }
        }
    }
}
