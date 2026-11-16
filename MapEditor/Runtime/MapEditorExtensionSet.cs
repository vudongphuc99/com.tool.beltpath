using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>The extensions of one editor: forwards the editor lifecycle to them and gathers their file format and data.</summary>
    public sealed class MapEditorExtensionSet
    {
        private readonly List<MapEditorExtension> extensions = new List<MapEditorExtension>();
        private readonly List<IMapDataExtension> dataExtensions = new List<IMapDataExtension>();

        /// <summary>Empty slots of the list are skipped. Throws when more than one extension is a file format.</summary>
        public MapEditorExtensionSet(IReadOnlyList<MapEditorExtension> source)
        {
            foreach (var extension in source)
            {
                if (extension == null)
                {
                    continue;
                }

                extensions.Add(extension);
                if (extension is IMapDataExtension data)
                {
                    dataExtensions.Add(data);
                }
            }

            FileFormat = ResolveFileFormat(extensions);
        }

        public IMapFileFormat FileFormat { get; }

        public IReadOnlyList<IMapDataExtension> DataExtensions => dataExtensions;

        /// <summary>The one candidate that is a file format, or the default format when there is none.</summary>
        public static IMapFileFormat ResolveFileFormat(IEnumerable<object> candidates)
        {
            IMapFileFormat found = null;
            foreach (var candidate in candidates)
            {
                if (!(candidate is IMapFileFormat format))
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException("Only one map editor extension may implement IMapFileFormat.");
                }

                found = format;
            }

            return found ?? new BeltPathFileFormat();
        }

        public void Attach(MapEditorContext context)
        {
            foreach (var extension in extensions)
            {
                extension.Attach(context);
            }
        }

        public void Detach()
        {
            foreach (var extension in extensions)
            {
                extension.Detach();
            }
        }

        public void Refresh()
        {
            foreach (var extension in extensions)
            {
                extension.Refresh();
            }
        }

        /// <summary>True when an extension used a key this frame.</summary>
        public bool HandleKeyboard(Keyboard keyboard)
        {
            foreach (var extension in extensions)
            {
                if (extension.HandleKeyboard(keyboard))
                {
                    return true;
                }
            }

            return false;
        }

        public void WriteTo(BeltPathData file)
        {
            foreach (var data in dataExtensions)
            {
                data.WriteTo(file);
            }
        }

        public void ReadFrom(BeltPathData file)
        {
            foreach (var data in dataExtensions)
            {
                data.ReadFrom(file);
            }
        }

        public void ClearData()
        {
            foreach (var data in dataExtensions)
            {
                data.ClearData();
            }
        }
    }
}
