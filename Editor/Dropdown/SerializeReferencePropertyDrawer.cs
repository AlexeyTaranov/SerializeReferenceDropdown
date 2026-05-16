using System;
using System.Collections.Generic;
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

        private IReadOnlyList<Type> GetAssignableTypes(SerializedProperty property)
        {
            var srdAttribute = (SerializeReferenceDropdownAttribute)attribute;
            var notNull = srdAttribute.Flags.HasFlag(SRDFlags.NotNull);
            var types = PropertyDrawerTypesUtils.GetAssignableTypes(property);
            if (notNull)
            {
                types.RemoveAt(0);
            }

            return types;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            imguiImpl ??= new PropertyDrawerIMGUI(GetAssignableTypes(property));
            imguiImpl.OnGUI(rect, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var srdAttribute = (SerializeReferenceDropdownAttribute)attribute;
                uiToolkitImpl = new PropertyDrawerUIToolkit(property, GetAssignableTypes(property), root, srdAttribute);
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