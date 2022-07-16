using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    [InitializeOnLoad]
    public class SerializeReferenceCopyPasteContextMenu
    {
        private static SerializedProperty _lastCopyProperty;

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
                    (_) =>
                    {
                        _lastCopyProperty = copyProperty;
                        CopyReferenceValueToClipBoard(copyProperty);
                    }, null);
                var pasteContent = new GUIContent("Paste Serialize Reference Value");
                if (CanPasteValueFromClipBoard())
                {
                    menu.AddItem(pasteContent, false, (_) => PasteReferenceValueFromClipBoard(copyProperty),
                        null);
                }
                else
                {
                    menu.AddDisabledItem(pasteContent);
                }

                var pasteRefContent = new GUIContent("Paste Serialize Reference as Ref");
                var pasteDeepCopyContent = new GUIContent("Paste Serialize Reference as Deep Copy");
                if (_lastCopyProperty != null)
                {
                    menu.AddItem(pasteRefContent, false, (_) => PasteAsReference(_lastCopyProperty, copyProperty),
                        null);
                    menu.AddItem(pasteDeepCopyContent, false,
                        (_) => PasteAsDeepCopy(_lastCopyProperty, copyProperty), null);
                }
                else
                {
                    menu.AddDisabledItem(pasteRefContent, false);
                    menu.AddDisabledItem(pasteDeepCopyContent, false);
                }
            }
        }

        private static void CopyReferenceValueToClipBoard(SerializedProperty property)
        {
            var refValue = GetReferenceToValueFromSerializerPropertyReference(property);
            var stringValue = JsonUtility.ToJson(refValue);
            EditorGUIUtility.systemCopyBuffer = stringValue;
        }

        private static bool CanPasteValueFromClipBoard()
        {
            //TODO need learn how to check can paste values to target type =_=
            var stringValue = EditorGUIUtility.systemCopyBuffer;
            var isValueType = stringValue?.StartsWith("{") == true && stringValue?.EndsWith("}") == true;
            return isValueType;
        }

        private static void PasteReferenceValueFromClipBoard(SerializedProperty property)
        {
            var stringValue = EditorGUIUtility.systemCopyBuffer;
            var refValue = GetReferenceToValueFromSerializerPropertyReference(property);
            JsonUtility.FromJsonOverwrite(stringValue, refValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        private static void PasteAsReference(SerializedProperty from, SerializedProperty to)
        {
            var value = GetReferenceToValueFromSerializerPropertyReference(from);
            to.managedReferenceValue = value;
            to.serializedObject.ApplyModifiedProperties();
            to.serializedObject.Update();
        }

        private static void PasteAsDeepCopy(SerializedProperty from, SerializedProperty to)
        {
            var value = GetReferenceToValueFromSerializerPropertyReference(from);
            var deepCopy = GetDeepCopy(value);
            to.managedReferenceValue = deepCopy;
            to.serializedObject.ApplyModifiedProperties();
            to.serializedObject.Update();
        }

        private static object GetDeepCopy(object source)
        {
            MethodInfo memberwiseClone;
            memberwiseClone =
                source?.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return memberwiseClone?.Invoke(source, null);
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
