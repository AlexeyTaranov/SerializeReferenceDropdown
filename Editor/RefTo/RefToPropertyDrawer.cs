using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<,>))]
    [CustomPropertyDrawer(typeof(RefTo<>))]
    public class RefToPropertyDrawer : PropertyDrawer
    {
        private static RefToPropertyDrawerUIToolkit _uiToolkitDrawer;
        private static RefToPropertyDrawerIMGUI _imguiDrawer;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, false);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _uiToolkitDrawer ??= new RefToPropertyDrawerUIToolkit();
            return _uiToolkitDrawer.CreatePropertyGUI(property);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            _imguiDrawer ??= new RefToPropertyDrawerIMGUI();
            _imguiDrawer.OnGUI(rect, property, label);
        }
    }
}