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

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);
            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            const float firstButtonWidth = 40;
            const float secondButtonWidth = 45;
            const float offset = 2;

            var (copyRect, pasteRect, dropdownRect) = GetIMGUIRects(rect, firstButtonWidth, secondButtonWidth, offset);

            if (GUI.Button(copyRect, "Copy"))
            {
                SaveReferenceValueToClipBoard(property);
            }

            EditorGUI.BeginDisabledGroup(CanPasteValueFromClipBoard() == false);
            if (GUI.Button(pasteRect, "Paste"))
            {
                PasteReferenceValueFromClipBoard(property);
            }

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(GetTypeName(referenceType)), FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property));
                dropdown.Show(dropdownRect);
            }

            EditorGUI.PropertyField(rect, property, label, true);
        }

        (Rect firstButton, Rect secondButton, Rect dropdown) GetIMGUIRects(Rect mainRect, float firstButtonWidth,
            float secondButtonWidth, float offset)
        {
            Rect firstButton = new Rect(mainRect);
            firstButton.width = firstButtonWidth;
            firstButton.height = EditorGUIUtility.singleLineHeight;
            firstButton.x += EditorGUIUtility.labelWidth + offset;

            Rect secondButton = new Rect(firstButton);
            secondButton.width = secondButtonWidth;
            secondButton.x += secondButtonWidth;

            var dropdownOffset = (EditorGUIUtility.labelWidth + firstButtonWidth + secondButtonWidth + 5 * offset);
            Rect dropdownRect = new Rect(mainRect);
            dropdownRect.width -= dropdownOffset;
            dropdownRect.x += dropdownOffset;
            dropdownRect.height = EditorGUIUtility.singleLineHeight;

            return (firstButton, secondButton, dropdownRect);
        }

        private void SaveReferenceValueToClipBoard(SerializedProperty property)
        {
            var refValue = property.managedReferenceValue;
            var stringValue = JsonUtility.ToJson(refValue);
            EditorGUIUtility.systemCopyBuffer = stringValue;
        }

        private bool CanPasteValueFromClipBoard()
        {
            //TODO need learn how to check can paste values to target type =_=
            var stringValue = EditorGUIUtility.systemCopyBuffer;
            var isValueType = stringValue?.StartsWith('{') == true && stringValue?.EndsWith('}') == true;
            return isValueType;
        }

        private void PasteReferenceValueFromClipBoard(SerializedProperty property)
        {
            var stringValue = EditorGUIUtility.systemCopyBuffer;
            JsonUtility.FromJsonOverwrite(stringValue, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        private string GetTypeName(Type type) => type == null ? NullName : ObjectNames.NicifyVariableName(type.Name);

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
