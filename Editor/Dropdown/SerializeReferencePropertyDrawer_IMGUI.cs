using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public partial class SerializeReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            propertyRect = rect;

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawIMGUITypeDropdown(rect, property, label);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        //IMGUI with base implementation. If u want to use IMGUI in your project - u can extend and setup all features.
        //But much better - switch your inspector to UI Toolkit 
        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float fixButtonWidth = 40f;
            assignableTypes ??= GetAssignableTypes(property);

            var isHaveOtherReference = IsHaveSameOtherSerializeReference(property);

            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            var dropdownRect = GetDropdownIMGUIRect(rect);

            EditorGUI.EndDisabledGroup();

            var dropdownTypeContent = new GUIContent(
                text: GetTypeName(referenceType),
                tooltip: GetTypeTooltip(referenceType));

            var style = EditorStyles.miniPullDown;
            if (isHaveOtherReference)
            {
                var uniqueColor = GetColorForEqualSerializeReference(property);
                style = new GUIStyle(EditorStyles.miniPullDown)
                    { normal = new GUIStyleState() { textColor = uniqueColor } };
            }

            if (EditorGUI.DropdownButton(dropdownRect, dropdownTypeContent, FocusType.Keyboard, style))
            {
                var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property, registerUndo: true));
                dropdown.Show(dropdownRect);
            }

            if (isHaveOtherReference)
            {
                if (GUI.Button(GetFixCrossReferencesRect(dropdownRect), "Fix"))
                {
                    FixCrossReference(property);
                }
            }

            EditorGUI.PropertyField(rect, property, label, true);


            Rect GetDropdownIMGUIRect(Rect mainRect)
            {
                var dropdownOffset = EditorGUIUtility.labelWidth;
                Rect newRect = new Rect(mainRect);
                newRect.width -= dropdownOffset;
                newRect.x += dropdownOffset;
                newRect.height = EditorGUIUtility.singleLineHeight;
                if (isHaveOtherReference)
                {
                    newRect.width -= fixButtonWidth;
                }

                return newRect;
            }

            Rect GetFixCrossReferencesRect(Rect rectIn)
            {
                var newRect = rectIn;
                newRect.x += rectIn.width + EditorGUIUtility.standardVerticalSpacing;
                newRect.width = fixButtonWidth - EditorGUIUtility.standardVerticalSpacing;
                return newRect;
            }
        }
    }
}