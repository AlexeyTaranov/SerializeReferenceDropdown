using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(SRDAttribute))]
    public class SRDDrawer : PropertyDrawer
    {
        private SerializedPropertyInfo _serializedPropertyInfo;
        private SRDDropdown _dropdown;
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
                _serializedPropertyInfo.CanShowSRD())
            {
                Rect dropdowRect = new Rect(rect);
                dropdowRect.width -= EditorGUIUtility.labelWidth;
                dropdowRect.x += EditorGUIUtility.labelWidth;
                dropdowRect.height = EditorGUIUtility.singleLineHeight;
                DrawSRDTypeDropdown(dropdowRect, property, label);
                EditorGUI.PropertyField(rect, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        void DrawSRDTypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            var selectedIndex = _serializedPropertyInfo.GetIndexAssignedTypeOfProperty(property);
            if (EditorGUI.DropdownButton(rect,
                    new GUIContent(_serializedPropertyInfo.AssignableTypeNames[selectedIndex]), FocusType.Keyboard))
            {
                _dropdown ??= new SRDDropdown(new AdvancedDropdownState(),
                    _serializedPropertyInfo.AssignableTypeNames, WriteNewInstanceByIndexType);
                _dropdown.Show(rect);
            }

            void WriteNewInstanceByIndexType(int typeIndex)
            {
                if (selectedIndex == _lastUsedIndex) return;
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