using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SRD.Editor
{
    public class SerializedPropertyInfo
    {
        public readonly string[] AssignableTypeNames;
        public readonly Type[] AssignableTypes;
        private readonly FieldInfo _fieldInfo;

        public static bool IsArrayProperty(SerializedProperty property) =>
            property.propertyPath.Contains(arrayPropertySubstring);

        private static string arrayPropertySubstring = ".Array.data[";

        public SerializedPropertyInfo(SerializedProperty property)
        {
            var serializedObjectType = property.serializedObject.targetObject.GetType();
            var propertyPath = property.propertyPath;
            if (IsArrayProperty(property))
            {
                var startIndexArrayPropertyPath = property.propertyPath.IndexOf(arrayPropertySubstring);
                propertyPath = propertyPath.Remove(startIndexArrayPropertyPath);
            }

            _fieldInfo = serializedObjectType.GetField(propertyPath,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var fieldType = ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(property);
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            AssignableTypes = ReflectionUtils.GetFinalAssignableTypes(fieldType, allTypes);
            AssignableTypeNames = AssignableTypes.Select(type => type.Name).ToArray();
            if (!fieldType.IsValueType)
            {
                var typesWithNull = new List<Type> {null};
                typesWithNull.AddRange(AssignableTypes);
                AssignableTypes = typesWithNull.ToArray();
                var typeNamesWithNull = new List<string> {"null"};
                typeNamesWithNull.AddRange(AssignableTypeNames);
                AssignableTypeNames = typeNamesWithNull.ToArray();
            }
        }

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

        public int GetIndexAssignedTypeOfProperty(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var objectValue = _fieldInfo.GetValue(targetObject);
            if (IsArrayProperty(property))
            {
                int index = GetIndexArrayElementProperty(property);
                var objectsArray = (object[]) objectValue;
                objectValue = objectsArray[index];
            }

            if (objectValue is null) return 0;

            return Array.IndexOf(AssignableTypes, objectValue.GetType());
        }

        public void ApplyValueToProperty(object value, SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            if (IsArrayProperty(property))
            {
                var arrayFieldObjects = _fieldInfo.GetValue(targetObject);
                int index = GetIndexArrayElementProperty(property);
                var objectsArray = (object[]) arrayFieldObjects;
                objectsArray[index] = value;
            }
            else
            {
                _fieldInfo.SetValue(targetObject, value);
            }
        }
    }
}