using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor
{
    public static class ReflectionUtils
    {
        private static readonly Dictionary<AppDomain, List<Type>> CachedDomainTypes =
            new Dictionary<AppDomain, List<Type>>();

        public static Type ExtractTypeFromString(string managerReferenceTypeName)
        {
            if (string.IsNullOrEmpty(managerReferenceTypeName))
            {
                return null;
            }

            var splitFieldTypename = managerReferenceTypeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            var typeName = splitFieldTypename[1];
            var assembly = Assembly.Load(assemblyName);
            var targetType = assembly.GetType(typeName);
            return targetType;
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

        public static IEnumerable<Type> GetFinalAssignableTypes(Type baseType, IEnumerable<Type> types,
            Func<Type, bool> predicate = null)
        {
            bool IsFinalClass(Type type)
            {
                return baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
            }

            return predicate == null
                ? types.Where(IsFinalClass)
                : types.Where(type => IsFinalClass(type) && predicate.Invoke(type));
        }
    }
}