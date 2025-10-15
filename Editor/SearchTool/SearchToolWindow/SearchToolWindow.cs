using System;
using System.IO;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow
{
    public partial class SearchToolWindow : EditorWindow
    {
        private static SearchToolWindow instance;

        private Type selectedType = typeof(object);
        private SearchToolData lastSearchData;

        private SearchToolWindowTempSO temp;
        private SerializedObject tempSO;

        private bool checkUnityObjects;

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


        private void CreateUiToolkitLayout()
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SearchTool.uxml");

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            var layout = visualTreeAsset.Instantiate();

            layout.style.flexGrow = 1;
            layout.style.flexShrink = 1;
            rootVisualElement.Add(layout);

            rootVisualElement.Q<Button>("refresh-database").clicked += RefreshAssetsDatabase;
            
            var property = rootVisualElement.Q<PropertyField>("edit-property");
            property.Bind(tempSO);
            property.SetEnabled(false);
            property.bindingPath = nameof(temp.data);

            InitTypeLayout();
            CreateHierarchyTree();
        }

        private void RefreshAssetsDatabase()
        {
            if (SearchToolWindowAssetDatabase.TryRefreshAssetsDatabase(out var searchData))
            {
                ApplyAssetDatabase(searchData, DateTime.Now);
            }
        }

        private void ApplyAssetDatabase(SearchToolData searchToolData, DateTime updateDate)
        {
            lastSearchData = searchToolData;
            SetNewType(selectedType);
        }
    }
}