using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SRD.Editor
{
    public class SerializedPropertyInfo
    {
        private const string ArrayPropertySubstring = ".Array.data[";

        private readonly Type[] _assignableTypes;
        private readonly List<FieldInfo> _fieldHierarchyToTarget = new List<FieldInfo>();

        public readonly string[] AssignableTypeNames;

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
            _fieldHierarchyToTarget.Reverse();
            var fieldType = ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(property);
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            _assignableTypes = ReflectionUtils.GetFinalAssignableTypes(fieldType, allTypes)
                .Where(type => type.IsSubclassOf(typeof(UnityEngine.Object)) == false).ToArray();
            AssignableTypeNames = _assignableTypes.Select(type => type.Name).ToArray();
            if (!fieldType.IsValueType)
            {
                var typesWithNull = new List<Type> { null };
                typesWithNull.AddRange(_assignableTypes);
                _assignableTypes = typesWithNull.ToArray();
                var typeNamesWithNull = new List<string> { "null" };
                typeNamesWithNull.AddRange(AssignableTypeNames);
                AssignableTypeNames = typeNamesWithNull.ToArray();
            }

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
                    _fieldHierarchyToTarget.Add(field);
                    return field;
                }
                else
                {
                    var field = GetFieldFromHierarchyToBaseType(type, fieldPath);
                    var baseField = GetFieldFromPathPropertyHierarchy(field.FieldType, splitPath, index + 1);
                    _fieldHierarchyToTarget.Add(field);
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

        public bool CanShowSRD() => _assignableTypes.Any() && _fieldHierarchyToTarget.Any();

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

            return Array.IndexOf(_assignableTypes, objectValue.GetType());
        }

        public Type GetTypeAtIndex(int index) => _assignableTypes[index];

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
            foreach (var field in _fieldHierarchyToTarget)
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