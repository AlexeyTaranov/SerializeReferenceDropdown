using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public partial class SerializeReferencePropertyDrawer : PropertyDrawer
    {
        //TODO Need find better solution for check ui update and traverse all serialized properties
        private HashSet<Object> dirtySerializedObjects = new HashSet<Object>();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawUIToolkitTypeDropdown(root, property);
            }
            else
            {
                root.Add(new PropertyField(property));
            }

            return root;
        }

        private void DrawUIToolkitTypeDropdown(VisualElement root, SerializedProperty property)
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SerializeReferenceDropdown.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();

            var selectTypeButton = root.Q<Button>("type-select");
            selectTypeButton.clickable.clicked += ShowDropdown;

            var fixCrossRefButton = root.Q<Button>("fix-cross-references");
            fixCrossRefButton.clickable.clicked += () =>
            {
                MakeDirtyUIToolkit();
                FixCrossReference(property);
            };
            var openSourceFIleButton = root.Q<Button>("open-source-file");
            openSourceFIleButton.SetDisplayElement(false);
            openSourceFIleButton.clicked += () => { OpenSourceFile(property.managedReferenceValue.GetType()); };

            var showSearchToolButton = root.Q<Button>("show-search-tool");
            showSearchToolButton.SetDisplayElement(false);
            showSearchToolButton.clicked += () =>
            {
                var type = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                ShowSearchTool(type);
            };

            var propertyPath = property.propertyPath;
            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, UpdateDropdown);
            RefreshDropdown(property.serializedObject, true);

            void ShowDropdown()
            {
                var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index =>
                    {
                        MakeDirtyUIToolkit();
                        WriteNewInstanceByIndexType(index, property);
                    });
                var buttonMatrix = selectTypeButton.worldTransform;
                var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
                var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }

            void UpdateDropdown(SerializedObject so)
            {
                RefreshDropdown(so, false);
            }

            void RefreshDropdown(SerializedObject so, bool isNew)
            {
                var prop = so.FindProperty(propertyPath);
                var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                selectTypeButton.text = selectedTypeName;
                selectTypeButton.tooltip = $"Type Full Name: {selectedType?.FullName}";

                openSourceFIleButton.SetDisplayElement(property.managedReferenceValue != null);

                var activeSearchTool = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableSearchTool;
                showSearchToolButton.SetDisplayElement(selectedType != null && activeSearchTool);
                if (isNew == false && dirtySerializedObjects.Contains(so.targetObject) == false)
                {
                    return;
                }

                propertyField.BindProperty(prop);
                selectTypeButton.style.color = new StyleColor(Color.white);
                fixCrossRefButton.SetDisplayElement(false);

                if (IsHaveSameOtherSerializeReference(property))
                {
                    fixCrossRefButton.SetDisplayElement(true);
                    var color = GetColorForEqualSerializeReference(property);
                    selectTypeButton.style.color = color;
                }
            }

            void MakeDirtyUIToolkit()
            {
                var target = property.serializedObject.targetObject;
                dirtySerializedObjects.Add(target);
                EditorApplication.delayCall += () => { dirtySerializedObjects.Remove(target); };
            }
        }
    }
}