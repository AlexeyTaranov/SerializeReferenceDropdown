#if UNITY_2023_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<,>))]
    public class RefToPropertyDrawer : PropertyDrawer
    {
        private static Dictionary<SerializedObject, Action>
            _dirtyRefreshes = new Dictionary<SerializedObject, Action>();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            using var pooled = ListPool<SerializedObject>.Get(out var destroyList);
            _dirtyRefreshes.Where(t => t.Key != null).ForEach(t => destroyList.Add(t.Key));
            destroyList.ForEach(t => _dirtyRefreshes.Remove(t));
            DrawUIToolkit(root, property);
            return root;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, false);
        }

        private GUIStyle ErrorStyle = new GUIStyle(EditorStyles.boldLabel)
            { normal = new GUIStyleState() { textColor = Color.red } };

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var propertyRect = rect;
            var (refType, targetType, hostType, host, isSameType) = RefToExtensions.GetInspectorValues(property);
            label.tooltip = $"Target \nType - {targetType.Name} \nNamespace - {targetType.Namespace}";
            EditorGUI.BeginProperty(rect, label, property);
            var labelWidth = EditorGUIUtility.labelWidth / 2;
            propertyRect.width = labelWidth;
            EditorGUI.LabelField(propertyRect, label);

            var height = EditorGUIUtility.singleLineHeight;

            var fieldSize = (rect.width - labelWidth) * 0.5f;
            var labelRect = new Rect(rect.position + new Vector2(labelWidth, 0),
                new Vector2(rect.width - fieldSize - labelWidth, height));
            var fieldRect = new Rect(labelRect.position + new Vector2(labelRect.width, 0),
                new Vector2(fieldSize, height));

            var style = isSameType ? EditorStyles.boldLabel : ErrorStyle;
            var refLabel = $" R: {refType?.Name}";
            EditorGUI.LabelField(labelRect,
                new GUIContent(refLabel, $"Reference \nType - {refType?.Name} \nNamespace - {refType?.Namespace}"),
                style);

            var newValue = EditorGUI.ObjectField(fieldRect, host, hostType, true);
            if (host != newValue)
            {
                if (newValue != null)
                {
                    TryApplyRefToValue(property, newValue, targetType);
                }
                else
                {
                    RefToExtensions.ResetRefTo(property);
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "RefTo.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());

            var (_, targetType, hostType, _, _) = RefToExtensions.GetInspectorValues(property);

            var objectField = root.Q<ObjectField>();
            var propertyPath = property.propertyPath;
            var targetObject = property.serializedObject.targetObject;

            var fieldName = root.Q<Label>("property-name");
            fieldName.text = ObjectNames.NicifyVariableName(property.name);
            fieldName.tooltip =
                $"Field: {property.name} \nTarget Type: {targetType.Name} \nNamespace: {targetType.Namespace}";

            var fixButton = root.Q<Button>("fix-missing-references");
            fixButton.SetDisplayElement(false);
            fixButton.clicked += FixMissingReference;

            var copyButton = root.Q<Button>("copy");
            copyButton.clicked += () =>
            {
                using var localSo = new SerializedObject(targetObject);
                using var localProperty = localSo.FindProperty(propertyPath);
                var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(localProperty);
                RefToContextMenu.CopyDirectValues((id, host, targetType));
                _dirtyRefreshes[property.serializedObject].Invoke();
            };

            var pasteButton = root.Q<Button>("paste");
            pasteButton.SetDisplayElement(false);
            pasteButton.clicked += () => RefToContextMenu.PasteToProperty(property);

            var resetButton = root.Q<Button>("reset");

            resetButton.clicked += () => RefToExtensions.ResetRefTo(property);

            root.TrackSerializedObjectValue(property.serializedObject, RefreshDynamic);

            objectField.objectType = hostType;
            objectField.RegisterValueChangedCallback(ApplyRefToFromObject);
            if (_dirtyRefreshes.TryGetValue(property.serializedObject, out var action))
            {
                action += Refresh;
                _dirtyRefreshes[property.serializedObject] = action;
            }
            else
            {
                _dirtyRefreshes.Add(property.serializedObject, Refresh);
            }

            Refresh();


            void Refresh()
            {
                RefreshDynamic(property.serializedObject);
            }

            void RefreshDynamic(SerializedObject so)
            {
                using var localProperty = so.FindProperty(propertyPath);

                var (refType, _, _, host, isSameType) = RefToExtensions.GetInspectorValues(localProperty);
                var refLabel = root.Q<Label>("ref-name");
                var refTypeName = refType == null ? "null" : refType.Name;
                refLabel.text = $"R:{refTypeName}";
                refLabel.tooltip = $"Reference \nType: {refType?.Name} \nNamespace: {refType?.Namespace}";
                refLabel.style.color = isSameType ? new StyleColor(Color.white) : new StyleColor(Color.red);

                objectField.SetValueWithoutNotify(host);

                fixButton.SetDisplayElement(host != null && isSameType == false);
                resetButton.SetDisplayElement(host != null);
                copyButton.SetDisplayElement(isSameType && host != null);

                var pasteType = RefToContextMenu.GetPasteType(property);
                pasteButton.SetDisplayElement(pasteType == PasteType.CanPaste);
            }

            void ApplyRefToFromObject(ChangeEvent<Object> evt)
            {
                var newValue = evt.newValue;
                if (TryApplyRefToValue(property, newValue, targetType) == false && newValue != null)
                {
                    objectField.SetValueWithoutNotify(evt.previousValue);
                }
            }

            void FixMissingReference()
            {
                var prevObject = objectField.value;
                objectField.SetValueWithoutNotify(null);
                if (TryApplyRefToValue(property, prevObject, targetType))
                {
                    objectField.SetValueWithoutNotify(prevObject);
                }
            }
        }

        private bool TryApplyRefToValue(SerializedProperty toProperty, Object newObject, Type targetType)
        {
            return SOUtils.TraverseSO(newObject, TryWriteToRefTo);

            bool TryWriteToRefTo(SerializedProperty refProperty)
            {
                var refType = TypeUtils.ExtractTypeFromString(refProperty.managedReferenceFullTypename);
                if (targetType.IsAssignableFrom(refType))
                {
                    RefToExtensions.WriteRefToFromPropertyToProperty(refProperty, toProperty);
                    return true;
                }

                return false;
            }
        }
    }
}
#endif