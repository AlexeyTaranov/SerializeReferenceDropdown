using System;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor
{
    public static class PropertyUtils
    {
        public static bool TraverseProperty(SerializedProperty inProperty, string path,
            Func<SerializedProperty, bool> isCompleteFunc)
        {
            using var currentProperty = inProperty.Copy();

            while (true)
            {
                var propertyPath = string.IsNullOrEmpty(path) ? currentProperty.name : $"{path}.{currentProperty.name}";

                if (currentProperty.isArray && currentProperty.propertyType != SerializedPropertyType.String)
                {
                    for (int i = 0; i < currentProperty.arraySize; i++)
                    {
                        var element = currentProperty.GetArrayElementAtIndex(i);
                        if (TraverseProperty(element, $"{propertyPath}[{i}]", isCompleteFunc))
                        {
                            return true;
                        }
                    }
                }
                else if (currentProperty.hasVisibleChildren &&
                         currentProperty.propertyType == SerializedPropertyType.Generic)
                {
                    if (currentProperty.NextVisible(true))
                    {
                        if (TraverseProperty(currentProperty, propertyPath, isCompleteFunc))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (currentProperty.propertyType == SerializedPropertyType.ManagedReference)
                    {
                        if (isCompleteFunc.Invoke(currentProperty))
                        {
                            return true;
                        }
                    }
                }

                if (currentProperty.NextVisible(false) == false)
                {
                    break;
                }
            }

            return false;
        }
    }
}