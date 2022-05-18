using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        private SerializedPropertyInfo serializedPropertyInfo;
        private int lastUsedIndex = -1;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            serializedPropertyInfo ??= new SerializedPropertyInfo(property);
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                serializedPropertyInfo.CanShowDropdown())
            {
                Rect dropdownRect = new Rect(rect);
                dropdownRect.width -= EditorGUIUtility.labelWidth;
                dropdownRect.x += EditorGUIUtility.labelWidth;
                dropdownRect.height = EditorGUIUtility.singleLineHeight;
                DrawTypeDropdown(dropdownRect, property, label);
                EditorGUI.PropertyField(rect, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        void DrawTypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            var selectedIndex = serializedPropertyInfo.GetIndexAssignedTypeOfProperty(property);
            var typeName = serializedPropertyInfo.AssignableTypes[selectedIndex]?.Name ?? "null";
            if (EditorGUI.DropdownButton(rect, new GUIContent(typeName), FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    serializedPropertyInfo.AssignableTypes, WriteNewInstanceByIndexType);
                dropdown.Show(rect);
            }

            void WriteNewInstanceByIndexType(int typeIndex)
            {
                if (selectedIndex == lastUsedIndex)
                {
                    return;
                }
                lastUsedIndex = typeIndex;
                Undo.RecordObject(property.serializedObject.targetObject, "Update type in SRD");
                object newObject = null;
                if (lastUsedIndex != 0)
                {
                    newObject = Activator.CreateInstance(serializedPropertyInfo.AssignableTypes[lastUsedIndex]);
                }

                serializedPropertyInfo.ApplyValueToProperty(newObject, property);
            }
        }
    }
}
