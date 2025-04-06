#if UNITY_2023_2_OR_NEWER
using System;
using System.IO;
using SerializeReferenceDropdown.Editor.Utils;
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

            var style = isSameType ? EditorStyles.boldLabel : ErrorStyle;
            var refLabel = $" R: {refType?.Name}";
            EditorGUI.LabelField(labelRect,
                new GUIContent(refLabel, $"Reference \nType - {refType?.Name} \nNamespace - {refType?.Namespace}"),
                style);

            var newValue = EditorGUI.ObjectField(fieldRect, host, hostType, true);
            if (host != newValue)
            {
                if (newValue != null)
                {
                    TryApplyRefToValue(newValue, property.serializedObject.targetObject, targetType,
                        property.propertyPath);
                }
                else
                {
                    RefToExtensions.ResetRefTo(property);
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "RefTo.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());

            var (_, targetType, hostType, _, _) = RefToExtensions.GetInspectorValues(property);

            var fieldName = root.Q<Label>("property-name");
            fieldName.text = ObjectNames.NicifyVariableName(property.name);
            fieldName.tooltip =
                $"Field: {property.name} \nTarget Type: {targetType.Name} \nNamespace: {targetType.Namespace}";

            var propertyPath = property.propertyPath;
            root.TrackSerializedObjectValue(property.serializedObject, RefreshDynamic);

            var objectField = root.Q<ObjectField>();
            objectField.objectType = hostType;
            var targetObject = property.serializedObject.targetObject;
            objectField.RegisterValueChangedCallback(ApplyRefToFromObject);
            RefreshDynamic(property.serializedObject);

            void RefreshDynamic(SerializedObject so)
            {
                using var localProperty = so.FindProperty(propertyPath);
                var (refType, _, _, host, isSameType) = RefToExtensions.GetInspectorValues(localProperty);
                var refLabel = root.Q<Label>("ref-name");
                var refTypeName = refType == null ? "null" : refType.Name;
                refLabel.text = $"R:{refTypeName}";
                refLabel.tooltip = $"Reference \nType: {refType?.Name} \nNamespace: {refType?.Namespace}";
                refLabel.style.color = isSameType ? new StyleColor(Color.white) : new StyleColor(Color.red);
                root.Q<ObjectField>().SetValueWithoutNotify(host);
            }

            void ApplyRefToFromObject(ChangeEvent<Object> evt)
            {
                var newValue = evt.newValue;
                if (TryApplyRefToValue(newValue, targetObject, targetType, propertyPath) == false &&
                    newValue != null)
                {
                    objectField.SetValueWithoutNotify(evt.previousValue);
                }
            }
        }

        private bool TryApplyRefToValue(Object newValue, Object targetObject, Type targetType,
            string targetPropertyPath)
        {
            using var newValueSo = new SerializedObject(newValue);
            using var newValueIteratorProperty = newValueSo.GetIterator();
            newValueIteratorProperty.NextVisible(true);
            return PropertyUtils.TraverseProperty(newValueIteratorProperty, string.Empty, TryWriteToRefTo);


            bool TryWriteToRefTo(SerializedProperty refProperty)
            {
                var refType = TypeUtils.ExtractTypeFromString(refProperty.managedReferenceFullTypename);
                if (targetType.IsAssignableFrom(refType))
                {
                    using var targetSo = new SerializedObject(targetObject);
                    using var copyProperty = targetSo.FindProperty(targetPropertyPath);
                    RefToExtensions.WriteRefToFromPropertyToProperty(refProperty, copyProperty);
                    return true;
                }

                return false;
            }
        }
    }
}
#endif