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

        //TODO implement write data to serialize reference and save asset
        private Action<SearchToolData.ReferenceData> applyPropertyReferenceAction;
        private Action<SearchToolData.PrefabComponentData> selectPrefabComponentDataAction;
        private Action<VisualElement, int> bindItemPrefabComponentDataAction;

        //TODO Implement logs and asserts everywhere
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
            LoadSearchDataFromFile();
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

        private void CreateUiToolkitLayout()
        {
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SearchToolkit.uxml";

            //TODO need split database line and type filter
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            rootVisualElement.Add(visualTreeAsset.Instantiate());
            rootVisualElement.Q<Button>("searchTargetTypeObjects").clicked += SearchAssets;

            var property = rootVisualElement.Q<PropertyField>("serializeReferenceProperty");
            property.Bind(tempSO);
            property.bindingPath = nameof(temp.tempObject);

            unityObjectsListView = rootVisualElement.Q<ListView>("unityObjects");
            unityObjectsListView.selectionType = SelectionType.Single;
            unityObjectsListView.Clear();
            unityObjectsListView.itemsSource = new List<object>();
            unityObjectsListView.makeItem = () => new Label();
            unityObjectsListView.bindItem = BindUnityObjectItem;
            unityObjectsListView.selectionChanged += SelectUnityObject;

            propertiesListView = rootVisualElement.Q<ListView>("properties");
            propertiesListView.itemsSource = new List<object>();
            propertiesListView.makeItem = () => new Label();
            propertiesListView.selectionType = SelectionType.Single;
            propertiesListView.bindItem = BindPropertyItem;
            propertiesListView.selectionChanged += SelectPropertyItem;

            componentsListView = rootVisualElement.Q<ListView>("components");
            componentsListView.itemsSource = new List<object>();
            componentsListView.makeItem = () => new Label();
            componentsListView.bindItem = (element, i) => bindItemPrefabComponentDataAction?.Invoke(element, i);
            componentsListView.selectionType = SelectionType.Single;
            componentsListView.selectionChanged += SelectComponentItem;
            //TODO: apply button - update serialize reference and save asset

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
                    label.text = referenceData.objectTypeName;
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

        private void SelectUnityObject(IEnumerable<object> objects)
        {
            var selectedObject = objects.First();
            var componentsRootStyle = rootVisualElement.Q<VisualElement>("componentsRoot").style;
            if (selectedObject is SearchToolData.ScriptableObjectData soData)
            {
                componentsRootStyle.visibility = new StyleEnum<Visibility>() { value = Visibility.Hidden };
                SelectScriptableObject(soData);
            }

            if (selectedObject is SearchToolData.PrefabData prefabData)
            {
                componentsRootStyle.visibility = new StyleEnum<Visibility>() { value = Visibility.Visible };
                SelectPrefab(prefabData);
            }
        }

        private void SelectScriptableObject(SearchToolData.ScriptableObjectData soData)
        {
            AddSerializeReferenceProperties(soData.serializeReferencesData);
            selectPropertyReferenceAction = data =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(soData.assetPath);
                SelectSerializedReference(asset, data);
            };
        }

        private void SelectPrefab(SearchToolData.PrefabData prefabData)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabData.AssetPath);
            propertiesListView.itemsSource.Clear();
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
                var targetComponent = GetTargetComponent(data);
                if (targetComponent != null)
                {
                    AddSerializeReferenceProperties(data.serializeReferencesData);
                }
            };

            selectPropertyReferenceAction = referenceData =>
            {
                var selectedData = componentsListView.itemsSource[componentsListView.selectedIndex];
                if (selectedData is SearchToolData.PrefabComponentData componentData)
                {
                    var targetComponent = GetTargetComponent(componentData);
                    if (targetComponent != null)
                    {
                        SelectSerializedReference(targetComponent, referenceData);
                    }
                }
            };

            Component GetTargetComponent(SearchToolData.PrefabComponentData prefabComponentData)
            {
                var components = prefab.GetComponents<Component>();
                var targetComponent =
                    components?.FirstOrDefault(t => t.GetInstanceID() == prefabComponentData.instanceId);
                return targetComponent;
            }
        }

        private void SelectSerializedReference(Object asset, SearchToolData.ReferenceData data)
        {
            var referenceObject = ManagedReferenceUtility.GetManagedReference(asset, data.referenceID);
            if (referenceObject != null)
            {
                temp.tempObject = referenceObject;
            }
        }

        private void AddSerializeReferenceProperties(IReadOnlyList<SearchToolData.ReferenceData> referencesData)
        {
            propertiesListView.RefreshListViewData(referencesData);
        }

        private void SearchAssets()
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

            SaveSearchDataToFile(searchData);
            AssignSearchData(searchData, DateTime.Now);

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
                    prefabData.componentsData.Add(new SearchToolData.PrefabComponentData()
                    {
                        instanceId = component.GetInstanceID(),
                        //TODO: need skip serialize references on prefab overrides when references is not changed,
                        // or mark this reference
                        serializeReferencesData = GetReferencesListFromObject(component)
                    });
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
                            objectTypeName = type.FullName,
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
                "SerializeReferenceDropdown_SearchDataCacheFile.txt");
            return path;
        }

        private void LoadSearchDataFromFile()
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                //TODO switch to Newtonsoft.Json 
                var searchCachedData = JsonUtility.FromJson<SearchToolData>(json);
                var creationTime = File.GetCreationTime(path);
                AssignSearchData(searchCachedData, creationTime);
            }
        }

        private void SaveSearchDataToFile(SearchToolData searchToolData)
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            //TODO switch to Newtonsoft.Json 
            var json = JsonUtility.ToJson(searchToolData, prettyPrint: true);
            File.WriteAllText(path, json);
        }

        private void AssignSearchData(SearchToolData searchToolData, DateTime searchDataRefreshTime)
        {
            var lastRefreshDate = searchDataRefreshTime.ToString("G");
            rootVisualElement.Q<Label>("lastSearchRefresh").text = lastRefreshDate;

            unityObjectsListView.itemsSource.Clear();
            searchToolData.SOsData.ForEach(t => unityObjectsListView.itemsSource.Add(t));
            searchToolData.PrefabsData.ForEach(t => unityObjectsListView.itemsSource.Add(t));
            unityObjectsListView.RefreshItems();
        }

        private void SetNewType(Type type)
        {
            selectedType = type;
            var button = rootVisualElement.Q<Button>("targetType");
            button.text = $"Type: {selectedType.Name}";
            button.tooltip = $"Type Full Name: {selectedType.FullName}";
            //TODO Set new type and apply this type on all available references
        }
    }
}