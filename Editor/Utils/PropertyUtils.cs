using System;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class PropertyUtils
    {
        //TODO Need make better - some bugs with iteration here. Need to fix later - maybe...
        public static bool TraverseProperty(SerializedProperty property, string path,
            Func<SerializedProperty, bool> isCompleteFunc)
        {
            var currentProperty = property.Copy();
            int startDepth = currentProperty.depth;

            do
            {
                string propertyPath =
                    string.IsNullOrEmpty(path) ? currentProperty.name : $"{path}.{currentProperty.name}";

                if (currentProperty.propertyType == SerializedPropertyType.ManagedReference)
                {
                    if (isCompleteFunc.Invoke(currentProperty))
                    {
                        return true;
                    }
                }

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

                if (currentProperty.hasVisibleChildren &&
                    currentProperty.propertyType == SerializedPropertyType.Generic)
                {
                    var childProperty = currentProperty.Copy();
                    if (childProperty.Next(true))
                    {
                        do
                        {
                            if (TraverseProperty(childProperty, propertyPath, isCompleteFunc))
                            {
                                return true;
                            }
                        } while (childProperty.Next(false) && childProperty.depth > startDepth);
                    }
                }
            } while (currentProperty.Next(false) && currentProperty.depth >= startDepth);

            return false;
        }
    }
}