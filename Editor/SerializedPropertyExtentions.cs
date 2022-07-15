using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor
{
    public static class SerializedPropertyExtentions
    {
        private static Regex ArrayIndexCapturePattern = new Regex(@"\[(\d*)\]");

        public static object GetTarget(this SerializedProperty prop)
        {
            string[] propertyNames = prop.propertyPath.Split('.');
            object target = prop.serializedObject.targetObject;
            bool isNextPropertyArrayIndex = false;
            for (int i = 0; i < propertyNames.Length && target != null; ++i)
            {
                string propName = propertyNames[i];
                if (propName == "Array")
                {
                    isNextPropertyArrayIndex = true;
                }
                else if (isNextPropertyArrayIndex)
                {
                    isNextPropertyArrayIndex = false;
                    int arrayIndex = ParseArrayIndex(propName);
                    object[] targetAsArray = (object[])target;
                    target = targetAsArray[arrayIndex];
                }
                else
                {
                    target = GetField(target, propName);
                }
            }

            return target;
        }

        private static object GetField(object target, string name, Type targetType = null)
        {
            if (targetType == null)
            {
                targetType = target.GetType();
            }

            FieldInfo fi = targetType.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
            {
                return fi.GetValue(target);
            }

            // If not found, search in parent
            if (targetType.BaseType != null)
            {
                return GetField(target, name, targetType.BaseType);
            }

            return null;
        }

        private static int ParseArrayIndex(string propName)
        {
            Match match = ArrayIndexCapturePattern.Match(propName);
            if (!match.Success)
            {
                throw new Exception($"Invalid array index parsing in {propName}");
            }

            return int.Parse(match.Groups[1].Value);
        }
    }
}
