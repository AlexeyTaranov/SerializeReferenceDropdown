using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class TypeUtils
    {
        private const string ArrayPropertySubstring = ".Array.data[";
        private static Dictionary<AppDomain, List<Type>> cachedDomainTypes = new Dictionary<AppDomain, List<Type>>();

        public static object CreateObjectFromType(Type type)
        {
            object newObject;
            if (type?.GetConstructor(Type.EmptyTypes) != null)
            {
                newObject = Activator.CreateInstance(type);
            }
            else
            {
                newObject = type != null ? FormatterServices.GetUninitializedObject(type) : null;
            }

            return newObject;
        }

        public static Type ExtractTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var splitFieldTypename = typeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            assemblyName = assemblyName == "Assembly" ? "Assembly-CSharp" : assemblyName;
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
            if (cachedDomainTypes.TryGetValue(currentDomain, out var cachedTypes))
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
                    Log.DevError(e);
                }
            }

            cachedDomainTypes.Add(currentDomain, types);

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

        private static Type[] GetBuiltInUnitySerializeTypes()
        {
            return GetDefaultTypes();
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

        private static IReadOnlyList<Type> systemObjectTypes;
        
        public static IReadOnlyList<Type> GetAllSystemObjectTypes()
        {
            if (systemObjectTypes == null)
            {
                var assemblies = CompilationPipeline.GetAssemblies();
                var playerAssemblies = assemblies.Where(t => t.flags.HasFlag(AssemblyFlags.EditorAssembly) == false)
                    .Select(t => t.name).ToArray();
                var baseType = typeof(object);
                var typesCollection = TypeCache.GetTypesDerivedFrom(baseType);
                var customTypes = typesCollection.Where(IsValidTypeForGenericParameter).OrderBy(t => t.FullName);

                var typesList = new List<Type>();
                typesList.AddRange(GetBuiltInUnitySerializeTypes());
                typesList.AddRange(customTypes);
                systemObjectTypes = typesList.ToArray();

                bool IsValidTypeForGenericParameter(Type t)
                {
                    var isUnityObjectType = t.IsSubclassOf(typeof(UnityEngine.Object));
                    var isFinalSerializeType = !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.IsSerializable;
                    var isEnum = t.IsEnum;
                    var isTargetType = playerAssemblies.Any(asm => t.Assembly.FullName.StartsWith(asm)) ||
                                       t.Assembly.FullName.StartsWith(nameof(UnityEngine));

                    return isTargetType && (isFinalSerializeType || isEnum || isUnityObjectType);
                }
            }

            return systemObjectTypes;
        }

        public static List<Type> GetAssignableSerializeReferenceTypes(SerializedProperty property)
        {
            var propertyType = ExtractTypeFromString(property.managedReferenceFieldTypename);
            return GetAssignableSerializeReferenceTypes(propertyType);
        }
        
        public static List<Type> GetAssignableSerializeReferenceTypes(Type propertyType)
        {
            var derivedTypes = TypeCache.GetTypesDerivedFrom(propertyType);
            var nonUnityTypes = derivedTypes.Where(IsAssignableNonUnityType).ToList();
            nonUnityTypes.Insert(0, null);
            if (propertyType.IsGenericType && propertyType.IsInterface)
            {
                var allTypes = GetAllTypesInCurrentDomain().Where(IsAssignableNonUnityType)
                    .Where(t => t.IsGenericType);

                var assignableGenericTypes = allTypes.Where(IsImplementedGenericInterfacesFromGenericProperty);
                nonUnityTypes.AddRange(assignableGenericTypes);
            }

            return nonUnityTypes;

            bool IsAssignableNonUnityType(Type type)
            {
                return IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }

            bool IsImplementedGenericInterfacesFromGenericProperty(Type type)
            {
                var interfaces = type.GetInterfaces().Where(t => t.IsGenericType);
                var isImplementedInterface = interfaces.Any(t =>
                    t.GetGenericTypeDefinition() == propertyType.GetGenericTypeDefinition());
                return isImplementedInterface;
            }
        }
    }
}