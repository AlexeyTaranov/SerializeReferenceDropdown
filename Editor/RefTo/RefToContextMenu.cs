#if UNITY_2023_2_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public enum PasteType
    {
        NoData,
        IncorrectType,
        CanPaste
    }

    [InitializeOnLoad]
    public class RefToContextMenu
    {
        public static (long refId, Object host, Type referenceType) copy;

        static RefToContextMenu()
        {
            EditorApplication.contextualPropertyMenu += ShowRefToContextMenu;
        }

        private static void ShowRefToContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var copyProperty = property.Copy();
                menu.AddItem(new GUIContent("RefTo: Copy Serialize Reference"), false,
                    (_) => CopyProperty(copyProperty), null);
            }

            if (IsRefToType(property))
            {
                var copyProperty = property.Copy();
                menu.AddItem(new GUIContent("RefTo: Reset"), false,
                    (_) => { RefToExtensions.ResetRefTo(copyProperty); }, null);
                var getPasteType = GetPasteType(property);
                var typeName = copy.referenceType?.Name;
                switch (getPasteType)
                {
                    case PasteType.IncorrectType:
                        menu.AddDisabledItem(
                            new GUIContent($"RefTo: Paste Serialize Reference - incorrect type : {typeName}"));
                        break;
                    case PasteType.CanPaste:
                        menu.AddItem(new GUIContent($"RefTo: Paste Serialize Reference: {typeName}"), false,
                            (_) => { PasteToProperty(copyProperty); },
                            null);
                        break;
                }
            }
        }

        private static bool IsRefToType(SerializedProperty property)
        {
            return property.isArray == false && RefToExtensions.TryGetRefType(property, out _, out _);
        }

        public static PasteType GetPasteType(SerializedProperty property)
        {
            if (copy.host == null)
            {
                return PasteType.NoData;
            }

            using var so = new SerializedObject(copy.host);
            var value = ManagedReferenceUtility.GetManagedReference(copy.host, copy.refId);
            if (value == null)
            {
                return PasteType.NoData;
            }

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                var copy = property.Copy();

                if (property.isArray == false && RefToExtensions.TryGetRefType(copy, out var targetType, out _))
                {
                    var isSameType = RefToContextMenu.copy.referenceType != null &&
                                     targetType.IsAssignableFrom(RefToContextMenu.copy.referenceType);
                    if (isSameType)
                    {
                        return PasteType.CanPaste;
                    }

                    if (RefToContextMenu.copy.host != null && RefToContextMenu.copy.referenceType != null)
                    {
                        return PasteType.IncorrectType;
                    }
                }
            }

            return PasteType.NoData;
        }

        private static void CopyProperty(SerializedProperty property)
        {
            var host = property.serializedObject.targetObject;
            var type = property.managedReferenceValue.GetType();
            var id = property.managedReferenceId;
            copy = (id, host, type);
        }

        public static void CopyDirectValues((long refId, Object host, Type referenceType) tuple)
        {
            copy = tuple;
        }

        public static void PasteToProperty(SerializedProperty pasteProperty)
        {
            if (copy.host != null)
            {
                using var so = new SerializedObject(copy.host);
                var value = ManagedReferenceUtility.GetManagedReference(copy.host, copy.refId);
                if (value != null)
                {
                    RefToExtensions.WriteRefDataToProperty(copy.refId, copy.host, pasteProperty);
                }
            }
        }
    }
}
#endif