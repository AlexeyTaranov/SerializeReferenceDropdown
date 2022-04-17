using UnityEditor;
using UnityEngine;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(AnyType<>))]
    public class AnyTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var refTypeProperty = property.FindPropertyRelative("_isUnityObjectReference");
            var isUnityObj = refTypeProperty.boolValue;

            DrawLeftReferenceTypeButton();

            rect.width -= 40;
            rect.x += 40;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(GetPropertyName(isUnityObj)), label);
            EditorGUI.EndProperty();

            void DrawLeftReferenceTypeButton()
            {
                var refTypeButton = isUnityObj ? "U" : "#";
                var firstRect = new Rect(rect);
                firstRect.width = 20;
                firstRect.height = EditorGUIUtility.singleLineHeight;
                if(GUI.Button(firstRect,refTypeButton))
                {
                    isUnityObj = !isUnityObj;
                    refTypeProperty.boolValue = isUnityObj;
                    refTypeProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var isUnityObject = property.FindPropertyRelative("_isUnityObjectReference").boolValue;
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(GetPropertyName(isUnityObject)), label, true);
        }

        private string GetPropertyName(bool isUnityObject) => isUnityObject ? "_unityObject" : "_nativeObject";
    }
}
