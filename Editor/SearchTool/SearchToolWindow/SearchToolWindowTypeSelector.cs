using System;
using System.Collections.Generic;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow
{
    public partial class SearchToolWindow
    {
        private void InitTypeLayout()
        {
            rootVisualElement.Q<Button>("target-type").clicked += ShowAssignableTypes;
            rootVisualElement.Q<Button>("target-type-open-source").clicked += OpenTargetTypeSourceFile;
            rootVisualElement.Q<Button>("clear-target-type").clicked += () => SetNewType(typeof(object));
        }

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

            RefreshTree();

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
    }
}