using System;
using System.Reflection;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor
{
    public static class TypeUtils
    {
        private const string ArrayPropertySubstring = ".Array.data[";

        public static Type ExtractTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var splitFieldTypename = typeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            var subStringTypeName = splitFieldTypename[1];
            var assembly = Assembly.Load(assemblyName);
            var targetType = assembly.GetType(subStringTypeName);
            return targetType;
        }

        public static bool IsFinalAssignableType(Type type)
        {
            return type.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
        }

        public static bool IsArrayElement(this SerializedProperty property)
        {
            return property.propertyPath.Contains(ArrayPropertySubstring);
        }

        public static SerializedProperty GetArrayPropertyFromArrayElement(SerializedProperty property)
        {
            var path = property.propertyPath;
            var startIndexArrayPropertyPath = path.IndexOf(ArrayPropertySubstring);
            var propertyPath = path.Remove(startIndexArrayPropertyPath);
            return property.serializedObject.FindProperty(propertyPath);
        }
    }
}