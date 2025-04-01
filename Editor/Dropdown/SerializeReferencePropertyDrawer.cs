using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.SearchTool;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public partial class SerializeReferencePropertyDrawer : PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;
        private Rect propertyRect;

        #region Dropdown

        private string GetTypeName(Type type)
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

            return ObjectNames.NicifyVariableName(type.Name);
        }

        private string GetTypeTooltip(Type type)
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

        private List<Type> GetAssignableTypes(SerializedProperty property)
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

        private void WriteNewInstanceByIndexType(int typeIndex, SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);

            if (newType?.IsGenericType == true)
            {
                var concreteGenericType = TypeUtils.GetConcreteGenericType(propertyType, newType);
                if (concreteGenericType != null)
                {
                    CreateAndApplyNewInstanceFromType(concreteGenericType, property);
                }
                else
                {
                    GenericTypeCreateWindow.ShowCreateTypeMenu(property, propertyRect, newType,
                        (type) => CreateAndApplyNewInstanceFromType(type, property));
                }
            }
            else
            {
                CreateAndApplyNewInstanceFromType(newType, property);
            }
        }

        private void CreateAndApplyNewInstanceFromType(Type type, SerializedProperty property)
        {
            var newObject = TypeUtils.CreateObjectFromType(type);
            ApplyValueToProperty(newObject);

            void ApplyValueToProperty(object value)
            {
                var needSaveData = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().CopyDataWithNewType;
                var targets = property.serializedObject.targetObjects;

                // Multiple object edit.
                //One Serialized Object for multiple Objects work sometimes incorrectly  
                foreach (var target in targets)
                {
                    using var so = new SerializedObject(target);
                    var targetProperty = so.FindProperty(property.propertyPath);
                    var previousJsonData = string.Empty;
                    if (needSaveData && targetProperty.managedReferenceValue != null)
                    {
                        previousJsonData = JsonUtility.ToJson(targetProperty.managedReferenceValue);
                    }

                    if (needSaveData)
                    {
                        JsonUtility.FromJsonOverwrite(previousJsonData, value);
                    }

                    targetProperty.managedReferenceValue = value;


                    so.ApplyModifiedProperties();
                    so.Update();
                }
            }
        }

        #endregion


        #region CrossReferences

        //TODO: need to find better solution
        private static readonly Dictionary<Object, HashSet<string>> targetObjectAndSerializeReferencePaths =
            new Dictionary<Object, HashSet<string>>();

        //TODO Make better unique colors for equal references
        private static Color GetColorForEqualSerializeReference(SerializedProperty property)
        {
            var refId = ManagedReferenceUtility.GetManagedReferenceIdForObject(property.serializedObject.targetObject,
                property.managedReferenceValue);
            var refsArray = ManagedReferenceUtility.GetManagedReferenceIds(property.serializedObject.targetObject);
            var index = Array.FindIndex(refsArray, t => t == refId);
            return GetColorForEqualSerializeReference(index, refsArray.Length);
        }

        public static Color GetColorForEqualSerializeReference(int srIndex, int srCount)
        {
            var hue = srCount == 0 ? 0 : (float)srIndex / srCount;
            return Color.HSVToRGB(hue, 0.8f, 0.8f);
        }

        private bool IsHaveSameOtherSerializeReference(SerializedProperty property)
        {
            if (SerializeReferenceToolsUserPreferences.GetOrLoadSettings().DisableCrossReferencesCheck)
            {
                return false;
            }

            if (property.managedReferenceValue == null)
            {
                return false;
            }

            var target = property.serializedObject.targetObject;
            if (targetObjectAndSerializeReferencePaths.TryGetValue(target, out var serializeReferencePaths) == false)
            {
                serializeReferencePaths = new HashSet<string>();
                targetObjectAndSerializeReferencePaths.Add(target, serializeReferencePaths);
            }

            // Can't find this path in serialized object. Example - new element in array
            if (serializeReferencePaths.Contains(property.propertyPath) == false)
            {
                var paths = FindAllSerializeReferencePathsInTargetObject();
                serializeReferencePaths.Clear();
                foreach (var path in paths)
                {
                    serializeReferencePaths.Add(path);
                }
            }

            foreach (var referencePath in serializeReferencePaths)
            {
                if (property.propertyPath == referencePath)
                {
                    continue;
                }

                using var otherProperty = property.serializedObject.FindProperty(referencePath);
                if (otherProperty != null)
                {
                    if (otherProperty.managedReferenceId == property.managedReferenceId)
                    {
                        return true;
                    }
                }
                else
                {
                    //TODO null property???
                }
            }

            return false;

            HashSet<string> FindAllSerializeReferencePathsInTargetObject()
            {
                using var iterator = property.serializedObject.GetIterator();
                iterator.NextVisible(true);
                var paths = new HashSet<string>();
                PropertyUtils.TraverseProperty(iterator, string.Empty, FillAllPaths);
                return paths;

                bool FillAllPaths(SerializedProperty serializeReferenceProperty)
                {
                    paths.Add(serializeReferenceProperty.propertyPath);
                    return false;
                }
            }
        }

        private void FixCrossReference(SerializedProperty property)
        {
            var json = JsonUtility.ToJson(property.managedReferenceValue);
            CreateAndApplyNewInstanceFromType(property.managedReferenceValue.GetType(), property);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        #endregion


        #region OpenSource

        private void OpenSourceFile(Type type)
        {
            var (filePath, lineNumber, columnNumber) = CodeAnalysis.GetSourceFileLocation(type);

            if (string.IsNullOrEmpty(filePath) == false)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(filePath);
                AssetDatabase.OpenAsset(asset, lineNumber, columnNumber);
            }
        }

        #endregion


        #region SearchTool

        private void ShowSearchTool(Type type)
        {
            SearchToolWindow.ShowSearchTypeWindow(type);
        }

        #endregion
    }
}