#if UNITY_2023_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<,>))]
    public class RefToPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            DrawUIToolkit(root, property);
            return root;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, false);
        }

        private static readonly GUIStyle ErrorStyle = new GUIStyle(EditorStyles.boldLabel)
            { normal = new GUIStyleState() { textColor = Color.red } };

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var propertyRect = rect;
            var (refType, targetType, host, isSameType) = GetInspectorValues(property);
            var tooltip = $"Reference type: {TypeToName(refType)} \nRefTo Type: {TypeToName(targetType)}";
            label.tooltip = tooltip;
            EditorGUI.BeginProperty(rect, label, property);
            var labelWidth = EditorGUIUtility.labelWidth > 90 ? 90 : EditorGUIUtility.labelWidth;
            propertyRect.width = labelWidth;
            EditorGUI.LabelField(propertyRect, label);


            var height = EditorGUIUtility.singleLineHeight;

            var fieldSize = (rect.width - labelWidth) * 0.3f;
            var labelRect = new Rect(rect.position + new Vector2(labelWidth, 0),
                new Vector2(rect.width - fieldSize - labelWidth, height));
            var fieldRect = new Rect(labelRect.position + new Vector2(labelRect.width, 0),
                new Vector2(fieldSize, height));

            var style = isSameType ? EditorStyles.boldLabel : ErrorStyle;
            var refLabel = $"RefTo: {TypeToName(refType)} [{TypeToName(targetType)}]";
            EditorGUI.LabelField(labelRect, new GUIContent(refLabel), style);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(fieldRect, host, typeof(UnityEngine.Object), true);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndProperty();
        }

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            var uiToolkitLayoutPath = "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/RefTo.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);
            var (_, targetType, _, _) = GetInspectorValues(property);

            propertyField.tooltip = $"Field: {property.name} \nTarget Type: {targetType.Name} \nNamespace: {targetType.Namespace}";
            
            var propertyPath = property.propertyPath;
            root.TrackSerializedObjectValue(property.serializedObject, RefreshDynamic);
            RefreshDynamic(property.serializedObject);

            void RefreshDynamic(SerializedObject so)
            {
                using var localProperty = so.FindProperty(propertyPath);
                var (refType, _, host, isSameType) = GetInspectorValues(localProperty);
                var refLabel = root.Q<Label>("RefName");
                var refTypeName = refType == null ? "null" : refType.Name;
                refLabel.text = $"R:{refTypeName}";
                refLabel.tooltip = $"Reference \nType: {refType?.Name} \nNamespace: {refType?.Namespace}";
                refLabel.style.color = isSameType ? new StyleColor(Color.white) : new StyleColor(Color.red);
                root.Q<ObjectField>().value = host;
            }
        }

        private (Type refType, Type targetType, Object host, bool isSameType) GetInspectorValues(
            SerializedProperty property)
        {
            var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(property);
            RefToExtensions.TryGetRefType(property, out var targetType);
            var isSameType = false;
            Type refType = null;
            if (host != null)
            {
                var reference = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(host, id);
                if (reference != null)
                {
                    refType = reference.GetType();
                    isSameType = targetType.IsAssignableFrom(refType);
                }
            }
            else
            {
                isSameType = true;
            }

            return (refType, targetType, host, isSameType);
        }

        string TypeToName(Type type) => ObjectNames.NicifyVariableName(type?.Name);
    }
}
#endif