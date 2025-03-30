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
        private static bool isDirtyUIToolkit;

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
            var hideStyle = new StyleEnum<Visibility>() { value = Visibility.Hidden };
            var visibleStyle = new StyleEnum<Visibility>() { value = Visibility.Visible };
            bool isNew = true;
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SerializeReferenceDropdown.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();

            var selectTypeButton = root.Q<Button>("typeSelect");
            selectTypeButton.clickable.clicked += ShowDropdown;

            var fixCrossRefButton = root.Q<Button>("fixCrossReferences");
            fixCrossRefButton.clickable.clicked += () =>
            {
                MakeDirtyUIToolkit();
                FixCrossReference(property);
            };
            var openSourceFIleButton = root.Q<Button>("openSourceFile");
            openSourceFIleButton.style.visibility = hideStyle;
            openSourceFIleButton.clicked += () => { OpenSourceFile(property.managedReferenceValue.GetType()); };

            var showSearchToolButton = root.Q<Button>("showSearchTool");
            showSearchToolButton.style.visibility = hideStyle;
            showSearchToolButton.clicked += () =>
            {
                var type = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                ShowSearchTool(type);
            };

            var propertyPath = property.propertyPath;
            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, UpdateDropdown);
            UpdateDropdown(property.serializedObject);
            isNew = false;

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
                var prop = so.FindProperty(propertyPath);
                propertyField.BindProperty(prop);
                var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                selectTypeButton.text = selectedTypeName;
                selectTypeButton.tooltip = $"Type Full Name: {selectedType?.FullName}";

                if (SerializeReferenceToolsUserPreferences.GetOrLoadSettings().DisableOpenSourceFile == false)
                {
                    openSourceFIleButton.style.visibility =
                        property.managedReferenceValue == null ? hideStyle : visibleStyle;
                }

                showSearchToolButton.style.visibility = selectedType != null ? visibleStyle : hideStyle;

                if (isNew == false && isDirtyUIToolkit == false)
                {
                    return;
                }

                selectTypeButton.style.color = new StyleColor(Color.white);
                fixCrossRefButton.style.visibility = hideStyle;

                if (IsHaveSameOtherSerializeReference(property))
                {
                    fixCrossRefButton.style.visibility = visibleStyle;
                    var color = GetColorForEqualSerializedReference(property);
                    selectTypeButton.style.color = color;
                }
            }

            void MakeDirtyUIToolkit()
            {
                isDirtyUIToolkit = true;
                EditorApplication.delayCall += () => { isDirtyUIToolkit = false; };
            }
        }
    }
}