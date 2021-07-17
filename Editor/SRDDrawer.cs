using System;
using UnityEditor;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(SRDAttribute))]
    public class SRDDrawer : PropertyDrawer
    {
        private SerializedPropertyInfo _serializedPropertyInfo;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;


            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawSRDProperty(rect, property, label);
                EditorGUI.PropertyField(rect, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        void DrawSRDProperty(Rect rect, SerializedProperty property, GUIContent label)
        {
            _serializedPropertyInfo ??= new SerializedPropertyInfo(property);
            var selectedIndex = _serializedPropertyInfo.GetIndexAssignedTypeOfProperty(property);
            var newIndex = EditorGUI.Popup(rect, label.text, selectedIndex,
                _serializedPropertyInfo.AssignableTypeNames);
            if (selectedIndex != newIndex)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Dropdown");
                object newObject = null;
                if (newIndex != 0)
                {
                    newObject = Activator.CreateInstance(_serializedPropertyInfo.AssignableTypes[newIndex]);
                }

                _serializedPropertyInfo.ApplyValueToProperty(newObject, property);
            }
        }
    }
}