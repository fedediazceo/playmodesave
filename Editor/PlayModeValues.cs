using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FedeDiaz.ApplyPlayModeValues.Editor
{
    /// <summary>
    /// Core service: captures a component's values during Play Mode and re-applies
    /// them when Play Mode exits. Public so that optional front-ends (context menu,
    /// header button) in other assemblies can drive it without touching internals.
    /// </summary>
    public static class PlayModeValues
    {
        private const string SessionKey = "ApplyPlayModeValues.PendingItems";

        [Serializable]
        private class PendingItem
        {
            public string globalObjectId;
            public string typeAssemblyQualifiedName;
            public string json;
            public List<string> visiblePropertyPaths = new List<string>();
        }

        [Serializable]
        private class PendingItemList
        {
            public List<PendingItem> items = new List<PendingItem>();
        }

        public static bool CanCapture(Component component)
        {
            return Application.isPlaying && IsSceneSaved(component);
        }

        public static bool Capture(Component component)
        {
            if (component == null)
                return false;

            if (!IsSceneSaved(component))
            {
                Debug.LogWarning(
                    $"[PlayModeValues] Cannot capture '{component.name}'. The scene must be saved first.",
                    component);

                return false;
            }

            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(component);

            if (id.identifierType == 0)
            {
                Debug.LogWarning(
                    $"[PlayModeValues] Could not get a valid GlobalObjectId for '{component.name}'.",
                    component);

                return false;
            }

            PendingItem item = new PendingItem
            {
                globalObjectId = id.ToString(),
                typeAssemblyQualifiedName = component.GetType().AssemblyQualifiedName,
                json = EditorJsonUtility.ToJson(component),
                visiblePropertyPaths = GetVisibleSerializedPropertyPaths(component)
            };

            PendingItemList list = LoadPendingItems();

            list.items.RemoveAll(existing => existing.globalObjectId == item.globalObjectId);
            list.items.Add(item);

            SavePendingItems(list);

            return true;
        }

        private static bool IsSceneSaved(Component component)
        {
            if (component == null)
                return false;

            Scene scene = component.gameObject.scene;

            return scene.IsValid() && !string.IsNullOrEmpty(scene.path);
        }

        private static List<string> GetVisibleSerializedPropertyPaths(Object target)
        {
            List<string> paths = new List<string>();

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                    continue;

                paths.Add(iterator.propertyPath);
            }

            return paths;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            ApplyPendingItems();
        }

        private static void ApplyPendingItems()
        {
            PendingItemList list = LoadPendingItems();

            if (list.items.Count == 0)
                return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Play Mode Values");

            int applied = 0;

            foreach (PendingItem item in list.items)
            {
                if (ApplyPendingItem(item))
                    applied++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            SessionState.EraseString(SessionKey);

            if (applied > 0)
                Debug.Log($"[PlayModeValues] Applied Play Mode values to {applied} component(s).");
        }

        private static bool ApplyPendingItem(PendingItem item)
        {
            if (!GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId id))
            {
                Debug.LogWarning($"[PlayModeValues] Invalid GlobalObjectId: {item.globalObjectId}");
                return false;
            }

            Object targetObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);

            if (targetObject == null)
            {
                Debug.LogWarning($"[PlayModeValues] Could not resolve object from GlobalObjectId: {item.globalObjectId}");
                return false;
            }

            if (targetObject is not Component targetComponent)
            {
                Debug.LogWarning($"[PlayModeValues] Resolved object is not a Component: {targetObject.name}");
                return false;
            }

            Type componentType = Type.GetType(item.typeAssemblyQualifiedName);

            if (componentType == null)
            {
                Debug.LogWarning($"[PlayModeValues] Could not resolve component type: {item.typeAssemblyQualifiedName}");
                return false;
            }

            GameObject tempGameObject = null;

            try
            {
                tempGameObject = new GameObject("__ApplyPlayValuesTemp__");
                tempGameObject.hideFlags = HideFlags.HideAndDontSave;

                //reuse the one in transform if it exists, otherwise add a new one
                Component tempComponent = tempGameObject.GetComponent(componentType);

                if (tempComponent == null)
                    tempComponent = tempGameObject.AddComponent(componentType);

                if (tempComponent == null)
                {
                    Debug.LogWarning($"[PlayModeValues] Could not create a temporary '{componentType.Name}' to apply values.");
                    return false;
                }

                EditorJsonUtility.FromJsonOverwrite(item.json, tempComponent);

                SerializedObject sourceSerializedObject = new SerializedObject(tempComponent);
                SerializedObject targetSerializedObject = new SerializedObject(targetComponent);

                sourceSerializedObject.Update();
                targetSerializedObject.Update();

                Undo.RecordObject(targetComponent, "Apply Play Mode Values");

                foreach (string propertyPath in item.visiblePropertyPaths)
                {
                    SerializedProperty sourceProperty = sourceSerializedObject.FindProperty(propertyPath);

                    if (sourceProperty == null)
                        continue;

                    SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);

                    if (targetProperty == null)
                        continue;

                    targetSerializedObject.CopyFromSerializedProperty(sourceProperty);
                }

                targetSerializedObject.ApplyModifiedProperties();

                PrefabUtility.RecordPrefabInstancePropertyModifications(targetComponent);

                Scene scene = targetComponent.gameObject.scene;

                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                if (tempGameObject != null)
                    Object.DestroyImmediate(tempGameObject);
            }
        }

        private static PendingItemList LoadPendingItems()
        {
            string json = SessionState.GetString(SessionKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return new PendingItemList();

            try
            {
                PendingItemList list = JsonUtility.FromJson<PendingItemList>(json);
                return list ?? new PendingItemList();
            }
            catch
            {
                return new PendingItemList();
            }
        }

        private static void SavePendingItems(PendingItemList list)
        {
            string json = JsonUtility.ToJson(list);
            SessionState.SetString(SessionKey, json);
        }
    }
}
