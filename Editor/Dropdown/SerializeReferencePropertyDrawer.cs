using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public partial class SerializeReferencePropertyDrawer : PropertyDrawer
    {
        private PropertyDrawerIMGUI imguiImpl;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            imguiImpl ??= new PropertyDrawerIMGUI(PropertyDrawerTypesUtils.GetAssignableTypes(property));
            imguiImpl.OnGUI(rect, property, label);
        }

        private const string NullName = "null";
        private List<Type> assignableTypes;
        private Rect propertyRect;

        private static Object pingObject;
        private static Object previousSelection;
        private static long pingRefId;

        private static Dictionary<SerializeReferencePropertyDrawer, SerializedProperty> _allPropertyDrawers =
            new Dictionary<SerializeReferencePropertyDrawer, SerializedProperty>();

        private Action _pingSelf;

        public static void PingSerializeReference(Object selectionObject, long refId)
        {
            previousSelection = Selection.activeObject;
            Selection.activeObject = selectionObject;
            pingObject = selectionObject;
            pingRefId = refId;
            PingAll();

            EditorApplication.delayCall += () =>
            {
                previousSelection = null;
                pingObject = null;
                pingRefId = -1;
            };
        }

        private static void PingAll()
        {
            foreach (var pair in _allPropertyDrawers)
            {
                if (pair.Value != null)
                {
                    pair.Key.PingSelf();
                }
            }
        }

        private void PingSelf()
        {
            _pingSelf.Invoke();
        }
    }
}