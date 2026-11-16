using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>
    /// Builds the channel and water meshes of one belt from a JSON file or from code, and exposes the
    /// <see cref="BeltPath"/> that objects can follow. Works in edit mode too.
    /// </summary>
    [ExecuteAlways]
    public sealed class BeltView : MonoBehaviour
    {
        private const string GeneratedRootName = "BeltPathGenerated";
        private const string UnlitColorShader = "Unlit/Color";
        private static readonly Color FallbackChannelColor = new Color(0.55f, 0.6f, 0.7f);
        private static readonly Color FallbackWaterColor = new Color(0.35f, 0.7f, 0.95f);

        [SerializeField] private TextAsset pathFile;
        [SerializeField] private bool buildOnEnable = true;
        [SerializeField] private BeltShapeSettings shape = new BeltShapeSettings();
        [SerializeField] private BeltMeshSettings meshSettings;
        [SerializeField] private Material channelMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material waterCausticsMaterial;

        // Everything created at runtime by this view; assets assigned in the inspector must not be destroyed.
        private readonly List<UnityEngine.Object> ownedObjects =
            new List<UnityEngine.Object>();

        /// <summary>Raised after the path file was read and built, with the data that was read.</summary>
        public event Action<BeltPathData> FileLoaded;

        public BeltShapeSettings Shape => shape;

        public TextAsset PathFile => pathFile;

        /// <summary>The built path; null when nothing is built.</summary>
        public BeltPath Path { get; private set; }

        /// <summary>The data the current <see cref="Path"/> was built from; null when nothing is built.</summary>
        [field: NonSerialized]
        public BeltPathData BuiltData { get; private set; }

#if UNITY_EDITOR
        // Pack assets are found relative to this script (Runtime/BeltView.cs), so the pack folder can be moved.
        private const string ChannelMaterialFile = "Materials/BeltChannel.mat";
        private const string WaterMaterialFile = "Materials/BeltWater.mat";
        private const string CausticsMaterialFile = "Materials/BeltWaterCaustics.mat";
        private const string MeshSettingsFile = "Settings/BeltMeshSettings.asset";

        // Called by Unity when the component is added; fills the slots with the materials and settings of the pack.
        private void Reset()
        {
            var packRoot = FindPackRoot();
            if (packRoot == null)
            {
                return;
            }

            channelMaterial = LoadPackAsset<Material>(packRoot, ChannelMaterialFile);
            waterMaterial = LoadPackAsset<Material>(packRoot, WaterMaterialFile);
            waterCausticsMaterial = LoadPackAsset<Material>(packRoot, CausticsMaterialFile);
            meshSettings = LoadPackAsset<BeltMeshSettings>(packRoot, MeshSettingsFile);
        }

        private string FindPackRoot()
        {
            var scriptPath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this));
            if (string.IsNullOrEmpty(scriptPath))
            {
                return null;
            }

            var runtimeFolder = System.IO.Path.GetDirectoryName(scriptPath);
            return System.IO.Path.GetDirectoryName(runtimeFolder)?.Replace('\\', '/');
        }

        private static T LoadPackAsset<T>(string packRoot, string relativePath) where T : UnityEngine.Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>($"{packRoot}/{relativePath}");
        }
#endif

        private void OnEnable()
        {
            if (meshSettings != null)
            {
                meshSettings.Changed += HandleMeshSettingsChanged;
            }

            if (buildOnEnable && pathFile != null)
            {
                BuildFromFile();
            }
        }

        private void OnDisable()
        {
            if (meshSettings != null)
            {
                meshSettings.Changed -= HandleMeshSettingsChanged;
            }

            Clear();
        }

        private void HandleMeshSettingsChanged()
        {
#if UNITY_EDITOR
            // Objects cannot be created or destroyed inside OnValidate, so the rebuild waits for the editor loop.
            UnityEditor.EditorApplication.delayCall += RebuildFromBuiltData;
#endif
        }

        private void RebuildFromBuiltData()
        {
            if (this == null || BuiltData == null)
            {
                return;
            }

            Build(BuiltData);
        }

        [ContextMenu("Build")]
        public void Build()
        {
            if (pathFile == null)
            {
                Debug.LogException(new InvalidOperationException("BeltView has no path file assigned."), this);
                return;
            }

            BuildFromFile();
        }

        private void BuildFromFile()
        {
            var data = BeltPathData.FromJson(pathFile.text);
            Build(data);
            FileLoaded?.Invoke(data);
        }

        /// <exception cref="ArgumentException">The data has fewer than two nodes.</exception>
        public void Build(BeltPathData data)
        {
            Clear();
            Path = BeltPath.Build(data, shape);
            BuiltData = data;
            var settings = ResolveMeshSettings();
            var root = BeltSceneUtility.CreateChild(transform, GeneratedRootName);
            root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var channel = BeltSweepMeshBuilder.BuildChannel(Path, shape, settings);
            var water = BeltSweepMeshBuilder.BuildWaterSurface(Path, shape, settings);
            ownedObjects.Add(channel);
            ownedObjects.Add(water);
            BeltSceneUtility.CreateMeshObject(root.transform, "Channel", channel,
                ResolveMaterial(channelMaterial, FallbackChannelColor));
            BeltSceneUtility.CreateMeshObject(root.transform, "Water", water, WaterMaterials());
        }

        /// <summary>World-space bounds of the built channel and water; false when nothing is built.</summary>
        public bool TryGetRenderBounds(out Bounds bounds)
        {
            bounds = default;
            var root = transform.Find(GeneratedRootName);
            if (root == null)
            {
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                BeltSceneUtility.DestroyObject(existing.gameObject);
            }

            foreach (var owned in ownedObjects)
            {
                BeltSceneUtility.DestroyObject(owned);
            }

            ownedObjects.Clear();
            Path = null;
            BuiltData = null;
        }

        private BeltMeshSettings ResolveMeshSettings()
        {
            if (meshSettings != null)
            {
                return meshSettings;
            }

            var defaults = ScriptableObject.CreateInstance<BeltMeshSettings>();
            ownedObjects.Add(defaults);
            return defaults;
        }

        private Material ResolveMaterial(Material assigned, Color fallbackColor)
        {
            if (assigned != null)
            {
                return assigned;
            }

            var fallback = new Material(Shader.Find(UnlitColorShader))
            {
                color = fallbackColor,
                hideFlags = HideFlags.DontSave
            };
            ownedObjects.Add(fallback);
            return fallback;
        }

        private Material[] WaterMaterials()
        {
            var water = ResolveMaterial(waterMaterial, FallbackWaterColor);
            return waterCausticsMaterial != null ? new[] { water, waterCausticsMaterial } : new[] { water };
        }
    }
}
