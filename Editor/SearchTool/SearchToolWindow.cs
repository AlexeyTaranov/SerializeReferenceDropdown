using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SerializeReferenceDropdown.Editor.Dropdown;
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
        private const string fileName = "SerializeReference_ToolSearch_DataCacheFile.json";
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
        private ListView componentsListView;

        private Action saveRefAction;

        private Action<SearchToolData.ReferenceIdData> selectRefIdAction;
        private Action<SearchToolData.ReferenceIdData> applyRefIdAction;

        private Action<SearchToolData.ReferencePropertyData> selectRefPropertyAction;
        private Action<SearchToolData.ReferencePropertyData> applyRefPropertyAction;

        private Action<SearchToolData.PrefabComponentData> selectPrefabComponentDataAction;
        private Action<VisualElement, int> bindItemPrefabComponentDataAction;

        private (SearchToolData.UnityObjectReferenceData referenceData, Object unityObject) selectedUnityData;


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
            LoadAssetDatabaseFromFile();
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
            rootVisualElement.Q<Button>("select-props").clicked += () => SetDisplayPropsOrIDs(true);
            rootVisualElement.Q<Button>("select-ids").clicked += () => SetDisplayPropsOrIDs(false);
            rootVisualElement.Q<Toggle>("unity-objects-activate-prefabs")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<Toggle>("unity-objects-activate-scriptableobjects")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name")
                .RegisterValueChangedCallback(evt => RefreshFilterSelection());
            rootVisualElement.Q<Button>("target-type").clicked += ShowAssignableTypes;
            rootVisualElement.Q<Button>("target-type-open-source").clicked += OpenTargetTypeSourceFile;

            var property = rootVisualElement.Q<PropertyField>("edit-property");
            property.Bind(tempSO);
            property.bindingPath = nameof(temp.tempObject);

            unityObjectsListView = rootVisualElement.Q<ListView>("unity-objects");
            MakeDefaultSingleListView(ref unityObjectsListView);
            unityObjectsListView.makeItem = MakeUnityObjectItem;

            unityObjectsListView.bindItem = BindUnityObjectItem;
            unityObjectsListView.selectionChanged += SelectUnityObject;
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

            componentsListView = rootVisualElement.Q<ListView>("components");
            MakeDefaultSingleListView(ref componentsListView);
            componentsListView.bindItem = (element, i) => bindItemPrefabComponentDataAction?.Invoke(element, i);
            componentsListView.selectionChanged += SelectComponentItem;
            var nonSelectedUnityObject = rootVisualElement.Q<Label>("non-selected-unity-object");
            nonSelectedUnityObject.SetActiveEmptyPlaceholder(true);
            SetDisplayPropsOrIDs(true);

            void SetDisplayPropsOrIDs(bool isProps)
            {
                refPropertiesListView.SetDisplayElement(isProps);
                refIdsListView.SetDisplayElement(isProps == false);
                refIdsListView.ClearSelection();
                refIdsListView.RefreshItems();
                refPropertiesListView.ClearSelection();
                refPropertiesListView.RefreshItems();
                ClearSaveRefId();
            }

            void MakeDefaultSingleListView(ref ListView listView)
            {
                listView.showBorder = true;
                listView.itemsSource = new List<object>();
                listView.makeItem = () => new Label();
                listView.selectionType = SelectionType.Single;
            }

            VisualElement MakeUnityObjectItem()
            {
                var root = new VisualElement();
                var image = new Image();
                root.Add(image);
                root.Add(new Label());
                image.style.maxWidth = new StyleLength(15);
                root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                root.style.alignItems = new StyleEnum<Align>(Align.Center);
                return root;
            }

            void BindUnityObjectItem(VisualElement element, int i)
            {
                var label = element.Q<Label>();
                var icon = element.Q<Image>();
                if (label != null && icon != null &&
                    unityObjectsListView.itemsSource[i] is SearchToolData.IAssetData assetData)
                {
                    label.text = Path.GetFileNameWithoutExtension(assetData.AssetPath);
                    label.tooltip = assetData.AssetPath;
                    var iconName = assetData is SearchToolData.PrefabData ? "d_Prefab Icon" : "d_ScriptableObject Icon";
                    var guiContent = EditorGUIUtility.IconContent(iconName);
                    icon.image = guiContent.image;
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

            void SelectRefIdItem(IEnumerable<object> objects)
            {
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
            AddUnityReferenceData(soData);
            var so = AssetDatabase.LoadAssetAtPath<Object>(soData.AssetPath);
            selectedUnityData = (soData, so);
            selectRefIdAction = data => { SelectRefId(so, data, SaveCallback); };
            selectRefPropertyAction = data => { SelectRefProperty(so, data, SaveCallback); };

            void SaveCallback()
            {
                EditorUtility.SetDirty(so);
            }
        }

        private void SelectPrefab(SearchToolData.PrefabData prefabData)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabData.AssetPath);
            ClearUnityReferenceData();

            bindItemPrefabComponentDataAction = (element, i) =>
            {
                if (element is Label label &&
                    componentsListView.itemsSource[i] is SearchToolData.PrefabComponentData data)
                {
                    var targetComponent = GetTargetComponent(data);
                    if (targetComponent != null)
                    {
                        label.text = targetComponent.name;
                        label.tooltip = $"Name: {targetComponent.name}\n" +
                                        $"Component type: {targetComponent.GetType().FullName}\n" +
                                        $"Instance ID: {targetComponent.GetInstanceID()}\n";
                    }
                }
            };

            componentsListView.RefreshListViewData(prefabData.componentsData);
            selectPrefabComponentDataAction = data =>
            {
                ClearSaveRefId();
                var targetComponent = GetTargetComponent(data);
                if (targetComponent != null)
                {
                    selectedUnityData = (data, targetComponent);
                    AddUnityReferenceData(data);
                }
            };

            selectRefIdAction = referenceData =>
            {
                var targetComponent = GetLastSelectedTargetComponent();
                if (targetComponent != null)
                {
                    SelectRefId(targetComponent, referenceData, SaveCallback);

                    void SaveCallback()
                    {
                        EditorUtility.SetDirty(targetComponent);
                        EditorUtility.SetDirty(prefab);
                    }
                }
            };

            selectRefPropertyAction = data =>
            {
                var targetComponent = GetLastSelectedTargetComponent();
                if (targetComponent != null)
                {
                    SelectRefProperty(targetComponent, data, SaveCallback);

                    void SaveCallback()
                    {
                        EditorUtility.SetDirty(targetComponent);
                        EditorUtility.SetDirty(prefab);
                    }
                }
            };

            SearchToolData.PrefabComponentData GetLastSelectedComponent()
            {
                var selectedData = componentsListView.itemsSource[componentsListView.selectedIndex];
                return selectedData as SearchToolData.PrefabComponentData;
            }

            Component GetTargetComponent(SearchToolData.PrefabComponentData prefabComponentData)
            {
                var components = prefab.GetComponents<Component>();
                var targetComponent =
                    components?.FirstOrDefault(t => t.GetInstanceID() == prefabComponentData.InstanceId);
                return targetComponent;
            }

            Component GetLastSelectedTargetComponent()
            {
                var componentData = GetLastSelectedComponent();
                if (componentData != null)
                {
                    var targetComponent = GetTargetComponent(componentData);
                    if (targetComponent != null)
                    {
                        return targetComponent;
                    }
                }

                return null;
            }
        }

        private void ClearSaveRefId()
        {
            temp.tempObject = null;
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

        private void SelectRefId(Object asset, SearchToolData.ReferenceIdData idData, Action saveCallback)
        {
            var referenceObject = ManagedReferenceUtility.GetManagedReference(asset, idData.referenceId);
            if (referenceObject != null)
            {
                var copyObject = TypeUtils.CreateObjectFromType(referenceObject.GetType());
                var json = JsonUtility.ToJson(referenceObject);
                JsonUtility.FromJsonOverwrite(json, copyObject);
                rootVisualElement.Q<Label>("edit-property-label").text = $"Edit: {copyObject.GetType().Name}";
                temp.tempObject = copyObject;
            }

            saveRefAction = () =>
            {
                var refObject = ManagedReferenceUtility.GetManagedReference(asset, idData.referenceId);
                var json = JsonUtility.ToJson(temp.tempObject);
                JsonUtility.FromJsonOverwrite(json, refObject);
                saveCallback.Invoke();
            };
        }

        private void SelectRefProperty(Object asset, SearchToolData.ReferencePropertyData propertyData,
            Action saveCallback)
        {
            if (soForPropertyEdit != null && propertyEdit != null)
            {
                soForPropertyEdit.Dispose();
                propertyEdit.Dispose();
            }

            soForPropertyEdit = new SerializedObject(asset);
            propertyEdit = soForPropertyEdit.FindProperty(propertyData.propertyPath);
            if (propertyEdit != null)
            {
                var copyObject = TypeUtils.CreateObjectFromType(propertyEdit.managedReferenceValue.GetType());
                var json = JsonUtility.ToJson(propertyEdit.managedReferenceValue);
                JsonUtility.FromJsonOverwrite(json, copyObject);
                rootVisualElement.Q<Label>("edit-property-label").text = $"Edit: {copyObject.GetType().Name}";
                temp.tempObject = copyObject;
            }
            else
            {
                //TODO invalid so??
            }

            saveRefAction = () =>
            {
                var refObject = propertyEdit.managedReferenceValue;
                var json = JsonUtility.ToJson(temp.tempObject);
                JsonUtility.FromJsonOverwrite(json, refObject);
                saveCallback.Invoke();
            };
        }

        private void AddUnityReferenceData(SearchToolData.UnityObjectReferenceData referenceData)
        {
            refIdsListView.RefreshListViewData(referenceData.RefIdsData);
            refPropertiesListView.RefreshListViewData(referenceData.RefPropertiesData);
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
            FillReferenceDataFromUnityObject(selectedUnityData.unityObject, selectedUnityData.referenceData);
            AddUnityReferenceData(selectedUnityData.referenceData);
        }

        #endregion

        #region Assets Database Operations

        private void RefreshAssetsDatabase()
        {
            var searchData = new SearchToolData();
            var soIterator = new FileIterator<ScriptableObject>(FillScriptableObjects)
            {
                ProgressBarLabel = "Step 1: Analyze Scriptable Objects",
                SkipFileExtensions = new List<string>() { ".uss", ".uxml" }
            };
            if (soIterator.IterateOnUnityAssetFiles() == IteratorResult.CanceledByUser)
            {
                return;
            }

            var prefabIterator = new FileIterator<GameObject>(FillPrefabs)
            {
                ProgressBarLabel = "Step 2: Analyze Prefabs"
            };
            if (prefabIterator.IterateOnUnityAssetFiles() == IteratorResult.CanceledByUser)
            {
                return;
            }

            SaveAssetDatabaseToFile(searchData);
            ApplyAssetDatabase(searchData, DateTime.Now);

            bool FillScriptableObjects(string path)
            {
                var so = AssetDatabase.LoadMainAssetAtPath(path);
                var soData = new SearchToolData.ScriptableObjectData() { AssetPath = path };
                FillReferenceDataFromUnityObject(so, soData);
                if (soData.RefIdsData.Any())
                {
                    searchData.SOsData.Add(soData);
                }

                return false;
            }

            bool FillPrefabs(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    return false;
                }

                if (PrefabUtility.IsPartOfAnyPrefab(prefab) == false)
                {
                    return false;
                }

                var prefabData = new SearchToolData.PrefabData(path);
                var components = prefab.GetComponents<Component>();
                foreach (var component in components)
                {
                    //TODO: need skip serialize references on prefab overrides when references is not changed,
                    // or mark this reference

                    var componentData = new SearchToolData.PrefabComponentData()
                    {
                        InstanceId = component.GetInstanceID()
                    };
                    FillReferenceDataFromUnityObject(component, componentData);
                    if (componentData.RefIdsData.Any())
                    {
                        prefabData.componentsData.Add(componentData);
                    }
                }

                if (prefabData.componentsData.FirstOrDefault(t => t.RefIdsData.Any()) != null)
                {
                    searchData.PrefabsData.Add(prefabData);
                }

                return false;
            }
        }

        private void FillReferenceDataFromUnityObject(Object unityObject,
            SearchToolData.UnityObjectReferenceData refData)
        {
            refData.RefIdsData = GetReferenceIdsListFromObject();
            refData.RefPropertiesData = GetReferencePropertiesListFromObject();

            List<SearchToolData.ReferenceIdData> GetReferenceIdsListFromObject()
            {
                var ids = ManagedReferenceUtility.GetManagedReferenceIds(unityObject);
                var referenceList = new List<SearchToolData.ReferenceIdData>();
                foreach (var id in ids)
                {
                    var type = ManagedReferenceUtility.GetManagedReference(unityObject, id)?.GetType();
                    if (type != null)
                    {
                        referenceList.Add(new SearchToolData.ReferenceIdData()
                        {
                            objectType = type,
                            referenceId = id,
                        });
                    }
                }

                return referenceList;
            }

            List<SearchToolData.ReferencePropertyData> GetReferencePropertiesListFromObject()
            {
                var propertiesData = new List<SearchToolData.ReferencePropertyData>();
                using var so = new SerializedObject(unityObject);
                using var iterator = so.GetIterator();
                iterator.NextVisible(true);
                PropertyUtils.TraverseProperty(iterator, string.Empty, FillPropertiesData);
                return propertiesData.Distinct().ToList();

                bool FillPropertiesData(SerializedProperty property)
                {
                    if (property.managedReferenceValue != null)
                    {
                        propertiesData.Add(new SearchToolData.ReferencePropertyData()
                        {
                            assignedReferenceId = property.managedReferenceId,
                            propertyPath = property.propertyPath,
                            propertyType = property.managedReferenceValue.GetType()
                        });
                    }

                    return false;
                }
            }
        }


        private string GetFilePath()
        {
            var editorLibraryPath = Path.Combine(Application.dataPath, "../Library");
            var path = Path.Combine(editorLibraryPath, fileName);
            return path;
        }

        private void LoadAssetDatabaseFromFile()
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var searchCachedData = JsonConvert.DeserializeObject<SearchToolData>(json);
                var creationTime = File.GetCreationTime(path);
                ApplyAssetDatabase(searchCachedData, creationTime);
            }
        }

        private void SaveAssetDatabaseToFile(SearchToolData searchToolData)
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var settings = SerializeReferenceToolsUserPreferences.GetOrLoadSettings();
            var port = settings.SearchToolIntegrationPort;
            searchToolData.InteagrationPort = port;
            var json = JsonConvert.SerializeObject(searchToolData, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private void ApplyAssetDatabase(SearchToolData searchToolData, DateTime searchDataRefreshTime)
        {
            var lastRefreshDate = searchDataRefreshTime.ToString("G");
            var refreshDateText = $"{lastRefreshDate}";
            rootVisualElement.Q<Toggle>("unity-objects-activate-prefabs").SetValueWithoutNotify(true);
            rootVisualElement.Q<Toggle>("unity-objects-activate-scriptableobjects").SetValueWithoutNotify(true);
            rootVisualElement.Q<ToolbarSearchField>("unity-objects-filter-name").SetValueWithoutNotify(String.Empty);
            rootVisualElement.Q<Label>("last-search-refresh").text = refreshDateText;

            lastSearchData = searchToolData;
            SetNewType(selectedType);
        }

        #endregion

        #region Type filter

        private void SetNewType(Type type)
        {
            selectedType = type;
            var button = rootVisualElement.Q<Button>("target-type");
            button.text = $"Type: {selectedType.Name}";
            button.tooltip = $"Type FullName: {selectedType.FullName}";

            var interfacesRoot = rootVisualElement.Q<VisualElement>("target-type-interfaces-root");
            var previousButtons = interfacesRoot.Query<Button>().ToList();
            foreach (var previousButton in previousButtons)
            {
                interfacesRoot.Remove(previousButton);
            }

            var typeInterfaces = selectedType.GetInterfaces();
            foreach (var typeInterface in typeInterfaces)
            {
                var typeButton = new Button
                {
                    text = $"{typeInterface.Name}",
                    tooltip = $"FullName: {typeInterface.FullName}"
                };
                typeButton.clicked += () => SetNewType(typeInterface);
                interfacesRoot.Add(typeButton);
            }

            var openSourceButton = rootVisualElement.Q<Button>("target-type-open-source");
            openSourceButton.SetDisplayElement(type != typeof(object));

            RefreshFilterSelection();
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