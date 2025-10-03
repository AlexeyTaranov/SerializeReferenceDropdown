using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow
{
    public partial class SearchToolWindow
    {
        private interface IUnityObjectLoader
        {
            public Object LoadEditorObject();
        }

        private class ScriptableObjectNode : SearchToolData.ScriptableObjectData, IUnityObjectLoader
        {
            public Object LoadEditorObject()
            {
                return AssetDatabase.LoadAssetAtPath<Object>(AssetPath);
            }

            public ScriptableObjectNode(SearchToolData.ScriptableObjectData scriptableObjectData)
            {
                AssetPath = scriptableObjectData.AssetPath;
            }
        }

        private class GameObjectNode : SearchToolData.PrefabData
        {
            public GameObjectNode(string assetPath) : base(assetPath)
            {
            }
        }

        private class ComponentNode : SearchToolData.UnityObjectReferenceData, IUnityObjectLoader,
            SearchToolData.IAssetData
        {
            public long FileId { get; set; }
            public string AssetPath { get; set; }

            public ComponentNode(SearchToolData.UnityObjectReferenceData objectRefData)
            {
                RefPropertiesData = objectRefData.RefPropertiesData;
                RefIdsData = objectRefData.RefIdsData;
            }

            public bool IsHaveCrossReferences()
            {
                var groups = RefPropertiesData.GroupBy(t => t.assignedReferenceId);
                if (groups.Any(t => t.Count() > 1))
                {
                    return true;
                }

                return false;
            }

            public Object LoadEditorObject()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
                if (prefab == null)
                {
                    Log.DevError($"Load null prefab - {AssetPath}");
                    return null;
                }

                var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var component in components)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out var localId))
                    {
                        if (FileId == localId)
                        {
                            return component;
                        }
                    }
                }

                Log.DevError($"Can't find component in prefab - {AssetPath}");
                return null;
            }
        }

        private class SerializeReferenceNode
        {
            public IUnityObjectLoader ObjectLoader;
            public SearchToolData.ReferencePropertyData PropertyData;
        }
    }
}