using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FedeDiaz.ApplyPlayModeValues.Editor.Bridge
{
    /// <summary>
    /// OPTIONAL header button. Draws a small save icon in every component header,
    /// next to the preset/help buttons, that captures Play Mode values.
    ///
    /// This relies on the INTERNAL Unity attribute <c>EditorHeaderItem</c>, which is
    /// only reachable from an assembly named "Unity.InternalAPIEditorBridge.NNN"
    /// (see the asmdef next to this file). 
    /// Those NNN slots are a shared, finite pool
    /// and other packages claim them too.
    ///
    /// If you get a compile error about a duplicate assembly name, just open
    /// "Unity.InternalAPIEditorBridge.024.asmdef" and change 024 to another free
    /// number (001-025). Nothing else needs to change.
    ///
    /// If you don't want this button at all, delete this whole HeaderItem folder —
    /// the context-menu entry keeps working without it.
    /// 
    /// Thanks claude for helping with the reflection hard-tasks and the comments
    /// I hate writing comments so much (the next bit is from copilot lol, it scolds me)
    /// but I know they are important for other people to understand my code.
    /// </summary>
    internal static class ApplyPlayModeHeaderItem
    {
        // Unity discovers this by reflection during editor domain reload and draws
        // it in the component header. typeof(Component) targets every component.
        [EditorHeaderItem(typeof(Component))]
        private static bool DrawApplyPlayValuesButton(Rect rect, Object[] targets)
        {
            List<Component> components = GetComponents(targets);

            if (components.Count == 0)
                return false;

            bool canCapture = true;

            foreach (Component component in components)
            {
                if (!PlayModeValues.CanCapture(component))
                {
                    canCapture = false;
                    break;
                }
            }

            string tooltip;

            if (!Application.isPlaying)
                tooltip = "Apply Play Values is only available in Play Mode.";
            else if (!canCapture)
                tooltip = "Save the scene before using Apply Play Values.";
            else
                tooltip = "Capture current Play Mode values and apply them when exiting Play Mode.";

            GUIContent content = EditorGUIUtility.IconContent("SaveActive");
            content.tooltip = tooltip;

            using (new EditorGUI.DisabledScope(!canCapture))
            {
                if (GUI.Button(rect, content, EditorStyles.iconButton))
                {
                    int captured = 0;

                    foreach (Component component in components)
                    {
                        if (PlayModeValues.Capture(component))
                            captured++;
                    }

                    if (captured > 0)
                        Debug.Log($"[PlayModeValues] Captured {captured} component(s). Values will be applied when exiting Play Mode.");
                }
            }
            return true;
        }

        private static List<Component> GetComponents(Object[] targets)
        {
            List<Component> result = new List<Component>();

            if (targets == null)
                return result;

            foreach (Object target in targets)
            {
                if (target is Component component)
                    result.Add(component);
            }

            return result;
        }
    }
}
