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
            var hideStyle = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            var flexStyle = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
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
            openSourceFIleButton.style.display = hideStyle;
            openSourceFIleButton.clicked += () => { OpenSourceFile(property.managedReferenceValue.GetType()); };
            if (SerializeReferenceToolsUserPreferences.GetOrLoadSettings().DisableOpenSourceFile == false)
            {
                openSourceFIleButton.style.display = property.managedReferenceValue == null ? hideStyle : flexStyle;
            }

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
                selectTypeButton.tooltip = $"Class: {selectedType?.Name}\n" +
                                           $"Namespace: {selectedType?.Namespace}";
                if (isNew == false && isDirtyUIToolkit == false)
                {
                    return;
                }

                selectTypeButton.style.color = new StyleColor(Color.white);
                fixCrossRefButton.style.display = hideStyle;

                if (IsHaveSameOtherSerializeReference(property))
                {
                    fixCrossRefButton.style.display = flexStyle;
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