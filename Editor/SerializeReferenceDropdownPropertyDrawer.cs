using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        public const string NullName = "null";
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

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property,GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);
            Rect dropdownRect = new Rect(rect);
            dropdownRect.width -= EditorGUIUtility.labelWidth;
            dropdownRect.x += EditorGUIUtility.labelWidth;
            dropdownRect.height = EditorGUIUtility.singleLineHeight;
            var objectType = ReflectionUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
            var objectTypeName = objectType == null ? NullName : ObjectNames.NicifyVariableName(objectType.Name);
            if (EditorGUI.DropdownButton(rect, new GUIContent(objectTypeName), FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes, index => WriteNewInstanceByIndexType(index,property));
                dropdown.Show(dropdownRect);
            }
            EditorGUI.PropertyField(rect, property, label, true);
        }

        private List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = ReflectionUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            var targetAssignableTypes = ReflectionUtils.GetFinalAssignableTypes(propertyType, allTypes,
                predicate: type => type.IsSubclassOf(typeof(UnityEngine.Object)) == false).ToList();
            targetAssignableTypes.Insert(0,null);
            return targetAssignableTypes;
        }
        
        void WriteNewInstanceByIndexType(int typeIndex,SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var newObject = newType != null ? Activator.CreateInstance(newType) : null;
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
