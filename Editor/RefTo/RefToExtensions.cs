using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public static class RefToExtensions
    {
        private const string HostPropertyName = "_host";
        private const string ReferenceIdName = "_referenceId";

        public static void WriteRefToFromPropertyToProperty(SerializedProperty fromProperty,
            SerializedProperty toProperty)
        {
            var targetObject = toProperty.boxedValue;
            WriteTo(targetObject, HostPropertyName, fromProperty.serializedObject.targetObject);
            WriteTo(targetObject, ReferenceIdName, fromProperty.managedReferenceId);
            toProperty.boxedValue = targetObject;
            toProperty.serializedObject.ApplyModifiedProperties();
            toProperty.serializedObject.Update();
        }

        public static (Object host, long id) GetRefToFieldsFromProperty(SerializedProperty property)
        {
            var targetObject = property.boxedValue;
            if (targetObject != null)
            {
                var host = GetValue(targetObject, HostPropertyName);
                var id = GetValue(targetObject, ReferenceIdName);
                return (host as Object, (long)id);
            }

            return default;
        }

        public static void ResetRefTo(SerializedProperty property)
        {
            var targetObject = property.boxedValue;
            WriteTo(targetObject, HostPropertyName, null);
            WriteTo(targetObject, ReferenceIdName, 0);
            property.boxedValue = targetObject;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        private static void WriteTo(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static object GetValue(object target, string fieldName)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(target);
        }
    }
}