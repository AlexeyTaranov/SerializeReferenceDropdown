using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.SearchTool.SearchToolWindow;
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

        private static Object pingObject;
        private static Object previousSelection;
        private static long pingRefId;

        private static Dictionary<SerializeReferencePropertyDrawer, SerializedProperty> _allPropertyDrawers =
            new Dictionary<SerializeReferencePropertyDrawer, SerializedProperty>();

        private Action _pingSelf;

        private static void DropCaches()
        {
            targetObjectAndSerializeReferencePaths.Clear();
            targetObjectAndMissingPaths.Clear();
        }

        #region Dropdown

        public static void PingSerializeReference(Object selectionObject, long refId)
        {
            previousSelection = Selection.activeObject;
            Selection.activeObject = selectionObject;
            pingObject = selectionObject;
            pingRefId = refId;
            PingAll();

            EditorApplication.delayCall += () =>
            {
                previousSelection = null;
                pingObject = null;
                pingRefId = -1;
            };
        }

        private static void PingAll()
        {
            foreach (var pair in _allPropertyDrawers)
            {
                if (pair.Value != null)
                {
                    pair.Key.PingSelf();
                }
            }
        }

        private void PingSelf()
        {
            _pingSelf.Invoke();
        }

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

        private static void CreateAndApplyNewInstanceFromType(Type type, SerializedProperty property, bool registerUndo)
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

        #endregion


        #region Missing Types

        //TODO: need to find better solution
        private static readonly Dictionary<Object, IReadOnlyList<(string propertyPath, long refId)>>
            targetObjectAndMissingPaths =
                new Dictionary<Object, IReadOnlyList<(string propertyPath, long refId)>>();

        private bool TryGetMissingType(SerializedProperty property, string assetPath,
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

            if (targetObjectAndMissingPaths.TryGetValue(checkObject, out var missingPropertyPaths) == false)
            {
                missingPropertyPaths = MissingTypeUtils.GetMissingPropertyPaths(property, assetPath);
                targetObjectAndMissingPaths[checkObject] = missingPropertyPaths;
            }

            var missingIdFromYaml =
                missingPropertyPaths.FirstOrDefault(t => t.propertyPath == property.propertyPath).refId;
            var missingTypeFromYaml = GetMissingType(missingIdFromYaml);
            if (missingTypeFromYaml != null)
            {
                missingType = missingTypeFromYaml.Value;
                return true;
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

        private bool IsHaveSameOtherSerializeReference(SerializedProperty property, out bool isNewElement)
        {
            isNewElement = false;
            if (SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableCrossReferencesCheck == false)
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
                isNewElement = true;
                serializeReferencePaths = new HashSet<string>();
                targetObjectAndSerializeReferencePaths.Add(target, serializeReferencePaths);
            }

            // Can't find this path in serialized object. Example - new element in array
            if (serializeReferencePaths.Contains(property.propertyPath) == false)
            {
                isNewElement = true;
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
            }

            return false;

            HashSet<string> FindAllSerializeReferencePathsInTargetObject()
            {
                var paths = new HashSet<string>();
                SOUtils.TraverseSO(property.serializedObject.targetObject, FillAllPaths);
                return paths;

                bool FillAllPaths(SerializedProperty serializeReferenceProperty)
                {
                    paths.Add(serializeReferenceProperty.propertyPath);
                    return false;
                }
            }
        }

        private static void FixCrossReference(SerializedProperty property)
        {
            SOUtils.RegisterUndo(property, "Fix cross references");

            var json = JsonUtility.ToJson(property.managedReferenceValue);
            CreateAndApplyNewInstanceFromType(property.managedReferenceValue.GetType(), property, registerUndo: false);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            DropCaches();
        }

        #endregion


        #region OpenSource

        public static void OpenSourceFile(Type type)
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