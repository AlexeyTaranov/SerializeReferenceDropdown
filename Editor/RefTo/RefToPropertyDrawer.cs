using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

#if UNITY_2022_3_OR_NEWER
namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<>))]
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
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private static GUIStyle _errorStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            var (refName, targetName, host, isSameType) = GetInspectorValues(property);
            var elementRect = position;
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.position += new Vector2(EditorGUIUtility.labelWidth, 0);
            var refLabel = $"Ref to: {refName} {targetName}";
            if (isSameType)
            {
                EditorGUI.LabelField(elementRect, refLabel);
            }
            else
            {
                _errorStyle ??= new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } };
                EditorGUI.LabelField(elementRect, refLabel, _errorStyle);
            }

            elementRect.position += new Vector2(120, 0);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(elementRect, host, typeof(Object), true);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndProperty();
        }

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            var uiToolkitLayoutPath = "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/RefTo.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());

            var objectField = root.Q<ObjectField>();
            root.Q<PropertyField>().BindProperty(property);
            var propertyPath = property.propertyPath;

            root.TrackSerializedObjectValue(property.serializedObject, RefreshName);
            RefreshName(property.serializedObject);


            void RefreshName(SerializedObject so)
            {
                var localProperty = so.FindProperty(propertyPath);
                var (refName, targetName, host, isSameType) = GetInspectorValues(localProperty);
                objectField.value = host;
                var label = root.Q<Label>("RefName");
                root.Q<Label>("Target").text = targetName;
                label.style.color = isSameType ? new StyleColor(Color.white) : new StyleColor(Color.red);
                label.text = refName;
            }
        }

        private (string referenceName, string targetName, Object host, bool isSameType) GetInspectorValues(
            SerializedProperty property)
        {
            var refName = "null";
            var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(property);
            RefToExtensions.TryGetRefType(property, out var targetType);
            var targetName = $"[{TypeToName(targetType)}]";
            if (host != null)
            {
                var reference = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(host, id);
                if (reference != null)
                {
                    refName = TypeToName(reference.GetType());
                    var isSameType = targetType.IsAssignableFrom(reference.GetType());
                    if (isSameType)
                    {
                        return (refName, targetName, host, true);
                    }
                }
            }

            return (refName, targetName, host, host == null);

            string TypeToName(Type type) => ObjectNames.NicifyVariableName(type?.Name);
        }
    }
}
#endif