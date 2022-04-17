using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(SRDAttribute))]
    public class SRDDrawer : PropertyDrawer
    {
        private const int DropdownHeight = 20;
        private SerializedPropertyInfo _serializedPropertyInfo;
        private SRDDropdown _dropdown;
        private int _lastUsedIndex = -1;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true) + DropdownHeight;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            _serializedPropertyInfo ??= new SerializedPropertyInfo(property);
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                _serializedPropertyInfo.CanShowSRD())
            {
                var rects = SplitRect();
                DrawSRDTypeDropdown(rects.dropdownRect, property, label);
                EditorGUI.PropertyField(rects.propertyRect, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();

            (Rect dropdownRect, Rect propertyRect) SplitRect()
            {
                var dropdownRect = new Rect(rect.x, rect.y, rect.width, DropdownHeight);
                var propertyRect = new Rect(rect.x, rect.y + DropdownHeight, rect.width, rect.height - DropdownHeight);
                return (dropdownRect, propertyRect);
            }
        }

        void DrawSRDTypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            var selectedIndex = _serializedPropertyInfo.GetIndexAssignedTypeOfProperty(property);
            if (GUI.Button(rect, new GUIContent("Show types"), EditorStyles.toolbarButton))
            {
                _dropdown ??= new SRDDropdown(new AdvancedDropdownState(),
                    _serializedPropertyInfo.AssignableTypeNames, WriteNewInstanceByIndexType);
                _dropdown.Show(rect);
            }

            void WriteNewInstanceByIndexType(int typeIndex)
            {
                if (selectedIndex != _lastUsedIndex)
                {
                    _lastUsedIndex = typeIndex;
                    Undo.RecordObject(property.serializedObject.targetObject, "Update type in SRD");
                    object newObject = null;
                    if (_lastUsedIndex != 0)
                    {
                        newObject = Activator.CreateInstance(_serializedPropertyInfo.GetTypeAtIndex(_lastUsedIndex));
                    }

                    _serializedPropertyInfo.ApplyValueToProperty(newObject, property);
                }
            }
        }
    }
}
