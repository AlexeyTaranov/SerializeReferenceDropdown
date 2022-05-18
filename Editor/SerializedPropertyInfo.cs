using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor
{
    public class SerializedPropertyInfo
    {
        private const string ArrayPropertySubstring = ".Array.data[";
        private static readonly Dictionary<Type, Type[]> AssignableTypesCache = new Dictionary<Type, Type[]>();

        private readonly Type propertyType;
        private readonly List<FieldInfo> fieldHierarchyToTarget = new List<FieldInfo>();

        public Type[] AssignableTypes => AssignableTypesCache[propertyType];

        public SerializedPropertyInfo(SerializedProperty property)
        {
            var serializedObjectType = property.serializedObject.targetObject.GetType();
            var propertyPath = property.propertyPath;
            if (IsArrayProperty(property))
            {
                var startIndexArrayPropertyPath = property.propertyPath.IndexOf(ArrayPropertySubstring);
                propertyPath = property.propertyPath.Remove(startIndexArrayPropertyPath);
            }

            GetFieldFromPathPropertyHierarchy(serializedObjectType, propertyPath.Split('.'));
            fieldHierarchyToTarget.Reverse();
            propertyType = ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(property);
            if (AssignableTypesCache.ContainsKey(propertyType))
            {
                return;
            }

            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            var assignableTypes = ReflectionUtils.GetFinalAssignableTypes(propertyType, allTypes,
                predicate: type => type.IsSubclassOf(typeof(UnityEngine.Object)) == false).ToArray();
            var assignableTypesCache = new Type[assignableTypes.Length + 1];
            assignableTypesCache[0] = null;
            for (int i = 1; i < assignableTypesCache.Length; i++)
            {
                assignableTypesCache[i] = assignableTypes[i - 1];
            }

            AssignableTypesCache.Add(propertyType, assignableTypesCache);

            FieldInfo GetFieldFromPathPropertyHierarchy(Type type, string[] splitPath, int index = 0)
            {
                var fieldPath = splitPath[index];
                if (IsArrayProperty(property))
                {
                    var startIndexArrayPropertyPath = property.propertyPath.IndexOf(ArrayPropertySubstring);
                    fieldPath = property.propertyPath.Remove(startIndexArrayPropertyPath);
                }

                if (index == splitPath.Length - 1)
                {
                    var field = GetFieldFromHierarchyToBaseType(type, fieldPath);
                    fieldHierarchyToTarget.Add(field);
                    return field;
                }
                else
                {
                    var field = GetFieldFromHierarchyToBaseType(type, fieldPath);
                    var baseField = GetFieldFromPathPropertyHierarchy(field.FieldType, splitPath, index + 1);
                    fieldHierarchyToTarget.Add(field);
                    return baseField;
                }
            }

            FieldInfo GetFieldFromHierarchyToBaseType(Type type, string path)
            {
                var field = type.GetField(path,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null)
                {
                    var baseType = type.BaseType;
                    if (baseType != null)
                    {
                        return GetFieldFromHierarchyToBaseType(baseType, path);
                    }
                }

                return field;
            }
        }

        public bool CanShowDropdown() => AssignableTypes.Any() && fieldHierarchyToTarget.Any();

        public int GetIndexAssignedTypeOfProperty(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var objectValue = GetObjectValueFromHierarchy(targetObject);
            if (IsArrayProperty(property))
            {
                int index = GetIndexArrayElementProperty(property);
                var objectsArray = (object[])objectValue;
                objectValue = objectsArray[index];
            }

            if (objectValue is null) return 0;

            var type = objectValue.GetType();
            var cacheTypes = AssignableTypesCache[propertyType];
            for (int i = 0; i < cacheTypes.Length; i++)
            {
                if (cacheTypes[i] == type)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ApplyValueToProperty(object value, SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            if (IsArrayProperty(property))
            {
                var arrayFieldObjects = GetObjectValueFromHierarchy(targetObject);
                int index = GetIndexArrayElementProperty(property);
                var objectsArray = (object[])arrayFieldObjects;
                objectsArray[index] = value;
            }
            else
            {
                property.managedReferenceValue = value;
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            }
        }

        private object GetObjectValueFromHierarchy(object objectValue)
        {
            foreach (var field in fieldHierarchyToTarget)
            {
                if (objectValue != null)
                {
                    objectValue = field.GetValue(objectValue);
                }
            }

            return objectValue;
        }

        private bool IsArrayProperty(SerializedProperty property) =>
            property.propertyPath.Contains(ArrayPropertySubstring);

        private int GetIndexArrayElementProperty(SerializedProperty property)
        {
            var index = 0;
            if (IsArrayProperty(property))
            {
                var propertyPath = property.propertyPath;
                string arrayElementIndex =
                    propertyPath.Substring(property.propertyPath.IndexOf("[") + 1).Replace("]", "");
                index = Convert.ToInt32(arrayElementIndex);
            }

            return index;
        }
    }
}
