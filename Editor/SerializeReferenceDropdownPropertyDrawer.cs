using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        private SerializedPropertyInfo _serializedPropertyInfo;
        private int _lastUsedIndex = -1;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            _serializedPropertyInfo ??= new SerializedPropertyInfo(property);
            if (property.propertyType == SerializedPropertyType.ManagedReference && 
                _serializedPropertyInfo.CanShowDropdown())
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
            var selectedIndex = _serializedPropertyInfo.GetIndexAssignedTypeOfProperty(property);
            var typeName = _serializedPropertyInfo.AssignableTypes[selectedIndex]?.Name ?? "null";
            if (EditorGUI.DropdownButton(rect, new GUIContent(typeName), FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    _serializedPropertyInfo.AssignableTypes, WriteNewInstanceByIndexType);
                dropdown.Show(rect);
            }

            void WriteNewInstanceByIndexType(int typeIndex)
            {
                if (selectedIndex == _lastUsedIndex) return;
                _lastUsedIndex = typeIndex;
                Undo.RecordObject(property.serializedObject.targetObject, "Update type in SRD");
                object newObject = null;
                if (_lastUsedIndex != 0)
                {
                    newObject = Activator.CreateInstance(_serializedPropertyInfo.AssignableTypes[_lastUsedIndex]);
                }

                _serializedPropertyInfo.ApplyValueToProperty(newObject, property);
            }
        }
    }
}