using System;
using System.IO;
using SerializeReferenceDropdown.Editor.Utils;
using SerializeReferenceDropdown.Editor.YAMLEdit;
using UnityEditor;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.EditReferenceType
{
    public class EditReferenceTypeWindow : EditorWindow
    {
        private static EditReferenceTypeWindow instance;

        private Action<TypeData> callback;

        private TypeData startTypeData;
        private TypeData selectedTypeData;

        public static void ShowWindow(TypeData typeData, Action<TypeData> onApplyTypeData)
        {
            var window = GetOrCreateWindow();
            window.callback = onApplyTypeData;
            window.startTypeData = typeData;
            window.selectedTypeData = typeData;
            window.Focus();
            window.Refresh();
        }

        private static EditReferenceTypeWindow GetOrCreateWindow()
        {
            if (instance)
            {
                return instance;
            }

            var window = GetWindow<EditReferenceTypeWindow>();
            return window;
        }

        private void OnEnable()
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "EditReferenceType.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            var layout = visualTreeAsset.Instantiate();
            rootVisualElement.Add(layout);

            rootVisualElement.Q<Button>("apply").clicked += () =>
            {
                callback.Invoke(selectedTypeData);
                Close();
            };

            Refresh();
        }

        private void Refresh()
        {
            var asmText = rootVisualElement.Q<TextField>("assembly-field");
            var nsText = rootVisualElement.Q<TextField>("namespace-field");
            var cText = rootVisualElement.Q<TextField>("classname-field");

            asmText.value = startTypeData.AssemblyName;
            nsText.value = startTypeData.Namespace;
            cText.value = startTypeData.ClassName;

            asmText.RegisterValueChangedCallback((evt => selectedTypeData.AssemblyName = evt.newValue));
            nsText.RegisterValueChangedCallback((evt => selectedTypeData.Namespace = evt.newValue));
            cText.RegisterValueChangedCallback((evt => selectedTypeData.ClassName = evt.newValue));
        }
    }
}