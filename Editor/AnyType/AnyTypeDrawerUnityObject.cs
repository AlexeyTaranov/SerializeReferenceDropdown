using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.AnyType
{
    public class AnyTypeDrawerUnityObject
    {
        private readonly Dictionary<Type, string> searchFilterCache = new Dictionary<Type, string>();

        private readonly SerializedProperty unityObjectProperty;
        private readonly Type targetType;
        private bool needCheckObjectSelector;

        public AnyTypeDrawerUnityObject(SerializedProperty unityObjectProperty, SerializedProperty nativeTypeProperty)
        {
            this.unityObjectProperty = unityObjectProperty;
            targetType = GetPropertyType(nativeTypeProperty);
            if (searchFilterCache.ContainsKey(targetType) == false)
            {
                searchFilterCache.Add(targetType, GetSearchFilter());
            }
        }

        public void DrawUnityReferenceType(GUIContent label, Rect mainRect, Rect leftButtonRect)
        {
            var searchButton = new Rect(leftButtonRect);
            searchButton.x += leftButtonRect.width + 5;
            searchButton.width = 35;
            mainRect.x += 25;
            mainRect.width -= 25;
            if (GUI.Button(searchButton, "Pick"))
            {
                EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(null, true, searchFilterCache[targetType], 0);
                needCheckObjectSelector = true;
            }

            if (IsUpdatedFromObjectSelector())
            {
                return;
            }

            var isNullUnityRef = unityObjectProperty.objectReferenceValue == null;
            var newObject = EditorGUI.ObjectField(mainRect, label, unityObjectProperty.objectReferenceValue,
                typeof(UnityEngine.Object), true);
            FillUnityObjectToProperty(newObject);

            if (isNullUnityRef == false && newObject == null)
            {
                unityObjectProperty.objectReferenceValue = null;
                unityObjectProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private bool IsUpdatedFromObjectSelector()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && needCheckObjectSelector)
            {
                needCheckObjectSelector = false;
                var pickedObject = EditorGUIUtility.GetObjectPickerObject();
                FillUnityObjectToProperty(pickedObject);
                return true;
            }

            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                needCheckObjectSelector = false;
                return true;
            }

            return false;
        }

        private string GetSearchFilter()
        {
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            var types = ReflectionUtils.GetFinalAssignableTypes(targetType, allTypes,
                predicate: type => type.IsSubclassOf(typeof(UnityEngine.Object)));

            var sb = new StringBuilder();
            foreach (var type in types)
            {
                sb.Append($"t: {type.Name} ");
            }

            return sb.ToString();
        }

        private Type GetPropertyType(SerializedProperty property)
        {
            return ReflectionUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
        }

        private void FillUnityObjectToProperty(UnityEngine.Object pickedObject)
        {
            UnityEngine.Object targetObject = null;
            if (targetType.IsInstanceOfType(pickedObject))
            {
                targetObject = pickedObject;
            }
            else
            {
                var component = GetComponentFromGameObject(pickedObject);
                if (component != null)
                {
                    targetObject = component;
                }
            }

            if (targetObject != null && targetType.IsInstanceOfType(targetObject))
            {
                unityObjectProperty.objectReferenceValue = targetObject;
                unityObjectProperty.serializedObject.ApplyModifiedProperties();
            }

            UnityEngine.Object GetComponentFromGameObject(UnityEngine.Object someObject)
            {
                if (someObject != null && someObject is GameObject go)
                {
                    var component = go.GetComponent(targetType);
                    return component;
                }

                return null;
            }
        }
    }
}