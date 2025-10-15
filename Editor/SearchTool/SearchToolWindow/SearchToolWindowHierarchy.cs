using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow
{
    public partial class SearchToolWindow
    {
        private void CreateHierarchyTree()
        {
            var errorIcon = EditorGUIUtility.IconContent("d_console.erroricon").image;
            var warningIcon = EditorGUIUtility.IconContent("d_console.warnicon").image;
            var prefabIcon = EditorGUIUtility.IconContent("d_Prefab Icon").image;
            var soIcon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image;
            var scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image;
            var refIcon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow@2x").image;

            var tree = rootVisualElement.Q<TreeView>("unity-objects");
            tree.makeItem = MakeHierarchyElement;
            tree.bindItem = BindUnityObjectItem;
            tree.selectionChanged += SelectItem;
            tree.itemsChosen += ItemChosen;

            rootVisualElement.Q<Button>("unity-objects-fast-check").clicked += () => SetUnityObjectsCheck(false);
            rootVisualElement.Q<Button>("unity-objects-reference-check").clicked += () => SetUnityObjectsCheck(true);
            rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name")
                .RegisterValueChangedCallback(evt => RefreshTree());

            void SetUnityObjectsCheck(bool isActiveChecks)
            {
                checkUnityObjects = isActiveChecks;
                RefreshTree();
            }

            VisualElement MakeHierarchyElement()
            {
                var root = new VisualElement();
                var imageSize = 22;
                var typeImage = new Image()
                {
                    name = "type-image",
                    style = { maxWidth = imageSize, minWidth = imageSize, minHeight = imageSize, maxHeight = imageSize }
                };
                var fixCrossReferencesImage = new Image()
                {
                    name = "fix-cross-refs",
                    style = { maxWidth = imageSize, minWidth = imageSize, minHeight = imageSize, maxHeight = imageSize }
                };
                fixCrossReferencesImage.tooltip = "Asset has cross references";
                var missingTypeImage = new Image()
                {
                    name = "missing-types",
                    style = { maxWidth = imageSize, minWidth = imageSize, minHeight = imageSize, maxHeight = imageSize }
                };
                missingTypeImage.tooltip = "Asset has missing types";
                var label = new Label() { style = { flexGrow = 1 } };
                root.Add(typeImage);
                root.Add(label);
                root.Add(fixCrossReferencesImage);
                root.Add(missingTypeImage);
                root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                root.style.alignItems = new StyleEnum<Align>(Align.Center);
                return root;
            }

            void BindUnityObjectItem(VisualElement element, int index)
            {
                var itemData = tree.GetItemDataForIndex<object>(index);
                var label = element.Q<Label>();
                var typeIcon = element.Q<Image>("type-image");

                switch (itemData)
                {
                    case ScriptableObjectNode: typeIcon.image = soIcon; break;
                    case GameObjectNode: typeIcon.image = prefabIcon; break;
                    case ComponentNode: typeIcon.image = scriptIcon; break;
                    case SerializeReferenceNode: typeIcon.image = refIcon; break;
                }

                if (itemData is SearchToolData.IAssetData assetData)
                {
                    label.text = Path.GetFileNameWithoutExtension(assetData.AssetPath);
                    label.tooltip = assetData.AssetPath;
                    ApplyAssetTypeData(assetData, element);
                }

                if (itemData is SerializeReferenceNode serializeReferenceNode)
                {
                    label.text =
                        $"{serializeReferenceNode.PropertyData.propertyPath} - {serializeReferenceNode.PropertyData.propertyType?.Name}";
                }

                if (itemData is ComponentNode componentNode)
                {
                    var component = componentNode.LoadEditorObject() as Component;
                    if (component != null)
                    {
                        var path = GetGameObjectPath(component.gameObject);
                        label.text = $"{component.gameObject.name} - {component.GetType().Name}";
                        label.tooltip = path;
                    }
                    
                    
                    string GetGameObjectPath(GameObject obj)
                    {
                        var path = "/" + obj.name;
                        while (obj.transform.parent != null)
                        {
                            obj = obj.transform.parent.gameObject;
                            path = "/" + obj.name + path;
                        }

                        return path.TrimStart('/');
                    }
                }
            }

            void ApplyAssetTypeData(SearchToolData.IAssetData assetData, VisualElement element)
            {
                var fixCrossRefsImage = element.Q<Image>("fix-cross-refs");
                var missingTypesImage = element.Q<Image>("missing-types");

                if (checkUnityObjects == false)
                {
                    fixCrossRefsImage.SetDisplayElement(false);
                    missingTypesImage.SetDisplayElement(false);
                    return;
                }

                var isHaveCrossReferences = assetData.IsHaveCrossReferences();
                fixCrossRefsImage.SetDisplayElement(isHaveCrossReferences);
                fixCrossRefsImage.image = warningIcon;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.AssetPath);
                bool haveMissingTypes = false;
                if (asset.GetType().IsAssignableFrom(typeof(ScriptableObject)) ||
                    asset.GetType().IsAssignableFrom(typeof(Component)))
                {
                    haveMissingTypes = SerializationUtility.HasManagedReferencesWithMissingTypes(asset);
                }

                if (asset is GameObject)
                {
                    using var editingScope = new PrefabUtility.EditPrefabContentsScope(assetData.AssetPath);
                    var components = editingScope.prefabContentsRoot.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var component in components)
                    {
                        if (SerializationUtility.HasManagedReferencesWithMissingTypes(component))
                        {
                            haveMissingTypes = true;
                            break;
                        }
                    }
                }

                missingTypesImage.image = errorIcon;
                missingTypesImage.SetDisplayElement(haveMissingTypes);
            }

            void ItemChosen(IEnumerable<object> objects)
            {
                var selectedObject = objects.FirstOrDefault();
                if (selectedObject is SearchToolData.IAssetData assetData)
                {
                    var unityObject = AssetDatabase.LoadAssetAtPath<Object>(assetData.AssetPath);
                    Selection.activeObject = unityObject;
                    EditorGUIUtility.PingObject(unityObject);
                }
            }

            void SelectItem(IEnumerable<object> objects)
            {
                var selectedObject = objects.FirstOrDefault();
                object targetReferenceCopyObject = null;

                if (selectedObject is SerializeReferenceNode serializeReferenceNode)
                {
                    var asset = serializeReferenceNode.ObjectLoader.LoadEditorObject();
                    if (asset != null)
                    {
                        var referenceObject = ManagedReferenceUtility.GetManagedReference(asset,
                            serializeReferenceNode.PropertyData.assignedReferenceId);
                        if (referenceObject != null)
                        {
                            targetReferenceCopyObject = TypeUtils.CreateObjectFromType(referenceObject.GetType());
                            var json = JsonUtility.ToJson(referenceObject);
                            JsonUtility.FromJsonOverwrite(json, targetReferenceCopyObject);
                        }
                    }
                }

                rootVisualElement.Q<Label>("edit-property-label").text = targetReferenceCopyObject?.GetType().Name;
                temp.data = targetReferenceCopyObject;
            }
        }

        private void RefreshTree()
        {
            var tree = rootVisualElement.Q<TreeView>("unity-objects");
            tree.Clear();
            tree.SetRootItems(BuildTree());
            tree.RefreshItems();
        }

        private List<TreeViewItemData<object>> BuildTree()
        {
            var searchFilter = rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name").value
                ?.ToLowerInvariant();

            int referencesCount = 0;

            var rootTree = new List<TreeViewItemData<object>>();
            var targetSos = lastSearchData.SOsData.Where(t => IsInFilterAsset(t.AssetPath));
            foreach (var soData in targetSos)
            {
                var refs = new List<TreeViewItemData<object>>();
                var soNode = new ScriptableObjectNode(soData);
                var soNodeTreeItem = new TreeViewItemData<object>(soData.AssetPath.GetHashCode(), soNode, refs);
                foreach (var referencePropertyData in soData.RefPropertiesData)
                {
                    if (IsTargetType(referencePropertyData, soData))
                    {
                        var refHash = HashCode.Combine(soNodeTreeItem.id, referencePropertyData.assignedReferenceId);
                        refs.Add(new TreeViewItemData<object>(refHash, new SerializeReferenceNode()
                            { ObjectLoader = soNode, PropertyData = referencePropertyData }));
                        referencesCount++;
                    }
                }

                if (refs.Count > 0)
                {
                    rootTree.Add(soNodeTreeItem);
                }
            }

            var targetPrefabs = lastSearchData.PrefabsData.Where(t => IsInFilterAsset(t.AssetPath));
            foreach (var prefabData in targetPrefabs)
            {
                var components = new List<TreeViewItemData<object>>();
                var prefabNode = new GameObjectNode(prefabData.AssetPath);
                var prefabNodeItem = new TreeViewItemData<object>(prefabData.AssetPath.GetHashCode(), prefabNode, components);
                foreach (var prefabComponentData in prefabData.componentsData)
                {
                    var refs = new List<TreeViewItemData<object>>();
                    var componentNode = new ComponentNode(prefabComponentData)
                        { AssetPath = prefabData.AssetPath, FileId = prefabComponentData.FileId };
                    var componentHash =
                        HashCode.Combine(prefabData.AssetPath.GetHashCode(), prefabComponentData.FileId);
                    var componentNodeItem = new TreeViewItemData<object>(componentHash, componentNode, refs);
                    foreach (var referencePropertyData in prefabComponentData.RefPropertiesData)
                    {
                        if (IsTargetType(referencePropertyData, prefabComponentData))
                        {
                            var refHash = HashCode.Combine(componentHash, referencePropertyData.assignedReferenceId);
                            refs.Add(new TreeViewItemData<object>(refHash, new SerializeReferenceNode()
                                { ObjectLoader = componentNode, PropertyData = referencePropertyData }));
                            referencesCount++;
                        }
                    }

                    if (refs.Count > 0)
                    {
                        components.Add(componentNodeItem);
                    }
                }

                if (components.Count > 0)
                {
                    rootTree.Add(prefabNodeItem);
                }
            }
            
            rootVisualElement.Q<Label>("target-type-references-count").text = referencesCount.ToString();

            return rootTree;

            bool IsInFilterAsset(string path)
            {
                if (string.IsNullOrEmpty(searchFilter))
                {
                    return true;
                }

                var assetName = Path.GetFileName(path);
                return assetName.Contains(searchFilter);
            }
        }

        private bool IsTargetType(SearchToolData.ReferencePropertyData propertyData,
            SearchToolData.UnityObjectReferenceData objectData)
        {
            var refIdIndex =
                objectData.RefIdsData.FindIndex(idData => idData.referenceId == propertyData.assignedReferenceId);
            if (refIdIndex >= 0)
            {
                var isAssignable = selectedType.IsAssignableFrom(objectData.RefIdsData[refIdIndex].objectType);
                return isAssignable;
            }

            return false;
        }
    }
}