using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(AnyType<>))]
    public class AnyTypeDrawer : PropertyDrawer
    {
        private string _searchFilter;
        private bool _needCheckSelectedObject;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var refTypeProperty = property.FindPropertyRelative("_isUnityObjectReference");
            var isUnityObj = refTypeProperty.boolValue;

            var leftButtonRect = DrawLeftReferenceTypeButton();

            rect.width -= 40;
            rect.x += 40;
            if (isUnityObj)
            {
                DrawUnityReferenceType(property, label, rect, leftButtonRect);
            }
            else
            {
                var refProperty = property.FindPropertyRelative(GetPropertyName(false));
                EditorGUI.PropertyField(rect, refProperty, label);
            }

            EditorGUI.EndProperty();

            Rect DrawLeftReferenceTypeButton()
            {
                var refTypeButton = isUnityObj ? "U" : "#";
                var buttonRect = new Rect(rect);
                buttonRect.width = 20;
                buttonRect.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(buttonRect, refTypeButton))
                {
                    isUnityObj = !isUnityObj;
                    refTypeProperty.boolValue = isUnityObj;
                    refTypeProperty.serializedObject.ApplyModifiedProperties();
                }

                return buttonRect;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var isUnityObject = property.FindPropertyRelative("_isUnityObjectReference").boolValue;
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(GetPropertyName(isUnityObject)), label,
                true);
        }

        private string GetPropertyName(bool isUnityObject) => isUnityObject ? "_unityObject" : "_nativeObject";

        private void DrawUnityReferenceType(SerializedProperty property, GUIContent label, Rect mainRect,
            Rect leftButtonRect)
        {
            var refProperty = property.FindPropertyRelative(GetPropertyName(true));
            var searchButton = new Rect(leftButtonRect);
            searchButton.x += leftButtonRect.width + 5;
            searchButton.width = 35;
            mainRect.x += 25;
            mainRect.width -= 25;
            if (GUI.Button(searchButton, "Pick"))
            {
                _searchFilter ??= GetSearchFilter();
                EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(null, true, _searchFilter, 0);
                _needCheckSelectedObject = true;
            }

            var targetType = GetTargetAbstractType();

            if (Event.current.commandName == "ObjectSelectorUpdated" && _needCheckSelectedObject)
            {
                _needCheckSelectedObject = false;
                var pickedObject = EditorGUIUtility.GetObjectPickerObject();
                if (pickedObject != null && pickedObject is GameObject go)
                {
                    var component = go.GetComponent(targetType);
                    if (component != null)
                    {
                        refProperty.objectReferenceValue = component;
                        refProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                _needCheckSelectedObject = false;
            }

            var isNullUnityRef = refProperty.objectReferenceValue == null;
            var newObject = EditorGUI.ObjectField(mainRect, label, refProperty.objectReferenceValue,
                typeof(UnityEngine.Object), true);
            if (newObject != null)
            {
                if (targetType.IsInstanceOfType(newObject))
                {
                    refProperty.objectReferenceValue = newObject;
                    refProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            if (isNullUnityRef == false && newObject == null)
            {
                refProperty.objectReferenceValue = null;
                refProperty.serializedObject.ApplyModifiedProperties();
            }

            string GetSearchFilter()
            {
                var fieldType = GetTargetAbstractType();
                var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
                var types = ReflectionUtils.GetFinalAssignableTypes(fieldType, allTypes)
                    .Where(type => type.IsSubclassOf(typeof(UnityEngine.Object))).ToArray();

                var sb = new StringBuilder();
                foreach (var type in types)
                {
                    sb.Append($"t: {type.Name} ");
                }

                return sb.ToString();
            }

            Type GetTargetAbstractType()
            {
                var nativePropertyName = GetPropertyName(false);
                var nativeProperty = property.FindPropertyRelative(nativePropertyName);
                return ReflectionUtils.ExtractReferenceFieldTypeFromSerializedProperty(nativeProperty);
            }
        }
    }
}
