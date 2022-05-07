using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SRD.Editor.AnyType
{
    public class AnyTypeDrawerUnityObject
    {
        private Dictionary<Type, string> SearchFilterCache = new Dictionary<Type, string>();
        
        private readonly SerializedProperty _unityObjectProperty;
        private readonly Type _targetType;
        private bool _needCheckObjectSelector;

        public AnyTypeDrawerUnityObject(SerializedProperty unityObjectProperty, SerializedProperty nativeTypeProperty)
        {
            _unityObjectProperty = unityObjectProperty;
            _targetType = GetPropertyType(nativeTypeProperty);
            if (SearchFilterCache.ContainsKey(_targetType) == false)
            {
                SearchFilterCache.Add(_targetType,GetSearchFilter());
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
                EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(null, true, SearchFilterCache[_targetType], 0);
                _needCheckObjectSelector = true;
            }

            if (IsUpdatedFromObjectSelector())
            {
                return;
            }

            var isNullUnityRef = _unityObjectProperty.objectReferenceValue == null;
            var newObject = EditorGUI.ObjectField(mainRect, label, _unityObjectProperty.objectReferenceValue,
                typeof(UnityEngine.Object), true);
            FillUnityObjectToProperty(newObject);

            if (isNullUnityRef == false && newObject == null)
            {
                _unityObjectProperty.objectReferenceValue = null;
                _unityObjectProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private bool IsUpdatedFromObjectSelector()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && _needCheckObjectSelector)
            {
                _needCheckObjectSelector = false;
                var pickedObject = EditorGUIUtility.GetObjectPickerObject();
                FillUnityObjectToProperty(pickedObject);
                return true;
            }

            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                _needCheckObjectSelector = false;
                return true;
            }

            return false;
        }

        private string GetSearchFilter()
        {
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            var types = ReflectionUtils.GetFinalAssignableTypes(_targetType, allTypes,
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
            return ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(property);
        }

        private void FillUnityObjectToProperty(UnityEngine.Object pickedObject)
        {
            UnityEngine.Object targetObject = null;
            if (_targetType.IsInstanceOfType(pickedObject))
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

            if (targetObject != null && _targetType.IsInstanceOfType(targetObject))
            {
                _unityObjectProperty.objectReferenceValue = targetObject;
                _unityObjectProperty.serializedObject.ApplyModifiedProperties();
            }

            UnityEngine.Object GetComponentFromGameObject(UnityEngine.Object someObject)
            {
                if (someObject != null && someObject is GameObject go)
                {
                    var component = go.GetComponent(_targetType);
                    return component;
                }

                return null;
            }
        }
    }
}