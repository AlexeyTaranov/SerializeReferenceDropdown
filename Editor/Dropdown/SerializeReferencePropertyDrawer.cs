using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferencePropertyDrawer : PropertyDrawer
    {
        private PropertyDrawerIMGUI imguiImpl;
        private PropertyDrawerUIToolkit uiToolkitImpl;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            imguiImpl ??= new PropertyDrawerIMGUI(PropertyDrawerTypesUtils.GetAssignableTypes(property));
            imguiImpl.OnGUI(rect, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                uiToolkitImpl = new PropertyDrawerUIToolkit(property,
                    PropertyDrawerTypesUtils.GetAssignableTypes(property), root);
                uiToolkitImpl.CreateUIToolkitLayout();
            }
            else
            {
                root.Add(new PropertyField(property));
            }

            return root;
        }
    }
}