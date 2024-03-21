using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    public static class TypeUtils
    {
        private const string ArrayPropertySubstring = ".Array.data[";
        private static Dictionary<AppDomain, List<Type>> CachedDomainTypes = new Dictionary<AppDomain, List<Type>>();

        public static Type ExtractTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var splitFieldTypename = typeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            var subStringTypeName = splitFieldTypename[1];
            if (splitFieldTypename.Length > 2)
            {
                subStringTypeName = typeName.Substring(assemblyName.Length + 1);
            }

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


        public static IEnumerable<Type> GetAllTypesInCurrentDomain()
        {
            var currentDomain = AppDomain.CurrentDomain;
            if (CachedDomainTypes.TryGetValue(currentDomain, out var cachedTypes))
            {
                return cachedTypes;
            }

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

            CachedDomainTypes.Add(currentDomain, types);

            return types;
        }

        public static Type GetConcreteGenericType(Type propertyType, Type genericType)
        {
            if (propertyType.IsGenericType && CanCreateDirectGenericType())
            {
                var genericConcreteType = genericType.MakeGenericType(propertyType.GetGenericArguments());
                return genericConcreteType;
            }

            return null;

            bool CanCreateDirectGenericType()
            {
                var genericArguments = genericType.GetInterfaces();
                var interfaceIndex = Array.FindIndex(genericArguments,
                    argType => argType.IsGenericType &&
                               argType.GetGenericTypeDefinition() == propertyType.GetGenericTypeDefinition());
                var isHaveSameArgumentsCount =
                    propertyType.GetGenericArguments().Length == genericType.GetGenericArguments().Length &&
                    interfaceIndex != -1;
                var anyAbstract = propertyType.GetGenericArguments().Any(t => t.IsAbstract);
                return isHaveSameArgumentsCount && anyAbstract == false;
            }
        }

        public static Type[] GetBuiltInUnitySerializeTypes()
        {
            var list = new List<Type>();
            var types = GetDefaultTypes();
            var arrayTypes = types.Select(t => t.MakeArrayType());
            list.AddRange(types);
            list.AddRange(arrayTypes);
            return list.ToArray();
        }

        private static Type[] GetDefaultTypes()
        {
            return new[]
            {
                typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int),
                typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string),

                typeof(Color), typeof(Color32), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion),
                typeof(Ray), typeof(Ray2D)
            };
        }
    }
}