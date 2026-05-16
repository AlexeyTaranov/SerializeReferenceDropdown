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
        private static readonly Dictionary<Type, string> BuiltInTypeNames = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(object), "object" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(string), "string" },
        };

        public static string GetTypeName(Type type)
        {
            if (type == null)
            {
                return NullName;
            }

            if (BuiltInTypeNames.TryGetValue(type, out var builtInTypeName))
            {
                return builtInTypeName;
            }

            var customTypeName = GetCustomTypeName(type);
            if (customTypeName != null)
            {
                return type.IsGenericType
                    ? customTypeName + GetGenericArgumentsName(type)
                    : customTypeName;
            }

            if (type.IsGenericType)
            {
                var genericName = ObjectNames.NicifyVariableName(GetGenericTypeNameWithoutArity(type)) +
                                  GetGenericArgumentsName(type);
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

        private static string GetCustomTypeName(Type type)
        {
            var typesWithNames = TypeCache.GetTypesWithAttribute(typeof(SerializeReferenceDropdownNameAttribute));
            if (typesWithNames.Contains(type))
            {
                var dropdownNameAttribute = type.GetCustomAttribute<SerializeReferenceDropdownNameAttribute>();
                return dropdownNameAttribute.Name;
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (typesWithNames.Contains(genericTypeDefinition))
                {
                    var dropdownNameAttribute =
                        genericTypeDefinition.GetCustomAttribute<SerializeReferenceDropdownNameAttribute>();
                    return dropdownNameAttribute.Name;
                }
            }

            return null;
        }

        private static string GetGenericArgumentsName(Type type)
        {
            var genericNames = type.GetGenericArguments().Select(GetTypeName);
            return "<" + string.Join(", ", genericNames) + ">";
        }

        private static string GetGenericTypeNameWithoutArity(Type type)
        {
            var name = type.Name;
            var arityIndex = name.IndexOf('`');
            return arityIndex < 0 ? name : name.Substring(0, arityIndex);
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
            return TypeUtils.GetAssignableSerializeReferenceTypes(property);
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
            if (type == null)
            {
                return;
            }

            var (filePath, lineNumber, columnNumber) = CodeAnalysis.GetSourceFileLocation(type);

            if (string.IsNullOrEmpty(filePath) == false)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(filePath);
                AssetDatabase.OpenAsset(asset, lineNumber, columnNumber);
            }
        }

        public static void ShowOpenSourceFileMenu(SerializedProperty property, Rect buttonRect)
        {
            var menu = new GenericMenu();
            var typeItems = GetOpenSourceFileTypeItems(property);
            foreach (var typeItem in typeItems)
            {
                menu.AddItem(new GUIContent(typeItem.menuPath), false, () => OpenSourceFile(typeItem.type));
            }

            if (typeItems.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No type"));
            }

            menu.DropDown(buttonRect);
        }

        private static List<(string menuPath, Type type)> GetOpenSourceFileTypeItems(SerializedProperty property)
        {
            var typeItems = new List<(string menuPath, Type type)>();
            var addedTypes = new HashSet<Type>();
            var fieldType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var valueType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            AddTypeItem("Field", fieldType);
            AddTypeItem("Value", valueType);

            return typeItems;

            void AddTypeItem(string labelPrefix, Type type)
            {
                if (type == null || addedTypes.Add(type) == false)
                {
                    return;
                }

                typeItems.Add(($"{labelPrefix}: {GetTypeName(type)}", type));

                if (type.IsGenericType == false)
                {
                    return;
                }

                var genericArguments = type.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    AddTypeItem($"{labelPrefix} Arg {i}", genericArguments[i]);
                }
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
