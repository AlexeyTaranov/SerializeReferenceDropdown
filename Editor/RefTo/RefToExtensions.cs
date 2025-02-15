#if UNITY_2023_2_OR_NEWER
using System;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public static class RefToExtensions
    {
        private const string HostPropertyName = "_host";
        private const string ReferenceIdName = "_referenceId";

        public static bool TryGetRefType(SerializedProperty property, out Type refToType)
        {
            var toType = property.boxedValue?.GetType();
            refToType = null;
            if (IsGenericTypeOf(toType, typeof(RefTo<,>)))
            {
                refToType = toType.GenericTypeArguments[0];
                return true;
            }

            return false;
        }

        private static bool IsGenericTypeOf(Type type, Type genericTypeDefinition)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        public static void WriteRefToFromPropertyToProperty(SerializedProperty fromProperty,
            SerializedProperty toProperty)
        {
            var targetObject = toProperty.boxedValue;
            SetValue(targetObject, HostPropertyName, fromProperty.serializedObject.targetObject);
            SetValue(targetObject, ReferenceIdName, fromProperty.managedReferenceId);
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
            SetValue(targetObject, HostPropertyName, null);
            SetValue(targetObject, ReferenceIdName, 0);
            property.boxedValue = targetObject;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        private static void SetValue(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static object GetValue(object target, string fieldName)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(target);
        }
    }
}
#endif