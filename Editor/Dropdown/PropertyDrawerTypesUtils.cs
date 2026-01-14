using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    public static class PropertyDrawerTypesUtils
    {
        private const string NullName = "null";

        public static string GetTypeName(Type type)
        {
            if (type == null)
            {
                return NullName;
            }

            var typesWithNames = TypeCache.GetTypesWithAttribute(typeof(SerializeReferenceDropdownNameAttribute));
            if (typesWithNames.Contains(type))
            {
                var dropdownNameAttribute = type.GetCustomAttribute<SerializeReferenceDropdownNameAttribute>();
                return dropdownNameAttribute.Name;
            }

            if (type.IsGenericType)
            {
                var genericNames = type.GenericTypeArguments.Select(t => t.Name);
                var genericParamNames = " [" + string.Join(",", genericNames) + "]";
                var genericName = ObjectNames.NicifyVariableName(type.Name) + genericParamNames;
                return genericName;
            }

            if (type.IsNested)
            {
                var typeName = type.FullName;
                var lastDot = typeName?.LastIndexOf('.');
                if (lastDot > 0)
                {
                    typeName = typeName.Substring(lastDot.Value + 1);
                }

                return ObjectNames.NicifyVariableName(typeName);
            }

            return ObjectNames.NicifyVariableName(type.Name);
        }

        public static string GetTypeTooltip(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithTooltip = TypeCache.GetTypesWithAttribute(typeof(TypeTooltipAttribute));
            if (typesWithTooltip.Contains(type))
            {
                var tooltipAttribute = type.GetCustomAttribute<TypeTooltipAttribute>();
                return tooltipAttribute.tooltip;
            }

            return String.Empty;
        }

        public static List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var derivedTypes = TypeCache.GetTypesDerivedFrom(propertyType);
            var nonUnityTypes = derivedTypes.Where(IsAssignableNonUnityType).ToList();
            nonUnityTypes.Insert(0, null);
            if (propertyType.IsGenericType && propertyType.IsInterface)
            {
                var allTypes = TypeUtils.GetAllTypesInCurrentDomain().Where(IsAssignableNonUnityType)
                    .Where(t => t.IsGenericType);

                var assignableGenericTypes = allTypes.Where(IsImplementedGenericInterfacesFromGenericProperty);
                nonUnityTypes.AddRange(assignableGenericTypes);
            }

            return nonUnityTypes;

            bool IsAssignableNonUnityType(Type type)
            {
                return TypeUtils.IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }

            bool IsImplementedGenericInterfacesFromGenericProperty(Type type)
            {
                var interfaces = type.GetInterfaces().Where(t => t.IsGenericType);
                var isImplementedInterface = interfaces.Any(t =>
                    t.GetGenericTypeDefinition() == propertyType.GetGenericTypeDefinition());
                return isImplementedInterface;
            }
        }

        public static void WriteNewInstanceByType(Type newType,
            SerializedProperty property, Rect propertyRect, bool registerUndo)
        {
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);

            if (newType?.IsGenericType == true)
            {
                var concreteGenericType = TypeUtils.GetConcreteGenericType(propertyType, newType);
                if (concreteGenericType != null)
                {
                    CreateAndApplyNewInstanceFromType(concreteGenericType, property, registerUndo);
                }
                else
                {
                    GenericTypeCreateWindow.ShowCreateTypeMenu(property, propertyRect, newType,
                        (type) => CreateAndApplyNewInstanceFromType(type, property, registerUndo));
                }
            }
            else
            {
                CreateAndApplyNewInstanceFromType(newType, property, registerUndo);
            }
        }

        public static void CreateAndApplyNewInstanceFromType(Type type, SerializedProperty property, bool registerUndo)
        {
            var newObject = TypeUtils.CreateObjectFromType(type);
            ApplyValueToProperty(newObject);

            void ApplyValueToProperty(object value)
            {
                var targets = property.serializedObject.targetObjects;

                // Multiple object edit.
                //One Serialized Object for multiple Objects work sometimes incorrectly  
                foreach (var target in targets)
                {
                    using var so = new SerializedObject(target);
                    var targetProperty = so.FindProperty(property.propertyPath);
                    var previousJsonData = string.Empty;
                    if (targetProperty.managedReferenceValue != null)
                    {
                        previousJsonData = JsonUtility.ToJson(targetProperty.managedReferenceValue);
                    }

                    if (value != null)
                    {
                        JsonUtility.FromJsonOverwrite(previousJsonData, value);
                    }

                    targetProperty.managedReferenceValue = value;

                    so.ApplyModifiedProperties();
                    so.Update();
                }

                if (registerUndo)
                {
                    SOUtils.RegisterUndoMultiple(targets, "Apply new type");
                }
            }
        }

        public static void OpenSourceFile(Type type)
        {
            var (filePath, lineNumber, columnNumber) = CodeAnalysis.GetSourceFileLocation(type);

            if (string.IsNullOrEmpty(filePath) == false)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(filePath);
                AssetDatabase.OpenAsset(asset, lineNumber, columnNumber);
            }
        }

        public static void ShowSearchTool(Type type)
        {
            SearchToolWindow.ShowSearchTypeWindow(type);
        }


        public static bool TryGetMissingType(SerializedProperty property, string assetPath,
            out ManagedReferenceMissingType missingType)
        {
            var checkObject = property.serializedObject.targetObject;
            missingType = default;
            var haveMissingTypes = SerializationUtility.HasManagedReferencesWithMissingTypes(checkObject);
            if (haveMissingTypes == false)
            {
                return false;
            }

            var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(checkObject);
            var missingFromUnity = GetMissingType(property.managedReferenceId);
            if (missingFromUnity != null)
            {
                missingType = missingFromUnity.Value;
                return true;
            }

            if (string.IsNullOrEmpty(assetPath) == false)
            {
                if (PropertyDrawerGlobalCaches.targetObjectAndMissingPaths.TryGetValue(checkObject, out var missingPropertyPaths) == false)
                {
                    missingPropertyPaths = MissingTypeUtils.GetMissingPropertyPaths(property, assetPath);
                    PropertyDrawerGlobalCaches.targetObjectAndMissingPaths[checkObject] = missingPropertyPaths;
                }
                
                var missingIdFromYaml =
                    missingPropertyPaths.FirstOrDefault(t => t.propertyPath == property.propertyPath).refId;
                var missingTypeFromYaml = GetMissingType(missingIdFromYaml);
                if (missingTypeFromYaml != null)
                {
                    missingType = missingTypeFromYaml.Value;
                    return true;
                }
            }

            return false;

            ManagedReferenceMissingType? GetMissingType(long id)
            {
                foreach (var missingType in missingTypes)
                {
                    if (missingType.referenceId == id)
                    {
                        return missingType;
                    }
                }

                return null;
            }
        }
    }
}