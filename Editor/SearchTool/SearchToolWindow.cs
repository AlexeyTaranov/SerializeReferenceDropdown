using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
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
            CreateUiToolkitLayout();
            LoadSearchDataFromFile();
        }

        private void CreateUiToolkitLayout()
        {
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SearchToolkit.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            rootVisualElement.Add(visualTreeAsset.Instantiate());
            rootVisualElement.Q<Button>("searchTargetTypeObjects").clicked += SearchAssets;
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
                var prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null || PrefabUtility.GetPrefabInstanceStatus(prefab) != PrefabInstanceStatus.Connected)
                {
                    return false;
                }

                var prefabData = new SearchToolData.PrefabData();
                var components = prefab.GetComponents<Component>();
                foreach (var component in components)
                {
                    prefabData.componentsData.Add(new SearchToolData.PrefabComponentData()
                    {
                        instanceId = component.GetInstanceID(),
                        serializeReferencesData = GetReferencesListFromObject(component)
                    });
                }

                PrefabUtility.UnloadPrefabContents(prefab);
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

            var json = JsonUtility.ToJson(searchToolData, prettyPrint: true);
            File.WriteAllText(path, json);
        }

        private void AssignSearchData(SearchToolData searchToolData, DateTime searchDataRefreshTime)
        {
            var lastRefreshDate = searchDataRefreshTime.ToString("G");
            rootVisualElement.Q<Label>("lastSearchRefresh").text = $"Last refresh date: {lastRefreshDate}";
        }

        private void SetNewType(Type type)
        {
            selectedType = type;
            var button = rootVisualElement.Q<Button>("targetType");
            button.text = $"Type: {selectedType.Name}";
            button.tooltip = $"Type Full Name: {selectedType.FullName}";
        }
    }
}