#if UNITY_2023_2_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [InitializeOnLoad]
    public class RefToContextMenu
    {
        private static (string propertyPath, Object host, Type referenceType) _copy;

        static RefToContextMenu()
        {
            EditorApplication.contextualPropertyMenu += ShowRefToContextMenu;
        }

        private static void ShowRefToContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var copy = property.Copy();
                menu.AddItem(new GUIContent("RefTo: Copy Serialize Reference"), false,
                    (_) => CopyProperty(copy), null);
            }

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                var copy = property.Copy();

                if (property.isArray == false && RefToExtensions.TryGetRefType(copy, out var targetType, out _))
                {
                    menu.AddItem(new GUIContent("RefTo: Reset"), false,
                        (_) => { RefToExtensions.ResetRefTo(copy); },
                        null);

                    var isSameType = _copy.referenceType != null && targetType.IsAssignableFrom(_copy.referenceType);
                    var typeName = _copy.referenceType?.Name;

                    if (isSameType)
                    {
                        menu.AddItem(new GUIContent($"RefTo: Paste Serialize Reference: {typeName}"), false,
                            (_) => { PasteToProperty(copy); },
                            null);
                    }
                    else if (_copy.host != null && _copy.referenceType != null)
                    {
                        menu.AddDisabledItem(
                            new GUIContent($"RefTo: Paste Serialize Reference - incorrect type : {typeName}"));
                    }
                }
            }
        }


        private static void CopyProperty(SerializedProperty property)
        {
            var path = property.propertyPath;
            var host = property.serializedObject.targetObject;
            var type = property.managedReferenceValue.GetType();
            _copy = (path, host, type);
        }

        private static void PasteToProperty(SerializedProperty pasteProperty)
        {
            if (_copy.host != null)
            {
                using var so = new SerializedObject(_copy.host);
                using var property = so.FindProperty(_copy.propertyPath);
                RefToExtensions.WriteRefToFromPropertyToProperty(property, pasteProperty);
            }
        }
    }
}
#endif