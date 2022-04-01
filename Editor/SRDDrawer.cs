using System;
using UnityEditor;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(SRDAttribute))]
    public class SRDDrawer : PropertyDrawer
    {
        private const int DropdownHeight = 20;
        private SerializedPropertyInfo _serializedPropertyInfo;

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
            var oldText = label.text;
            var srdLabel = $"[SRD] {label.text}";
            var newIndex = EditorGUI.Popup(rect, srdLabel, selectedIndex, _serializedPropertyInfo.AssignableTypeNames);
            label.text = oldText;
            if (selectedIndex != newIndex)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Update type in SRD");
                object newObject = null;
                if (newIndex != 0)
                {
                    newObject = Activator.CreateInstance(_serializedPropertyInfo.GetTypeAtIndex(newIndex));
                }

                _serializedPropertyInfo.ApplyValueToProperty(newObject, property);
            }
        }
    }
}