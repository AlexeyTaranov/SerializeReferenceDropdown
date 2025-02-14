using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            var (name, host) = GetInspectorValues(property);
            var elementRect = position;
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.position += new Vector2(EditorGUIUtility.labelWidth, 0);
            EditorGUI.LabelField(elementRect, $"Ref to: {name}");
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
                var (name, host) = GetInspectorValues(localProperty);
                objectField.value = host;
                root.Q<Label>("RefName").text = name;
            }
        }

        private (string name, Object host) GetInspectorValues(SerializedProperty property)
        {
            var name = "null";
            var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(property);
            if (host != null)
            {
                var reference = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(host, id);
                if (reference != null)
                {
                    name = ObjectNames.NicifyVariableName(reference.GetType().Name);
                }
            }

            return (name, host);
        }
    }
}