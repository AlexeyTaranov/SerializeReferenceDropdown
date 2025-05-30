using System;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [InitializeOnLoad]
    public class DropdownCopyPasteContextMenu
    {
        private static (string json, Type type) lastObject;

        static DropdownCopyPasteContextMenu()
        {
            EditorApplication.contextualPropertyMenu += ShowSerializeReferenceCopyPasteContextMenu;
        }

        private static void ShowSerializeReferenceCopyPasteContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var copyProperty = property.Copy();
                menu.AddItem(new GUIContent("Copy Serialize Reference"), false,
                    (_) => { CopyReferenceValue(copyProperty); }, null);
                var pasteAsValueContent = new GUIContent("Paste Serialize Reference as Value");
                menu.AddItem(pasteAsValueContent, false, (_) => PasteAsValue(copyProperty),
                    null);
            }
        }

        private static void CopyReferenceValue(SerializedProperty property)
        {
            var refValue = GetReferenceToValueFromSerializerPropertyReference(property);
            lastObject.json = JsonUtility.ToJson(refValue);
            lastObject.type = refValue?.GetType();
        }

        private static void PasteAsValue(SerializedProperty property)
        {
            try
            {
                SOUtils.RegisterUndo(property, "Paste reference value");
                if (lastObject.type != null)
                {
                    var pasteObj = JsonUtility.FromJson(lastObject.json, lastObject.type);
                    property.managedReferenceValue = pasteObj;
                }
                else
                {
                    property.managedReferenceValue = null;
                }

                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            }
            catch (Exception e)
            {
                Log.DevError($"Failed paste value: {e}");
            }
        }

        private static object GetReferenceToValueFromSerializerPropertyReference(SerializedProperty property)
        {
#if UNITY_2021_2_OR_NEWER
            return property.managedReferenceValue;
#else
            return property.GetTarget();
#endif
        }
    }
}