using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.AnyType
{
    [CustomPropertyDrawer(typeof(AnyType<>))]
    public class AnyTypeDrawer : PropertyDrawer
    {
        private static readonly (string typeEnum, string unityObject, string nativeObject) PropertyName =
            ("isUnityObjectReference", "unityObject", "nativeObject");

        private AnyTypeDrawerUnityObject unityObjectDrawer;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var refTypeProperty = property.FindPropertyRelative(PropertyName.typeEnum);
            var isUnityObj = refTypeProperty.boolValue;

            var leftButtonRect = DrawLeftReferenceTypeButton();

            rect.width -= 40;
            rect.x += 40;
            if (isUnityObj)
            {
                unityObjectDrawer ??= new AnyTypeDrawerUnityObject(
                    GetFieldProperty(property, true),
                    GetFieldProperty(property, false));
                unityObjectDrawer.DrawUnityReferenceType(label, rect, leftButtonRect);
            }
            else
            {
                EditorGUI.PropertyField(rect, GetFieldProperty(property, false), label);
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
            var isUnityObject = property.FindPropertyRelative(PropertyName.typeEnum).boolValue;
            return EditorGUI.GetPropertyHeight(GetFieldProperty(property, isUnityObject), label, true);
        }

        private SerializedProperty GetFieldProperty(SerializedProperty property, bool isUnityObject)
        {
            var propertyName = isUnityObject ? PropertyName.unityObject : PropertyName.nativeObject;
            return property.FindPropertyRelative(propertyName);
        }
    }
}