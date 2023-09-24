using System;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    [InitializeOnLoad]
    public class SerializeReferenceCopyPasteContextMenu
    {
        private static (string json, Type type) _lastObject;

        static SerializeReferenceCopyPasteContextMenu()
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
                var pasteContent = new GUIContent("Paste Serialize Reference");
                menu.AddItem(pasteContent, false, (_) => PasteReferenceValue(copyProperty),
                    null);
            }
        }

        private static void CopyReferenceValue(SerializedProperty property)
        {
            var refValue = GetReferenceToValueFromSerializerPropertyReference(property);
            _lastObject.json = JsonUtility.ToJson(refValue);
            _lastObject.type = refValue?.GetType();
        }

        private static void PasteReferenceValue(SerializedProperty property)
        {
            try
            {
                if (_lastObject.type != null)
                {
                    var pasteObj = JsonUtility.FromJson(_lastObject.json, _lastObject.type);
                    property.managedReferenceValue = pasteObj;
                }
                else
                {
                    property.managedReferenceValue = null;
                }
                
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                SerializeReferenceDropdownPropertyDrawer.UpdateDropdownCallback?.Invoke();
            }
            catch (Exception e)
            {
                // ignored
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