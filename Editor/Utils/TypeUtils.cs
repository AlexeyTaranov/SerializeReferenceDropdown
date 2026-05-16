using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var assemblyNameEndIndex = typeName.IndexOf(' ');
            if (assemblyNameEndIndex < 0)
            {
                return Type.GetType(typeName, false);
            }

            var assemblyName = typeName.Substring(0, assemblyNameEndIndex);
            assemblyName = assemblyName == "Assembly" ? "Assembly-CSharp" : assemblyName;
            var subStringTypeName = typeName.Substring(assemblyNameEndIndex + 1);

            try
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    var type = assembly.GetType(subStringTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }
            catch (Exception)
            {
                // Assembly not found or invalid name
            }

            return Type.GetType($"{subStringTypeName}, {assemblyName}", false);
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
            if (TryGetGenericArgumentsFromTargetType(propertyType, genericType, out var genericArguments) == false)
            {
                return null;
            }

            if (genericArguments.Any(t => t == null || t.IsAbstract || t.IsInterface) ||
                AreGenericArgumentsValid(genericType, genericArguments) == false)
            {
                return null;
            }

            try
            {
                return genericType.MakeGenericType(genericArguments);
            }
            catch (Exception e)
            {
                Log.DevError(e);
                return null;
            }
        }

        public static bool TryGetGenericArgumentsFromTargetType(Type targetType, Type genericType,
            out Type[] genericArguments)
        {
            genericArguments = null;
            if (targetType?.IsGenericType != true || genericType?.IsGenericTypeDefinition != true)
            {
                return false;
            }

            var matchingGenericType = GetMatchingGenericType(targetType, genericType);
            if (matchingGenericType == null)
            {
                return false;
            }

            var genericParameterMap = new Dictionary<Type, Type>();
            if (TryMapGenericArguments(matchingGenericType, targetType, genericParameterMap) == false)
            {
                return false;
            }

            var genericParameters = genericType.GetGenericArguments();
            genericArguments = genericParameters.Select(t =>
                genericParameterMap.TryGetValue(t, out var mappedType) ? mappedType : null).ToArray();
            return true;
        }

        private static Type GetMatchingGenericType(Type targetType, Type genericType)
        {
            var targetGenericDefinition = targetType.GetGenericTypeDefinition();
            var genericInterfaces = genericType.GetInterfaces();
            var matchingInterface = genericInterfaces.FirstOrDefault(IsMatchingGenericType);
            if (matchingInterface != null)
            {
                return matchingInterface;
            }

            var currentBaseType = genericType.BaseType;
            while (currentBaseType != null)
            {
                if (IsMatchingGenericType(currentBaseType))
                {
                    return currentBaseType;
                }

                currentBaseType = currentBaseType.BaseType;
            }

            return null;

            bool IsMatchingGenericType(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == targetGenericDefinition;
            }
        }

        private static bool TryMapGenericArguments(Type sourceType, Type targetType,
            Dictionary<Type, Type> genericParameterMap)
        {
            if (sourceType.IsGenericParameter)
            {
                if (genericParameterMap.TryGetValue(sourceType, out var mappedType))
                {
                    return mappedType == targetType;
                }

                genericParameterMap[sourceType] = targetType;
                return true;
            }

            if (sourceType.IsArray && targetType.IsArray)
            {
                return TryMapGenericArguments(sourceType.GetElementType(), targetType.GetElementType(),
                    genericParameterMap);
            }

            if (sourceType.IsGenericType && targetType.IsGenericType &&
                sourceType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
            {
                var sourceArguments = sourceType.GetGenericArguments();
                var targetArguments = targetType.GetGenericArguments();
                for (int i = 0; i < sourceArguments.Length; i++)
                {
                    if (TryMapGenericArguments(sourceArguments[i], targetArguments[i], genericParameterMap) == false)
                    {
                        return false;
                    }
                }

                return true;
            }

            return sourceType == targetType;
        }

        public static bool AreGenericArgumentsValid(Type genericType, IReadOnlyList<Type> genericArguments)
        {
            if (genericType?.IsGenericType != true)
            {
                return false;
            }

            var genericParameters = genericType.GetGenericArguments();
            if (genericParameters.Length != genericArguments.Count)
            {
                return false;
            }

            for (int i = 0; i < genericParameters.Length; i++)
            {
                if (IsGenericArgumentValid(genericParameters[i], genericArguments[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsGenericArgumentValid(Type genericParameter, Type genericArgument)
        {
            if (genericParameter?.IsGenericParameter != true || genericArgument == null)
            {
                return false;
            }

            var attributes = genericParameter.GenericParameterAttributes;
            var specialConstraints = attributes & GenericParameterAttributes.SpecialConstraintMask;
            if (specialConstraints.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) &&
                genericArgument.IsValueType)
            {
                return false;
            }

            if (specialConstraints.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) &&
                (genericArgument.IsValueType == false || Nullable.GetUnderlyingType(genericArgument) != null))
            {
                return false;
            }

            if (specialConstraints.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
                HasDefaultConstructor(genericArgument) == false)
            {
                return false;
            }

            var constraints = genericParameter.GetGenericParameterConstraints();
            foreach (var constraint in constraints)
            {
                if (SatisfiesGenericConstraint(constraint, genericArgument) == false)
                {
                    return false;
                }
            }

            return true;

            bool HasDefaultConstructor(Type type)
            {
                return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
            }

            bool SatisfiesGenericConstraint(Type constraint, Type argument)
            {
                if (constraint.IsAssignableFrom(argument))
                {
                    return true;
                }

                if (constraint.IsGenericType == false)
                {
                    return false;
                }

                var constraintDefinition = constraint.GetGenericTypeDefinition();
                return argument.GetInterfaces().Any(IsMatchingGenericConstraint) ||
                       IsMatchingBaseGenericConstraint(argument.BaseType);

                bool IsMatchingGenericConstraint(Type type)
                {
                    return type.IsGenericType && type.GetGenericTypeDefinition() == constraintDefinition;
                }

                bool IsMatchingBaseGenericConstraint(Type type)
                {
                    while (type != null)
                    {
                        if (IsMatchingGenericConstraint(type))
                        {
                            return true;
                        }

                        type = type.BaseType;
                    }

                    return false;
                }
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
            if (propertyType.IsGenericType)
            {
                var allTypes = GetAllTypesInCurrentDomain().Where(IsAssignableNonUnityType)
                    .Where(t => t.IsGenericType);

                var assignableGenericTypes = allTypes.Where(IsAssignableGenericTypeFromGenericProperty);
                nonUnityTypes.AddRange(assignableGenericTypes);
            }

            return nonUnityTypes.Distinct().ToList();

            bool IsAssignableNonUnityType(Type type)
            {
                return IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }

            bool IsAssignableGenericTypeFromGenericProperty(Type type)
            {
                return TryGetGenericArgumentsFromTargetType(propertyType, type, out _);
            }
        }
    }
}
