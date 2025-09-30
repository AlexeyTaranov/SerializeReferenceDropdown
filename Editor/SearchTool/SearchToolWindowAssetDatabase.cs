using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public static class SearchToolWindowAssetDatabase
    {
        private const string fileName = "SerializeReference_ToolSearch_DataCacheFile.json";

        public static bool TryRefreshAssetsDatabase(out SearchToolData searchData)
        {
            searchData = new SearchToolData();
            var localData = searchData;
            var soIterator = new FileIterator<ScriptableObject>(FillScriptableObjects)
            {
                ProgressBarLabel = "Step 1: Analyze Scriptable Objects",
                SkipFileExtensions = new List<string>() { ".uss", ".uxml" }
            };
            if (soIterator.IterateOnUnityAssetFiles() == IteratorResult.CanceledByUser)
            {
                return false;
            }

            var prefabIterator = new FileIterator<GameObject>(FillPrefabs)
            {
                ProgressBarLabel = "Step 2: Analyze Prefabs"
            };
            if (prefabIterator.IterateOnUnityAssetFiles() == IteratorResult.CanceledByUser)
            {
                return false;
            }

            searchData.PrefabsData = searchData.PrefabsData.OrderBy(t => Path.GetFileNameWithoutExtension(t.AssetPath))
                .ToList();
            searchData.SOsData =
                searchData.SOsData.OrderBy(t => Path.GetFileNameWithoutExtension(t.AssetPath)).ToList();

            SaveAssetDatabaseToFile(searchData);
            return true;

            bool FillScriptableObjects(string path)
            {
                var so = AssetDatabase.LoadMainAssetAtPath(path);
                var soData = new SearchToolData.ScriptableObjectData() { AssetPath = path };
                FillReferenceDataFromUnityObject(so, soData);
                if (soData.RefIdsData.Any())
                {
                    localData.SOsData.Add(soData);
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
                var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var component in components)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out var localId))
                    {
                        var componentData = new SearchToolData.PrefabComponentData()
                        {
                            FileId = localId,
                        };
                        FillReferenceDataFromUnityObject(component, componentData);
                        if (componentData.RefIdsData.Any())
                        {
                            prefabData.componentsData.Add(componentData);
                        }
                    }
                }

                if (prefabData.componentsData.FirstOrDefault(t => t.RefIdsData.Any()) != null)
                {
                    localData.PrefabsData.Add(prefabData);
                }

                return false;
            }
        }

        public static void FillReferenceDataFromUnityObject(Object unityObject,
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
                SOUtils.TraverseSO(unityObject, FillPropertiesData);
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


        private static string GetFilePath()
        {
            var editorLibraryPath = Path.Combine(Application.dataPath, "../Library");
            var path = Path.Combine(editorLibraryPath, fileName);
            return path;
        }

        public static (SearchToolData data, DateTime fileCreateTime) LoadSearchData()
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var searchCachedData = JsonConvert.DeserializeObject<SearchToolData>(json,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }
                    );
                    var creationTime = File.GetCreationTime(path);
                    return (searchCachedData, creationTime);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            return (null, default);
        }

        public static void SaveAssetDatabaseToFile(SearchToolData searchToolData)
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = JsonConvert.SerializeObject(searchToolData, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}