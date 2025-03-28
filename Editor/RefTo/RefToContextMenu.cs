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
        private static (string propertyPath, Object host, Type referenceType) copy;

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

                    var isSameType = RefToContextMenu.copy.referenceType != null && targetType.IsAssignableFrom(RefToContextMenu.copy.referenceType);
                    var typeName = RefToContextMenu.copy.referenceType?.Name;

                    if (isSameType)
                    {
                        menu.AddItem(new GUIContent($"RefTo: Paste Serialize Reference: {typeName}"), false,
                            (_) => { PasteToProperty(copy); },
                            null);
                    }
                    else if (RefToContextMenu.copy.host != null && RefToContextMenu.copy.referenceType != null)
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
            copy = (path, host, type);
        }

        private static void PasteToProperty(SerializedProperty pasteProperty)
        {
            if (copy.host != null)
            {
                using var so = new SerializedObject(copy.host);
                using var property = so.FindProperty(copy.propertyPath);
                RefToExtensions.WriteRefToFromPropertyToProperty(property, pasteProperty);
            }
        }
    }
}
#endif