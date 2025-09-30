using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.EditReferenceType;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public class SearchToolWindow : EditorWindow
    {
        private static SearchToolWindow instance;

        private Type selectedType = typeof(object);
        private SearchToolData lastSearchData;

        private SearchToolWindowTempSO temp;
        private SerializedObject tempSO;

        private SerializedObject soForPropertyEdit;
        private SerializedProperty propertyEdit;

        private ListView unityObjectsListView;

        private ListView refIdsListView;
        private ListView refPropertiesListView;
        private ListView missingTypesListView;

        private ListView componentsListView;

        private Button missingTypesButton;

        private Action saveRefAction;

        private Action editMissingType;

        private Action<SearchToolData.ReferenceIdData> selectRefIdAction;

        private Action<SearchToolData.ReferencePropertyData> selectRefPropertyAction;

        private Action<SearchToolData.PrefabComponentData> selectPrefabComponentDataAction;
        private Action<VisualElement, int> bindItemPrefabComponentDataAction;

        private (SearchToolData.UnityObjectReferenceData referenceData, Object unityObject) selectedUnityData;
        private bool checkUnityObjects;


        #region Window

        public static void ShowSearchTypeWindow(Type type)
        {
            var window = GetOrCreateWindow();
            window.SetNewType(type);
            window.Focus();
        }

        private static SearchToolWindow GetOrCreateWindow()
        {
            if (instance)
            {
                return instance;
            }

            var window = GetWindow<SearchToolWindow>();
            return window;
        }

        private void OnEnable()
        {
            temp = CreateInstance<SearchToolWindowTempSO>();
            tempSO = new SerializedObject(temp);
            CreateUiToolkitLayout();
            var (loadedData, time) = SearchToolWindowAssetDatabase.LoadSearchData();
            if (loadedData != null)
            {
                ApplyAssetDatabase(loadedData, time);
            }

            if (selectedType == null)
            {
                SetNewType(typeof(object));
            }
        }


        private void OnDisable()
        {
            DestroyImmediate(temp);
            tempSO.Dispose();
            tempSO = null;
            temp = null;
        }

        #endregion

        #region Setup layout

        private void CreateUiToolkitLayout()
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SearchTool.uxml");

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            var layout = visualTreeAsset.Instantiate();

            layout.style.flexGrow = 1;
            layout.style.flexShrink = 1;
            rootVisualElement.Add(layout);

            rootVisualElement.Q<Button>("refresh-database").clicked += RefreshAssetsDatabase;
            rootVisualElement.Q<Button>("clear-target-type").clicked += () => SetNewType(typeof(object));
            rootVisualElement.Q<Button>("apply-data").clicked += () => saveRefAction?.Invoke();
            rootVisualElement.Q<Button>("edit-missing-type").clicked += () => editMissingType?.Invoke();
            rootVisualElement.Q<Button>("select-props").clicked += () => SetDisplayPropsOrIDs(true);
            rootVisualElement.Q<Button>("select-ids").clicked += () => SetDisplayPropsOrIDs(false);
            rootVisualElement.Q<Button>("unity-objects-fast-check").clicked += () => SetUnityObjectsCheck(false);
            rootVisualElement.Q<Button>("unity-objects-reference-check").clicked += () => SetUnityObjectsCheck(true);
            missingTypesButton = rootVisualElement.Q<Button>("missing-types");
            missingTypesButton.SetDisplayElement(false);
            missingTypesButton.clicked += SetDisplayMissingTypes;
            rootVisualElement.Q<Toggle>("unity-objects-activate-prefabs")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<Toggle>("unity-objects-activate-scriptableobjects")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<Button>("target-type").clicked += ShowAssignableTypes;
            rootVisualElement.Q<Button>("target-type-open-source").clicked += OpenTargetTypeSourceFile;

            var errorIcon = EditorGUIUtility.IconContent("d_console.erroricon").image;
            var warningIcon = EditorGUIUtility.IconContent("d_console.warnicon").image;
            var prefabIcon = EditorGUIUtility.IconContent("d_Prefab Icon").image;
            var soIcon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image;

            var property = rootVisualElement.Q<PropertyField>("edit-property");
            property.Bind(tempSO);
            property.bindingPath = nameof(temp.data);

            var editObjectRoot = rootVisualElement.Q("edit-data-root");
            var missingDataRoot = rootVisualElement.Q("missing-type-root");

            SetDisplayEditData(true);

            unityObjectsListView = rootVisualElement.Q<ListView>("unity-objects");
            MakeDefaultSingleListView(ref unityObjectsListView);
            unityObjectsListView.makeItem = MakeUnityObjectItem;

            unityObjectsListView.bindItem = BindUnityObjectItem;
            unityObjectsListView.selectionChanged += SelectUnityObject;
            unityObjectsListView.selectionChanged += (t) => SetDisplayEditData(true);
            unityObjectsListView.itemsChosen += ChoseUnityObject;

            refIdsListView = rootVisualElement.Q<ListView>("ref-ids");
            MakeDefaultSingleListView(ref refIdsListView);
            refIdsListView.bindItem = BindRefIdItem;
            refIdsListView.selectionChanged += SelectRefIdItem;

            refPropertiesListView = rootVisualElement.Q<ListView>("ref-props");
            MakeDefaultSingleListView(ref refPropertiesListView);
            refPropertiesListView.bindItem = BindRefPropertyItem;
            refPropertiesListView.makeItem = MakeRefPropertyItem;
            refPropertiesListView.selectionChanged += SelectRefPropertyItem;

            missingTypesListView = rootVisualElement.Q<ListView>("ref-missing");
            MakeDefaultSingleListView(ref missingTypesListView);
            missingTypesListView.bindItem = BindMissingType;
            missingTypesListView.selectionChanged += SelectMissingType;

            componentsListView = rootVisualElement.Q<ListView>("components");
            MakeDefaultSingleListView(ref componentsListView);
            componentsListView.bindItem = (element, i) => bindItemPrefabComponentDataAction?.Invoke(element, i);
            componentsListView.selectionChanged += SelectComponentItem;
            var nonSelectedUnityObject = rootVisualElement.Q<Label>("non-selected-unity-object");
            nonSelectedUnityObject.SetActiveEmptyPlaceholder(true);
            SetDisplayPropsOrIDs(true);

            void SetDisplayPropsOrIDs(bool isProps)
            {
                missingTypesListView.SetDisplayElement(false);
                refPropertiesListView.SetDisplayElement(isProps);
                refIdsListView.SetDisplayElement(isProps == false);
                refIdsListView.ClearSelection();
                refIdsListView.RefreshItems();
                refPropertiesListView.ClearSelection();
                refPropertiesListView.RefreshItems();
                ClearSaveRefId();
            }

            void SetDisplayEditData(bool isEditData)
            {
                missingDataRoot.SetDisplayElement(!isEditData);
                editObjectRoot.SetDisplayElement(isEditData);
            }

            void SetDisplayMissingTypes()
            {
                refPropertiesListView.SetDisplayElement(false);
                refIdsListView.SetDisplayElement(false);
                missingTypesListView.SetDisplayElement(true);
            }

            void MakeDefaultSingleListView(ref ListView listView)
            {
                listView.showBorder = true;
                listView.itemsSource = new List<object>();
                listView.makeItem = () => new Label();
                listView.selectionType = SelectionType.Single;
            }

            void SetUnityObjectsCheck(bool isActiveChecks)
            {
                checkUnityObjects = isActiveChecks;
                unityObjectsListView.RefreshItems();
            }

            VisualElement MakeUnityObjectItem()
            {
                var root = new VisualElement();
                var typeImage = new Image() { name = "type-image", style = { maxWidth = 15 } };
                var fixCrossReferencesImage = new Image() { name = "fix-cross-refs", style = { maxWidth = 15 } };
                fixCrossReferencesImage.tooltip = "Asset has cross references";
                var missingTypeImage = new Image() { name = "missing-types", style = { maxWidth = 15 } };
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

            void BindUnityObjectItem(VisualElement element, int i)
            {
                var label = element.Q<Label>();
                var typeIcon = element.Q<Image>("type-image");
                if (label != null && typeIcon != null &&
                    unityObjectsListView.itemsSource[i] is SearchToolData.IAssetData assetData)
                {
                    label.text = Path.GetFileNameWithoutExtension(assetData.AssetPath);
                    label.tooltip = assetData.AssetPath;
                    var typeImage = assetData is SearchToolData.PrefabData ? prefabIcon : soIcon;
                    typeIcon.image = typeImage;

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
            }

            void ChoseUnityObject(IEnumerable<object> objects)
            {
                var selectedObject = objects.First();
                if (selectedObject is SearchToolData.IAssetData assetData)
                {
                    var unityObject = AssetDatabase.LoadAssetAtPath<Object>(assetData.AssetPath);
                    Selection.activeObject = unityObject;
                    EditorGUIUtility.PingObject(unityObject);
                }
            }

            void BindRefIdItem(VisualElement element, int i)
            {
                if (element is Label label &&
                    refIdsListView.itemsSource[i] is SearchToolData.ReferenceIdData referenceData)
                {
                    label.text = $"{referenceData.objectType.Name}";
                    label.tooltip = $"Type: {referenceData.objectType.FullName}\n" +
                                    $"ID: {referenceData.referenceId}";
                }
            }

            void BindMissingType(VisualElement element, int i)
            {
                if (element is Label label &&
                    missingTypesListView.itemsSource[i] is ManagedReferenceMissingType missingTypeData)
                {
                    label.text = missingTypeData.className;
                    label.tooltip = $"ASM: {missingTypeData.assemblyName}\n" +
                                    $"Namespace: {missingTypeData.namespaceName}\n" +
                                    $"Classname: {missingTypeData.className}";
                }
            }

            void SelectRefIdItem(IEnumerable<object> objects)
            {
                SetDisplayEditData(true);

                var obj = objects.FirstOrDefault();
                if (obj is SearchToolData.ReferenceIdData referenceData)
                {
                    selectRefIdAction?.Invoke(referenceData);
                }
            }

            VisualElement MakeRefPropertyItem()
            {
                var root = new VisualElement();
                root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                var fixButton = new Button();
                fixButton.text = "Fix";
                fixButton.SetDisplayElement(false);
                root.Add(fixButton);
                root.Add(new Label());
                return root;
            }

            void BindRefPropertyItem(VisualElement element, int i)
            {
                var label = element.Q<Label>();
                if (label != null &&
                    refPropertiesListView.itemsSource[i] is SearchToolData.ReferencePropertyData propertyData)
                {
                    var isHaveSameRefId = selectedUnityData.referenceData.RefPropertiesData.Count(t =>
                        t.assignedReferenceId == propertyData.assignedReferenceId) > 1;
                    var fixButton = element.Q<Button>();
                    if (isHaveSameRefId)
                    {
                        var index = selectedUnityData.referenceData.RefIdsData.FindIndex(t =>
                            t.referenceId == propertyData.assignedReferenceId);
                        var equalsColor = SerializeReferencePropertyDrawer.GetColorForEqualSerializeReference(index,
                            selectedUnityData.referenceData.RefIdsData.Count);
                        label.style.color = equalsColor;
                        fixButton.clicked += () => FixCrossReference(propertyData);
                    }
                    else
                    {
                        label.style.color = new StyleColor() { value = Color.white };
                    }

                    fixButton.SetDisplayElement(isHaveSameRefId);

                    label.text = $"{propertyData.propertyPath} - {propertyData.propertyType.Name}";
                    label.tooltip = $"Type: {propertyData.propertyType.FullName}\n" +
                                    $"ID: {propertyData.assignedReferenceId}";
                }
            }

            void SelectRefPropertyItem(IEnumerable<object> objects)
            {
                SetDisplayEditData(true);

                var obj = objects.FirstOrDefault();
                if (obj is SearchToolData.ReferencePropertyData propertyData)
                {
                    selectRefPropertyAction?.Invoke(propertyData);
                }
            }

            void SelectComponentItem(IEnumerable<object> objects)
            {
                var obj = objects.FirstOrDefault();
                if (obj is SearchToolData.PrefabComponentData prefabComponentData)
                {
                    selectPrefabComponentDataAction?.Invoke(prefabComponentData);
                }
            }

            void SelectMissingType(IEnumerable<object> objects)
            {
                SetDisplayEditData(false);
                var obj = objects.FirstOrDefault();
                if (obj is ManagedReferenceMissingType missingTypeData)
                {
                    var label = rootVisualElement.Q<Label>("missing-type-data");
                    label.text = missingTypeData.GetDetailData();

                    editMissingType = () =>
                    {
                        EditReferenceTypeWindow.ShowWindow(new TypeData()
                        {
                            AssemblyName = missingTypeData.assemblyName,
                            Namespace = missingTypeData.namespaceName,
                            ClassName = missingTypeData.className
                        }, data => ModifyMissingTypeData(missingTypeData, data));
                    };
                }
            }
        }

        #endregion

        #region Selection

        private void RefreshFilterSelection()
        {
            refIdsListView.itemsSource.Clear();
            refIdsListView.ClearSelection();
            refIdsListView.RefreshItems();

            refPropertiesListView.itemsSource.Clear();
            refPropertiesListView.ClearSelection();
            refPropertiesListView.RefreshItems();

            componentsListView.itemsSource.Clear();
            componentsListView.ClearSelection();
            componentsListView.RefreshItems();

            unityObjectsListView.itemsSource.Clear();
            unityObjectsListView.ClearSelection();

            missingTypesListView.itemsSource.Clear();
            missingTypesListView.ClearSelection();

            int referencesCount = 0;
            if (lastSearchData != null)
            {
                var activatePrefabs = rootVisualElement.Q<Toggle>("unity-objects-activate-prefabs").value;
                var activateSOs = rootVisualElement.Q<Toggle>("unity-objects-activate-scriptableobjects").value;
                var searchFilter = rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name").value;

                var prefabData = lastSearchData.PrefabsData.Where(ApplyPrefabFilter);
                var soData = lastSearchData.SOsData.Where(ApplySOFilter);

                soData.ForEach(so => unityObjectsListView.itemsSource.Add(so));
                prefabData.ForEach(p => unityObjectsListView.itemsSource.Add(p));

                bool ApplySOFilter(SearchToolData.ScriptableObjectData so)
                {
                    var refCount = so.RefIdsData.Count(IsTargetType);
                    var isCorrectName = Path.GetFileNameWithoutExtension(so.AssetPath).ToLowerInvariant()
                        .Contains(searchFilter.ToLowerInvariant());
                    var isActive = activateSOs && refCount > 0 && isCorrectName;
                    return isActive;
                }

                bool ApplyPrefabFilter(SearchToolData.PrefabData p)
                {
                    var refCount = p.componentsData.Sum(c => c.RefIdsData.Count(IsTargetType));
                    referencesCount += refCount;
                    var isCorrectName = Path.GetFileNameWithoutExtension(p.AssetPath).ToLowerInvariant()
                        .Contains(searchFilter.ToLowerInvariant());
                    var isActive = activatePrefabs && refCount > 0 && isCorrectName;
                    return isActive;
                }
            }

            unityObjectsListView.RefreshItems();
            var nonUnityObjects = rootVisualElement.Q<Label>("non-unity-objects");
            nonUnityObjects.SetActiveEmptyPlaceholder(lastSearchData == null ||
                                                      (lastSearchData.PrefabsData.Count == 0 &&
                                                       lastSearchData.SOsData.Count == 0));

            rootVisualElement.Q<Label>("target-type-references-count").text = referencesCount.ToString();

            ClearSaveRefId();
        }

        private void SelectUnityObject(IEnumerable<object> objects)
        {
            var nonSelectedUnityObject = rootVisualElement.Q<Label>("non-selected-unity-object");
            nonSelectedUnityObject.SetActiveEmptyPlaceholder(false);
            ClearSaveRefId();
            var selectedObject = objects.FirstOrDefault();
            var componentsRootStyle = rootVisualElement.Q<VisualElement>("components-root");
            if (selectedObject is SearchToolData.ScriptableObjectData soData)
            {
                componentsRootStyle.SetDisplayElement(false);
                SelectScriptableObject(soData);
            }

            if (selectedObject is SearchToolData.PrefabData prefabData)
            {
                componentsRootStyle.SetDisplayElement(true);
                SelectPrefab(prefabData);
            }
        }

        private void SelectScriptableObject(SearchToolData.ScriptableObjectData soData)
        {
            var so = AssetDatabase.LoadAssetAtPath<Object>(soData.AssetPath);
            AddUnityReferenceData(soData, so);
            selectedUnityData = (soData, so);
            selectRefIdAction = data => { SelectRefId(so, data, PreEditCallback, PostEditCallback); };
            selectRefPropertyAction = data => { SelectRefProperty(so, data, PreEditCallback, PostEditCallback); };

            void PreEditCallback()
            {
                Undo.RecordObject(so, "Update SO Serialize Reference");
            }

            void PostEditCallback()
            {
            }
        }

        private void SelectPrefab(SearchToolData.PrefabData prefabData)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabData.AssetPath);
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            ClearUnityReferenceData();

            bindItemPrefabComponentDataAction = (element, i) =>
            {
                if (element is Label label &&
                    componentsListView.itemsSource[i] is SearchToolData.PrefabComponentData data)
                {
                    var targetComponent = GetTargetComponent(data);
                    if (targetComponent != null)
                    {
                        var goPath = GetGameObjectPath(targetComponent.gameObject);
                        label.text = goPath;
                        label.tooltip = $"Name: {targetComponent.name}\n" +
                                        $"Path: {goPath}\n" +
                                        $"Component type: {targetComponent.GetType().FullName}\n";
                    }
                }
            };

            var filteredComponents = prefabData.componentsData.Where(t => t.RefIdsData.Any(IsTargetType));
            componentsListView.RefreshListViewData(filteredComponents);
            selectPrefabComponentDataAction = data =>
            {
                ClearSaveRefId();
                var targetComponent = GetTargetComponent(data);
                if (targetComponent != null)
                {
                    //TODO Need to get missing reference types from loaded prefab
                    var targetComponentPath = GetGameObjectPath(targetComponent.gameObject);

                    using var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabData.AssetPath);
                    var tempComponents = editingScope.prefabContentsRoot.GetComponentsInChildren<MonoBehaviour>(true);
                    var targetTempComponent = tempComponents.FirstOrDefault(t =>
                    {
                        var path = GetGameObjectPath(t.gameObject);
                        // Bad solution, because AssetDatabase.TryGetGUIDAndLocalFileIdentifier dont work with temp objects
                        return path == targetComponentPath && targetComponent.GetType() == t.GetType() &&
                               t.name == targetComponent.name;
                    });

                    selectedUnityData = (data, targetComponent);
                    AddUnityReferenceData(data, targetTempComponent);
                }
            };

            selectRefIdAction = referenceData =>
            {
                var targetComponent = GetLastSelectedTargetComponent();
                if (targetComponent != null)
                {
                    SelectRefId(targetComponent, referenceData, SaveToUndo, ModifyPrefab);

                    void SaveToUndo()
                    {
                        Undo.RecordObject(targetComponent, "Update Prefab Component Serialize Reference");
                        PrefabUtility.RecordPrefabInstancePropertyModifications(targetComponent);
                    }

                    void ModifyPrefab()
                    {
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }
            };

            selectRefPropertyAction = data =>
            {
                var targetComponent = GetLastSelectedTargetComponent();
                if (targetComponent != null)
                {
                    SelectRefProperty(targetComponent, data, SaveToUndo, SaveEditPrefabContent);

                    void SaveToUndo()
                    {
                        Undo.RecordObject(targetComponent, "Update Prefab Component Serialize Reference");
                        PrefabUtility.RecordPrefabInstancePropertyModifications(targetComponent);
                    }

                    void SaveEditPrefabContent()
                    {
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }
            };

            Component GetTargetComponent(SearchToolData.PrefabComponentData prefabComponentData)
            {
                foreach (var component in components)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out var localId))
                    {
                        if (prefabComponentData.FileId == localId)
                        {
                            return component;
                        }
                    }
                }

                return null;
            }

            Component GetLastSelectedTargetComponent()
            {
                var selectedData = componentsListView.itemsSource[componentsListView.selectedIndex];
                if (selectedData is SearchToolData.PrefabComponentData componentData)
                {
                    var targetComponent = GetTargetComponent(componentData);
                    if (targetComponent != null)
                    {
                        return targetComponent;
                    }
                }

                return null;
            }

            string GetGameObjectPath(GameObject obj)
            {
                var path = "/" + obj.name;
                while (obj.transform.parent != null)
                {
                    obj = obj.transform.parent.gameObject;
                    path = "/" + obj.name + path;
                }

                return path;
            }
        }

        private void ClearSaveRefId()
        {
            temp.data = null;
            saveRefAction = () => { };
            if (soForPropertyEdit != null)
            {
                soForPropertyEdit.Dispose();
                soForPropertyEdit = null;
            }

            if (propertyEdit != null)
            {
                propertyEdit.Dispose();
                propertyEdit = null;
            }

            rootVisualElement.Q<Label>("edit-property-label").text = String.Empty;
        }

        private void SelectRefId(Object asset, SearchToolData.ReferenceIdData idData, Action preEditCallback,
            Action fromToPostEditCallback)
        {
            var referenceObject = ManagedReferenceUtility.GetManagedReference(asset, idData.referenceId);
            if (referenceObject != null)
            {
                var copyObject = TypeUtils.CreateObjectFromType(referenceObject.GetType());
                var json = JsonUtility.ToJson(referenceObject);
                JsonUtility.FromJsonOverwrite(json, copyObject);
                rootVisualElement.Q<Label>("edit-property-label").text = $"Edit: {copyObject.GetType().Name}";
                temp.data = copyObject;
            }

            saveRefAction = () =>
            {
                preEditCallback.Invoke();
                var refObject = ManagedReferenceUtility.GetManagedReference(asset, idData.referenceId);
                var json = JsonUtility.ToJson(temp.data);
                JsonUtility.FromJsonOverwrite(json, refObject);
                fromToPostEditCallback.Invoke();
            };
        }

        private void SelectRefProperty(Object asset, SearchToolData.ReferencePropertyData propertyData,
            Action preEditCallback, Action fromToPostEditCallback)
        {
            if (soForPropertyEdit != null && propertyEdit != null)
            {
                soForPropertyEdit.Dispose();
                propertyEdit.Dispose();
            }

            soForPropertyEdit = new SerializedObject(asset);
            propertyEdit = soForPropertyEdit.FindProperty(propertyData.propertyPath);
            var copyObject = TypeUtils.CreateObjectFromType(propertyEdit.managedReferenceValue.GetType());
            var json = JsonUtility.ToJson(propertyEdit.managedReferenceValue);
            JsonUtility.FromJsonOverwrite(json, copyObject);
            rootVisualElement.Q<Label>("edit-property-label").text = $"Edit: {copyObject.GetType().Name}";
            temp.data = copyObject;

            saveRefAction = () =>
            {
                preEditCallback.Invoke();
                var refObject = propertyEdit.managedReferenceValue;
                var tempJson = JsonUtility.ToJson(temp.data);
                JsonUtility.FromJsonOverwrite(tempJson, refObject);
                soForPropertyEdit.ApplyModifiedProperties();
                soForPropertyEdit.Update();
                fromToPostEditCallback.Invoke();
            };
        }

        private void AddUnityReferenceData(SearchToolData.UnityObjectReferenceData referenceData, Object unityObject)
        {
            refIdsListView.RefreshListViewData(referenceData.RefIdsData.Where(IsTargetType));
            refPropertiesListView.RefreshListViewData(referenceData.RefPropertiesData.Where(GetRefIdFromPropertyId));
            var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(unityObject);
            missingTypesListView.RefreshListViewData(missingTypes);
            missingTypesButton.SetDisplayElement(missingTypes.Any());

            bool GetRefIdFromPropertyId(SearchToolData.ReferencePropertyData property)
            {
                var refId = referenceData.RefIdsData.First(t => t.referenceId == property.assignedReferenceId);
                return IsTargetType(refId);
            }
        }

        private void ClearUnityReferenceData()
        {
            refIdsListView.itemsSource.Clear();
            refIdsListView.ClearSelection();
            refIdsListView.RefreshItems();

            refPropertiesListView.itemsSource.Clear();
            refPropertiesListView.ClearSelection();
            refPropertiesListView.RefreshItems();
        }

        private void FixCrossReference(SearchToolData.ReferencePropertyData propertyData)
        {
            var so = new SerializedObject(selectedUnityData.unityObject);
            var property = so.FindProperty(propertyData.propertyPath);
            SerializeReferencePropertyDrawer.FixCrossReference(property);
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            RefreshAssetDatabaseWithUpdateSelectedUnityObject();
        }

        private void RefreshAssetDatabaseWithUpdateSelectedUnityObject()
        {
            SearchToolWindowAssetDatabase.FillReferenceDataFromUnityObject(selectedUnityData.unityObject, selectedUnityData.referenceData);
            AddUnityReferenceData(selectedUnityData.referenceData, selectedUnityData.unityObject);
            SearchToolWindowAssetDatabase.SaveAssetDatabaseToFile(lastSearchData);
            unityObjectsListView.RefreshItems();
        }

        private void ModifyMissingTypeData(ManagedReferenceMissingType missingType, TypeData fixTypeData)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedUnityData.unityObject);
            var result = EditReferenceTypeUtils.TryModifyDirectFileReferenceType(path,
                missingType.referenceId,
                new TypeData()
                {
                    AssemblyName = missingType.assemblyName,
                    ClassName = missingType.className,
                    Namespace = missingType.namespaceName
                }, fixTypeData);
            if (result)
            {
                RefreshAssetDatabaseWithUpdateSelectedUnityObject();
            }
        }

        #endregion

        #region Assets Database Operations

        private void RefreshAssetsDatabase()
        {
            if (SearchToolWindowAssetDatabase.TryRefreshAssetsDatabase(out var searchData))
            {
                ApplyAssetDatabase(searchData, DateTime.Now);
            }
        }

        private void ApplyAssetDatabase(SearchToolData searchToolData, DateTime searchDataRefreshTime)
        {
            var lastRefreshDate = searchDataRefreshTime.ToString("G");
            var refreshDateText = $"{lastRefreshDate}";
            rootVisualElement.Q<Toggle>("unity-objects-activate-prefabs").SetValueWithoutNotify(true);
            rootVisualElement.Q<Toggle>("unity-objects-activate-scriptableobjects").SetValueWithoutNotify(true);
            rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name").SetValueWithoutNotify(String.Empty);
            rootVisualElement.Q<Label>("last-search-refresh").text = refreshDateText;

            var soRefs = searchToolData.SOsData.SelectMany(t => t.RefIdsData);
            var prefabRefs = searchToolData.PrefabsData.SelectMany(t => t.componentsData).SelectMany(t => t.RefIdsData);
            rootVisualElement.Q<Label>("total-type-references-count").text =
                (soRefs.Count() + prefabRefs.Count()).ToString();

            lastSearchData = searchToolData;
            SetNewType(selectedType);
        }

        #endregion

        #region Type filter

        private void SetNewType(Type newType)
        {
            selectedType = newType;
            var button = rootVisualElement.Q<Button>("target-type");
            button.text = $"Type: {selectedType.Name}";
            button.tooltip = $"Type FullName: {selectedType.FullName}";

            var interfacesRoot = rootVisualElement.Q<VisualElement>("target-type-interfaces-root");
            var typeInterfaces = selectedType.GetInterfaces();
            ClearButtons(interfacesRoot);
            GenerateTypeButtons(typeInterfaces, interfacesRoot);

            var baseTypesRoot = rootVisualElement.Q<VisualElement>("target-type-base-root");
            var baseTypes = GetBaseTypes(selectedType);
            ClearButtons(baseTypesRoot);
            GenerateTypeButtons(baseTypes, baseTypesRoot);

            var openSourceButton = rootVisualElement.Q<Button>("target-type-open-source");
            openSourceButton.SetDisplayElement(newType != typeof(object));

            RefreshFilterSelection();

            void ClearButtons(VisualElement element)
            {
                var previousButtons = element.Query<Button>().ToList();
                foreach (var previousButton in previousButtons)
                {
                    element.Remove(previousButton);
                }
            }

            void GenerateTypeButtons(IEnumerable<Type> types, VisualElement root)
            {
                foreach (var type in types)
                {
                    var typeButton = new Button
                    {
                        text = $"{type.Name}",
                        tooltip = $"FullName: {type.FullName}"
                    };
                    typeButton.clicked += () => SetNewType(type);
                    root.Add(typeButton);
                }
            }

            static IEnumerable<Type> GetBaseTypes(Type type)
            {
                var current = type.BaseType;
                while (current != null)
                {
                    yield return current;
                    current = current.BaseType;
                }
            }
        }

        private void OpenTargetTypeSourceFile()
        {
            SerializeReferencePropertyDrawer.OpenSourceFile(selectedType);
        }

        private void ShowAssignableTypes()
        {
            SearchToolWindowSearchProvider.Show(SetNewType);
        }

        private bool IsTargetType(SearchToolData.ReferenceIdData referenceIdData)
        {
            var isAssignable = selectedType.IsAssignableFrom(referenceIdData.objectType);
            return isAssignable;
        }

        #endregion
    }
}