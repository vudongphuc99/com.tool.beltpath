using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>Lists and loads the reference images (png, jpg) in one folder.</summary>
    public sealed class MapImageStore
    {
        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

        private readonly string folder;

        public MapImageStore(string folder)
        {
            this.folder = folder;
        }

        /// <summary>Creates the folder so that designers can see where to drop images.</summary>
        public void EnsureFolder()
        {
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (IOException e)
            {
                Debug.LogException(e);
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>File names with extension, sorted.</summary>
        public IReadOnlyList<string> ListNames()
        {
            var names = new List<string>();
            if (!Directory.Exists(folder))
            {
                return names;
            }

            foreach (var path in Directory.GetFiles(folder))
            {
                if (HasImageExtension(path))
                {
                    names.Add(Path.GetFileName(path));
                }
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>Loads an image into a new texture; the caller owns it and must destroy it.</summary>
        public bool TryLoad(string fileName, out Texture2D texture, out string error)
        {
            texture = null;
            if (!IsValidName(fileName) || !File.Exists(Path.Combine(folder, fileName)))
            {
                error = "Image not found.";
                return false;
            }

            try
            {
                return TryDecode(File.ReadAllBytes(Path.Combine(folder, fileName)), out texture, out error);
            }
            catch (IOException e)
            {
                Debug.LogException(e);
                error = e.Message;
                return false;
            }
        }

        private static bool TryDecode(byte[] bytes, out Texture2D texture, out string error)
        {
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            if (ImageConversion.LoadImage(texture, bytes))
            {
                error = null;
                return true;
            }

            BeltSceneUtility.DestroyObject(texture);
            texture = null;
            error = "File is not a valid image.";
            return false;
        }

        private static bool IsValidName(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                   && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && HasImageExtension(fileName);
        }

        private static bool HasImageExtension(string path)
        {
            var extension = Path.GetExtension(path);
            foreach (var allowed in Extensions)
            {
                if (string.Equals(extension, allowed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
