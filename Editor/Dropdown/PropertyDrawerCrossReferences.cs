using System;
using System.Collections.Generic;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    public static class PropertyDrawerCrossReferences
    {
        //TODO Make better unique colors for equal references
        public static Color GetColorForEqualSerializeReference(SerializedProperty property)
        {
            var refId = ManagedReferenceUtility.GetManagedReferenceIdForObject(property.serializedObject.targetObject,
                property.managedReferenceValue);
            var refsArray = ManagedReferenceUtility.GetManagedReferenceIds(property.serializedObject.targetObject);
            var index = Array.FindIndex(refsArray, t => t == refId);
            return GetColorForEqualSerializeReference(index, refsArray.Length);
        }

        private static Color GetColorForEqualSerializeReference(int srIndex, int srCount)
        {
            var hue = srCount == 0 ? 0 : (float)srIndex / srCount;
            return Color.HSVToRGB(hue, 0.8f, 0.8f);
        }

        public static bool IsHaveSameOtherSerializeReference(SerializedProperty property, out bool isNewElement)
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
            if (PropertyDrawerGlobalCaches.targetObjectAndSerializeReferencePaths.TryGetValue(target,
                    out var serializeReferencePaths) == false)
            {
                isNewElement = true;
                serializeReferencePaths = new HashSet<string>();
                PropertyDrawerGlobalCaches.targetObjectAndSerializeReferencePaths.Add(target, serializeReferencePaths);
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

        public static void FixCrossReference(SerializedProperty property)
        {
            SOUtils.RegisterUndo(property, "Fix cross references");

            var json = JsonUtility.ToJson(property.managedReferenceValue);
            PropertyDrawerTypesUtils.CreateAndApplyNewInstanceFromType(property.managedReferenceValue.GetType(),
                property, registerUndo: false);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            PropertyDrawerGlobalCaches.DropCaches();
        }
    }
}