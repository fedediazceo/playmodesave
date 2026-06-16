using UnityEditor;
using UnityEngine;

namespace FedeDiaz.ApplyPlayModeValues.Editor
{
    /// <summary>
    /// Adds "Apply Play Mode Values" to every component's context (gear) menu.
    /// Pure public API — no internal Unity dependencies.
    /// </summary>
    public static class ApplyPlayModeContextMenu
    {
        // "CONTEXT/Component" attaches the entry to every component's gear menu
        // (Transform, Rigidbody, colliders, you name it).
        private const string ContextMenuPath = "CONTEXT/Component/Apply Play Mode Values";

        [MenuItem(ContextMenuPath, false, 1000)]
        private static void ApplyPlayValuesMenu(MenuCommand command)
        {
            if (command.context is not Component component)
                return;

            if (PlayModeValues.Capture(component))
                Debug.Log(
                    $"[PlayModeValues] Captured '{component.name}'. Values will be applied when exiting Play Mode.",
                    component);
        }

        [MenuItem(ContextMenuPath, true)]
        private static bool ApplyPlayValuesMenuValidate(MenuCommand command)
        {
            return command.context is Component component
                && PlayModeValues.CanCapture(component);
        }
    }
}
