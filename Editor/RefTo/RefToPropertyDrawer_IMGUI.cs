using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public class RefToPropertyDrawerIMGUI
    {
        private static GUIStyle _errorStyle;

        private static GUIStyle ErrorStyle => _errorStyle ??= new GUIStyle(EditorStyles.boldLabel)
            { normal = new GUIStyleState() { textColor = Color.red } };

        public void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var (refType, refToTargetType, hostType, host, isSameType) = RefToExtensions.GetInspectorValues(property);
            label.tooltip = $"Target \nType - {refToTargetType.Name} \nNamespace - {refToTargetType.Namespace}";

            EditorGUI.BeginProperty(rect, label, property);

            var propertyRect = rect;
            var labelWidth = EditorGUIUtility.labelWidth / 2;
            propertyRect.width = labelWidth;
            EditorGUI.LabelField(propertyRect, label);

            var height = EditorGUIUtility.singleLineHeight;
            var fieldSize = (rect.width - labelWidth) * 0.5f;
            var labelRect = new Rect(rect.position + new Vector2(labelWidth, 0),
                new Vector2(rect.width - fieldSize - labelWidth, height));
            var fieldRect = new Rect(labelRect.position + new Vector2(labelRect.width, 0),
                new Vector2(fieldSize, height));

            var style = isSameType ? EditorStyles.boldLabel : ErrorStyle;
            var refLabel = $" R: {refType?.Name}";
            var refContent = new GUIContent(refLabel,
                $"Reference \nType - {refType?.Name} \nNamespace - {refType?.Namespace}");

            EditorGUI.LabelField(labelRect, refContent, style);

            var newValue = EditorGUI.ObjectField(fieldRect, host, hostType, true);
            if (host != newValue)
            {
                if (newValue != null)
                {
                    RefToDrawerUtils.TryApplyRefToValue(property, newValue, refToTargetType);
                }
                else
                {
                    RefToExtensions.ResetRefTo(property);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}