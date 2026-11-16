using UnityEngine;
using UnityEngine.InputSystem;

namespace Tool.BeltPath.MapEditor
{
    /// <summary>
    /// Base of a game-specific part of the map editor. Add the component to the scene and drag it into the
    /// <c>extensions</c> list of <see cref="MapEditorController"/>. Implement <see cref="IMapDataExtension"/>,
    /// <see cref="IMapFileFormat"/>, <see cref="IMapTopViewLayer"/> or <see cref="IMapTopViewTool"/> on the same class
    /// to add data, a file type, drawing or a click tool.
    /// </summary>
    public abstract class MapEditorExtension : MonoBehaviour
    {
        /// <summary>Called once the editor is built: add UI sections, layers, and subscribe to events here.</summary>
        public abstract void Attach(MapEditorContext context);

        /// <summary>Called before the editor shuts down: undo what <see cref="Attach"/> did.</summary>
        public virtual void Detach()
        {
        }

        /// <summary>Called after every change of the map, the editor state or the extension data.</summary>
        public virtual void Refresh()
        {
        }

        /// <summary>Returns true when the extension used a key; the editor then skips its own shortcuts this frame.</summary>
        public virtual bool HandleKeyboard(Keyboard keyboard)
        {
            return false;
        }
    }
}
