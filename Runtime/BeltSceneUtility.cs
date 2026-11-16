using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>Creates and destroys generated scene objects, in both edit mode and play mode.</summary>
    public static class BeltSceneUtility
    {
        public static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        /// <summary>Extra materials beyond the single submesh are drawn as additional passes over it.</summary>
        public static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, params Material[] materials)
        {
            var go = CreateChild(parent, name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        public static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
