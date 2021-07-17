using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SRD.Editor
{
    public static class ReflectionUtils
    {
        public static Type ExtractReferenceFieldTypeFromSerializedProperty(SerializedProperty property)
        {
            var fieldTypeName = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(fieldTypeName)) return null;
            var splitFieldTypename = fieldTypeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            var typeName = splitFieldTypename[1];
            var assembly = Assembly.Load(assemblyName);
            var targetType = assembly.GetType(typeName);
            return targetType;
        }

        public static IEnumerable<Type> GetAllTypesInCurrentDomain()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes());
        }

        public static Type[] GetFinalAssignableTypes(Type baseType, IEnumerable<Type> types)
        {
            bool IsFinalClass(Type type)
            {
                return baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
            }

            return types.Where(IsFinalClass).ToArray();
        }
    }
}