using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawIMGUITypeDropdown(rect, property, label);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

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
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SerializeReferenceDropdown.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            var selectTypeButton = root.Q<Button>();
            selectTypeButton.clickable.clicked += ShowDropdown;
            assignableTypes ??= GetAssignableTypes(property);
            UpdateDropdown();

            void ShowDropdown()
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index =>
                    {
                        WriteNewInstanceByIndexType(index, property);
                        UpdateDropdown();
                    });
                var buttonMatrix = selectTypeButton.worldTransform;
                var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
                var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }
            
            void UpdateDropdown()
            {
                propertyField.BindProperty(property);
                var selectedType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                selectTypeButton.text = selectedTypeName;
            }
        }

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);
            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            var dropdownRect = GetDropdownIMGUIRect(rect);

            EditorGUI.EndDisabledGroup();

            var dropdownTypeContent = new GUIContent(
                text: GetTypeName(referenceType),
                tooltip: GetTypeTooltip(referenceType));
            if (EditorGUI.DropdownButton(dropdownRect,dropdownTypeContent, FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property));
                dropdown.Show(dropdownRect);
            }

            EditorGUI.PropertyField(rect, property, label, true);

            Rect GetDropdownIMGUIRect(Rect mainRect)
            {
                var dropdownOffset = EditorGUIUtility.labelWidth;
                Rect rect = new Rect(mainRect);
                rect.width -= dropdownOffset;
                rect.x += dropdownOffset;
                rect.height = EditorGUIUtility.singleLineHeight;

                return rect;
            }
        }

        private string GetTypeName(Type type)
        {
            if (type == null)
            {
                return NullName;
            }

            var typesWithNames = TypeCache.GetTypesWithAttribute(typeof(SerializeReferenceDropdownNameAttribute));
            if (typesWithNames.Contains(type))
            {
                var dropdownNameAttribute = type.GetCustomAttribute<SerializeReferenceDropdownNameAttribute>();
                return dropdownNameAttribute.Name;
            }

            return ObjectNames.NicifyVariableName(type.Name);
        }

        private string GetTypeTooltip(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithTooltip = TypeCache.GetTypesWithAttribute(typeof(TypeTooltipAttribute));
            if (typesWithTooltip.Contains(type))
            {
                var tooltipAttribute = type.GetCustomAttribute<TypeTooltipAttribute>();
                return tooltipAttribute.tooltip;
            }

            return String.Empty;
        }

        private List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var nonUnityTypes = TypeCache.GetTypesDerivedFrom(propertyType).Where(IsAssignableNonUnityType).ToList();
            nonUnityTypes.Insert(0, null);
            return nonUnityTypes;

            bool IsAssignableNonUnityType(Type type)
            {
                return TypeUtils.IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }
        }

        private void WriteNewInstanceByIndexType(int typeIndex, SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var newObject = newType != null ? FormatterServices.GetUninitializedObject(newType) : null;
            ApplyValueToProperty(newObject, property);
        }

        private void ApplyValueToProperty(object value, SerializedProperty property)
        {
            property.managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }
    }
}
