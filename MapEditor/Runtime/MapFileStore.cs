using System;
using System.Collections.Generic;
using System.IO;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Reads and writes map JSON files in one folder (created on first use).</summary>
    public sealed class MapFileStore
    {
        private const string Extension = ".json";
        private const string SearchPattern = "*" + Extension;

        private readonly string folder;

        public MapFileStore(string folder)
        {
            this.folder = folder;
        }

        /// <summary>Map names (file names without extension), sorted.</summary>
        public IReadOnlyList<string> ListNames()
        {
            var names = new List<string>();
            if (!Directory.Exists(folder))
            {
                return names;
            }

            foreach (var path in Directory.GetFiles(folder, SearchPattern))
            {
                names.Add(Path.GetFileNameWithoutExtension(path));
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>Writes the map; returns false and an error text when the name or the disk is bad.</summary>
        public bool TrySave(string name, BeltPathData data, out string error)
        {
            if (!IsValidName(name))
            {
                error = "Invalid file name.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(PathOf(name), data.ToJson());
                error = null;
                return true;
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogException(e);
                error = e.Message;
                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                UnityEngine.Debug.LogException(e);
                error = e.Message;
                return false;
            }
        }

        public bool TryLoad(string name, out BeltPathData data, out string error)
        {
            data = null;
            if (!IsValidName(name) || !File.Exists(PathOf(name)))
            {
                error = "File not found.";
                return false;
            }

            try
            {
                data = BeltPathData.FromJson(File.ReadAllText(PathOf(name)));
                error = data == null ? "File is empty." : null;
                return data != null;
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogException(e);
                error = e.Message;
                return false;
            }
            catch (ArgumentException e)
            {
                // JsonUtility throws ArgumentException on malformed JSON.
                UnityEngine.Debug.LogException(e);
                error = "File is not a valid map.";
                return false;
            }
        }

        private string PathOf(string name)
        {
            return Path.Combine(folder, name + Extension);
        }

        private static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
