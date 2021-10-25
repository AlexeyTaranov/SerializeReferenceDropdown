using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            return types;
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