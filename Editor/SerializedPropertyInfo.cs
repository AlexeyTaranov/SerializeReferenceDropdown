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

        public readonly string[] AssignableTypeNames;

        private readonly Type[] _assignableTypes;
        private readonly FieldInfo _fieldInfo;

        public SerializedPropertyInfo(SerializedProperty property)
        {
            var serializedObjectType = property.serializedObject.targetObject.GetType();
            var propertyPath = property.propertyPath;
            if (IsArrayProperty(property))
            {
                var startIndexArrayPropertyPath = property.propertyPath.IndexOf(ArrayPropertySubstring);
                propertyPath = propertyPath.Remove(startIndexArrayPropertyPath);
            }

            _fieldInfo = GetFieldFromHierarchyToBaseType(serializedObjectType);
            var fieldType = ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(property);
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            _assignableTypes = ReflectionUtils.GetFinalAssignableTypes(fieldType, allTypes);
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

            FieldInfo GetFieldFromHierarchyToBaseType(Type type)
            {
                var field = type.GetField(propertyPath,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null)
                {
                    var baseType = type.BaseType;
                    if (baseType != null)
                    {
                        return GetFieldFromHierarchyToBaseType(baseType);
                    }
                }

                return field;
            }
        }

        public bool CanShowSRD() => _assignableTypes.Any() && _fieldInfo != null;

        public int GetIndexAssignedTypeOfProperty(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var objectValue = _fieldInfo.GetValue(targetObject);
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
                var arrayFieldObjects = _fieldInfo.GetValue(targetObject);
                int index = GetIndexArrayElementProperty(property);
                var objectsArray = (object[])arrayFieldObjects;
                objectsArray[index] = value;
            }
            else
            {
                _fieldInfo.SetValue(targetObject, value);
            }
        }

        bool IsArrayProperty(SerializedProperty property) => property.propertyPath.Contains(ArrayPropertySubstring);

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