#if UNITY_2023_2_OR_NEWER
using System;
using System.Reflection;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public static class RefToExtensions
    {
        private const string HostPropertyName = "_host";
        private const string ReferenceIdName = "_referenceId";

        public static bool TryGetRefType(SerializedProperty property, out Type refToType, out Type hostType)
        {
            var toType = property.boxedValue?.GetType();
            refToType = null;
            hostType = null;
            if (IsGenericTypeOf(toType, typeof(RefTo<,>)))
            {
                refToType = toType.GenericTypeArguments[0];
                hostType = toType.GenericTypeArguments[1];
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
            
            SOUtils.RegisterUndo(toProperty, "Paste RefTo");
            
            SetValue(targetObject, HostPropertyName, fromProperty.serializedObject.targetObject);
            SetValue(targetObject, ReferenceIdName, fromProperty.managedReferenceId);
            toProperty.boxedValue = targetObject;
            toProperty.serializedObject.ApplyModifiedProperties();
            toProperty.serializedObject.Update();
        }

        private static (Object host, long id) GetRefToFieldsFromProperty(SerializedProperty property)
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
            SOUtils.RegisterUndo(property, "Reset RefTo");
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

        public static (Type refType, Type targetType, Type hostType, Object host, bool isSameType) GetInspectorValues(
            SerializedProperty property)
        {
            var (host, id) = GetRefToFieldsFromProperty(property);
            TryGetRefType(property, out var targetType, out var hostType);
            var isSameType = false;
            Type refType = null;
            if (host != null)
            {
                var reference = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(host, id);
                if (reference != null)
                {
                    refType = reference.GetType();
                    isSameType = targetType.IsAssignableFrom(refType);
                }
            }
            else
            {
                isSameType = true;
            }

            return (refType, targetType, hostType, host, isSameType);
        }
    }
}
#endif