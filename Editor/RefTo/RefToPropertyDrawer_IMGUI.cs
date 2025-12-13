using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public partial class RefToPropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var propertyRect = rect;
            var (refType, targetType, hostType, host, isSameType) = RefToExtensions.GetInspectorValues(property);
            label.tooltip = $"Target \nType - {targetType.Name} \nNamespace - {targetType.Namespace}";
            EditorGUI.BeginProperty(rect, label, property);
            var labelWidth = EditorGUIUtility.labelWidth / 2;
            propertyRect.width = labelWidth;
            EditorGUI.LabelField(propertyRect, label);

            var height = EditorGUIUtility.singleLineHeight;

            var fieldSize = (rect.width - labelWidth) * 0.5f;
            var labelRect = new Rect(rect.position + new Vector2(labelWidth, 0),
                new Vector2(rect.width - fieldSize - labelWidth, height));
            var fieldRect = new Rect(labelRect.position + new Vector2(labelRect.width, 0),
                new Vector2(fieldSize, height));

            var style = isSameType
                ? EditorStyles.boldLabel
                : new GUIStyle(EditorStyles.boldLabel)
                    { normal = new GUIStyleState() { textColor = Color.red } };
            var refLabel = $" R: {refType?.Name}";
            EditorGUI.LabelField(labelRect,
                new GUIContent(refLabel, $"Reference \nType - {refType?.Name} \nNamespace - {refType?.Namespace}"),
                style);

            var newValue = EditorGUI.ObjectField(fieldRect, host, hostType, true);
            if (host != newValue)
            {
                if (newValue != null)
                {
                    TryApplyRefToValue(property, newValue, targetType);
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