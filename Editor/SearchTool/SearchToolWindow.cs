using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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

        private Type selectedType;
        private SearchToolData lastSearchData;

        private SearchToolWindowTempSO temp;
        private SerializedObject tempSO;

        private ListView unityObjectsListView;
        private ListView propertiesListView;
        private ListView componentsListView;

        private Action<SearchToolData.ReferenceData> selectPropertyReferenceAction;

        private Action<SearchToolData.ReferenceData> applyPropertyReferenceAction;
        private Action<SearchToolData.PrefabComponentData> selectPrefabComponentDataAction;
        private Action<VisualElement, int> bindItemPrefabComponentDataAction;
        private Action saveReferenceAction;

        #region Window

        public static void ShowSearchTypeWindow(Type type)
        {
            var window = GetOrCreateWindow();
            window.SetNewType(type);
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
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SearchTool.uxml";

            //TODO need split database line and type filter
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            rootVisualElement.Add(visualTreeAsset.Instantiate());

            rootVisualElement.Q<Button>("refreshDatabase").clicked += RefreshAssetsDatabase;
            rootVisualElement.Q<Button>("clearTargetType").clicked += () => SetNewType(typeof(object));
            rootVisualElement.Q<Button>("applyData").clicked += () => saveReferenceAction?.Invoke();

            var property = rootVisualElement.Q<PropertyField>("serializeReferenceProperty");
            property.Bind(tempSO);
            property.bindingPath = nameof(temp.tempObject);

            unityObjectsListView = rootVisualElement.Q<ListView>("unityObjects");
            MakeDefaultSingleListView(ref unityObjectsListView);
            unityObjectsListView.bindItem = BindUnityObjectItem;
            unityObjectsListView.selectionChanged += SelectUnityObject;

            propertiesListView = rootVisualElement.Q<ListView>("properties");
            MakeDefaultSingleListView(ref propertiesListView);
            propertiesListView.bindItem = BindPropertyItem;
            propertiesListView.selectionChanged += SelectPropertyItem;

            componentsListView = rootVisualElement.Q<ListView>("components");
            MakeDefaultSingleListView(ref componentsListView);
            componentsListView.bindItem = (element, i) => bindItemPrefabComponentDataAction?.Invoke(element, i);
            componentsListView.selectionChanged += SelectComponentItem;

            void MakeDefaultSingleListView(ref ListView listView)
            {
                listView.itemsSource = new List<object>();
                listView.makeItem = () => new Label();
                listView.selectionType = SelectionType.Single;
            }

            void BindUnityObjectItem(VisualElement element, int i)
            {
                if (element is Label label &&
                    unityObjectsListView.itemsSource[i] is SearchToolData.IAssetData assetData)
                {
                    label.text = Path.GetFileNameWithoutExtension(assetData.AssetPath);
                    label.tooltip = assetData.AssetPath;
                }
            }


            void BindPropertyItem(VisualElement element, int i)
            {
                if (element is Label label &&
                    propertiesListView.itemsSource[i] is SearchToolData.ReferenceData referenceData)
                {
                    label.text = referenceData.objectType.Name;
                    label.tooltip = referenceData.objectType.FullName;
                }
            }

            void SelectPropertyItem(IEnumerable<object> objects)
            {
                var obj = objects.FirstOrDefault();
                if (obj is SearchToolData.ReferenceData referenceData)
                {
                    selectPropertyReferenceAction?.Invoke(referenceData);
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

        private void SelectUnityObject(IEnumerable<object> objects)
        {
            ClearSerializedReference();
            var selectedObject = objects.First();
            var componentsRootStyle = rootVisualElement.Q<VisualElement>("componentsRoot").style;
            if (selectedObject is SearchToolData.ScriptableObjectData soData)
            {
                componentsRootStyle.display = new StyleEnum<DisplayStyle>() { value = DisplayStyle.None };
                SelectScriptableObject(soData);
            }

            if (selectedObject is SearchToolData.PrefabData prefabData)
            {
                componentsRootStyle.display = new StyleEnum<DisplayStyle>() { value = DisplayStyle.Flex };
                SelectPrefab(prefabData);
            }
        }

        private void SelectScriptableObject(SearchToolData.ScriptableObjectData soData)
        {
            AddSerializeReferenceProperties(soData.serializeReferencesData);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(soData.assetPath);
            selectPropertyReferenceAction = data =>
            {
                SelectSerializedReference(asset, data, SaveCallback);

                void SaveCallback()
                {
                    EditorUtility.SetDirty(asset);
                }
            };
        }

        private void SelectPrefab(SearchToolData.PrefabData prefabData)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabData.AssetPath);
            propertiesListView.itemsSource.Clear();
            propertiesListView.ClearSelection();
            propertiesListView.RefreshItems();

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
                ClearSerializedReference();
                var targetComponent = GetTargetComponent(data);
                if (targetComponent != null)
                {
                    AddSerializeReferenceProperties(data.serializeReferencesData);
                }
            };

            selectPropertyReferenceAction = referenceData =>
            {
                var componentData = GetLastSelectedComponent();
                if (componentData != null)
                {
                    var targetComponent = GetTargetComponent(componentData);
                    if (targetComponent != null)
                    {
                        SelectSerializedReference(targetComponent, referenceData, SaveCallback);

                        void SaveCallback()
                        {
                            EditorUtility.SetDirty(targetComponent);
                            EditorUtility.SetDirty(prefab);
                        }
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
                    components?.FirstOrDefault(t => t.GetInstanceID() == prefabComponentData.instanceId);
                return targetComponent;
            }
        }

        private void ClearSerializedReference()
        {
            temp.tempObject = null;
            saveReferenceAction = () => { };
        }

        private void SelectSerializedReference(Object asset, SearchToolData.ReferenceData data, Action saveCallback)
        {
            var referenceObject = ManagedReferenceUtility.GetManagedReference(asset, data.referenceID);
            if (referenceObject != null)
            {
                var copyObject = TypeUtils.CreateObjectFromType(referenceObject.GetType());
                var json = JsonUtility.ToJson(referenceObject);
                JsonUtility.FromJsonOverwrite(json, copyObject);
                temp.tempObject = copyObject;
            }

            saveReferenceAction = () =>
            {
                var refObject = ManagedReferenceUtility.GetManagedReference(asset, data.referenceID);
                var json = JsonUtility.ToJson(temp.tempObject);
                JsonUtility.FromJsonOverwrite(json, refObject);
                saveCallback.Invoke();
            };
        }

        private void AddSerializeReferenceProperties(IReadOnlyList<SearchToolData.ReferenceData> referencesData)
        {
            propertiesListView.RefreshListViewData(referencesData);
        }

        #endregion

        #region Assets Database Operations

        private void RefreshAssetsDatabase()
        {
            var searchData = new SearchToolData();
            var soIterator = new FileIterator<ScriptableObject>(FillScriptableObjects)
            {
                ProgressBarLabel = "Step 1: Analyze Scriptable Objects",
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
                var targetObject = AssetDatabase.LoadMainAssetAtPath(path);
                var data = new SearchToolData.ScriptableObjectData()
                {
                    assetPath = path,
                    serializeReferencesData = GetReferencesListFromObject(targetObject)
                };
                if (data.serializeReferencesData.Any())
                {
                    searchData.SOsData.Add(data);
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

                var prefabData = new SearchToolData.PrefabData()
                {
                    assetPath = path
                };
                var components = prefab.GetComponents<Component>();
                foreach (var component in components)
                {
                    var componentData = new SearchToolData.PrefabComponentData()
                    {
                        instanceId = component.GetInstanceID(),
                        //TODO: need skip serialize references on prefab overrides when references is not changed,
                        // or mark this reference
                        serializeReferencesData = GetReferencesListFromObject(component)
                    };
                    if (componentData.serializeReferencesData.Any())
                    {
                        prefabData.componentsData.Add(componentData);
                    }
                }

                if (prefabData.componentsData.FirstOrDefault(t => t.serializeReferencesData.Any()) != null)
                {
                    searchData.PrefabsData.Add(prefabData);
                }

                return false;
            }

            List<SearchToolData.ReferenceData> GetReferencesListFromObject(Object unityObject)
            {
                var ids = ManagedReferenceUtility.GetManagedReferenceIds(unityObject);
                var referenceList = new List<SearchToolData.ReferenceData>();
                foreach (var id in ids)
                {
                    var type = ManagedReferenceUtility.GetManagedReference(unityObject, id)?.GetType();
                    if (type != null)
                    {
                        referenceList.Add(new SearchToolData.ReferenceData()
                        {
                            objectType = type,
                            referenceID = id,
                        });
                    }
                }

                return referenceList;
            }
        }


        private string GetFilePath()
        {
            var path = Path.Combine($"{Application.persistentDataPath}",
                "SerializeReference_ToolSearch_DataCacheFile.txt");
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

            var json = JsonConvert.SerializeObject(searchToolData, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private void ApplyAssetDatabase(SearchToolData searchToolData, DateTime searchDataRefreshTime)
        {
            var lastRefreshDate = searchDataRefreshTime.ToString("G");
            rootVisualElement.Q<Label>("lastSearchRefresh").text = lastRefreshDate;

            lastSearchData = searchToolData;
            SetNewType(selectedType);
        }

        #endregion

        #region Type filter

        private void SetNewType(Type type)
        {
            selectedType = type;
            var button = rootVisualElement.Q<Button>("targetType");
            button.text = $"Type: {selectedType.Name}";
            button.tooltip = $"Type FullName: {selectedType.FullName}";

            propertiesListView.itemsSource.Clear();
            propertiesListView.ClearSelection();
            propertiesListView.RefreshItems();

            componentsListView.itemsSource.Clear();
            componentsListView.ClearSelection();
            componentsListView.RefreshItems();

            unityObjectsListView.itemsSource.Clear();
            unityObjectsListView.ClearSelection();
            var soData = lastSearchData.SOsData.Where(so => so.serializeReferencesData.Any(IsTargetType));
            var prefabData = lastSearchData.PrefabsData.Where(p =>
                p.componentsData.FirstOrDefault(c => c.serializeReferencesData.Any(IsTargetType)) != null);
            soData.ForEach(so => unityObjectsListView.itemsSource.Add(so));
            prefabData.ForEach(p => unityObjectsListView.itemsSource.Add(p));
            unityObjectsListView.RefreshItems();

            ClearSerializedReference();
        }

        private bool IsTargetType(SearchToolData.ReferenceData referenceData)
        {
            var isAssignable = selectedType.IsAssignableFrom(referenceData.objectType);
            return isAssignable;
        }

        #endregion
    }
}