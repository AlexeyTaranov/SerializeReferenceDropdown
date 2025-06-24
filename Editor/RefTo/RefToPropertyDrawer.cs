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
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<,>))]
    [CustomPropertyDrawer(typeof(RefTo<>))]
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

            var style = isSameType
                ? EditorStyles.boldLabel
                : new GUIStyle(EditorStyles.boldLabel)
                    { normal = new GUIStyleState() { textColor = Color.red } };
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

            var copyButton = root.Q<Button>("copy");
            var pasteButton = root.Q<Button>("paste");
            var resetButton = root.Q<Button>("reset");

            var fixButton = root.Q<Button>("fix-missing-references");
            fixButton.SetDisplayElement(false);
            fixButton.clicked += FixMissingReference;

            copyButton.clicked += () =>
            {
                using var localSo = new SerializedObject(targetObject);
                using var localProperty = localSo.FindProperty(propertyPath);
                var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(localProperty);
                RefToContextMenu.CopyDirectValues((id, host, targetType));
                _dirtyRefreshes[property.serializedObject].Invoke();
            };

            pasteButton.SetDisplayElement(false);
            pasteButton.clicked += () => RefToContextMenu.PasteToProperty(property);
            resetButton.clicked += Reset;

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

            void Reset()
            {
                RefToExtensions.ResetRefTo(property);
            }

            void RefreshDynamic(SerializedObject so)
            {
                using var localProperty = so.FindProperty(propertyPath);

                var (refType, _, _, host, isSameType) = RefToExtensions.GetInspectorValues(localProperty);
                var refLabel = root.Q<Label>("ref-name");
                var refTypeName = refType == null ? "null" : refType.Name;
                refLabel.text = $"R:{refTypeName}";
                refLabel.tooltip = $"Reference \nType: {refType?.Name} \nNamespace: {refType?.Namespace}";
                objectField.SetValueWithoutNotify(host);

                var isErrorType = host != null && isSameType == false;
                if (isErrorType)
                {
                    refLabel.AddToClassList("error-bg");
                }
                else
                {
                    refLabel.RemoveFromClassList("error-bg");
                }

                fixButton.SetDisplayElement(isErrorType);
                resetButton.SetDisplayElement(host != null);
                copyButton.SetDisplayElement(isSameType && host != null);

                var pasteType = RefToContextMenu.GetPasteType(property);
                pasteButton.SetDisplayElement(pasteType == PasteType.CanPaste);
                if (pasteType == PasteType.CanPaste)
                {
                    var pasteHost = RefToContextMenu.copy.host;
                    var pasteValue =
                        ManagedReferenceUtility.GetManagedReference(RefToContextMenu.copy.host,
                            RefToContextMenu.copy.refId);
                    pasteButton.tooltip = $"Paste: Host - {pasteHost.name}, Value - {pasteValue.GetType()}";
                }
            }

            void ApplyRefToFromObject(ChangeEvent<Object> evt)
            {
                var newValue = evt.newValue;
                if (newValue == null)
                {
                    Reset();
                    return;
                }

                var canUseComponents = hostType.IsAssignableFrom(typeof(MonoBehaviour));
                if (newValue is GameObject go && canUseComponents)
                {
                    var components = go.GetComponents<MonoBehaviour>();
                    foreach (var component in components)
                    {
                        if (TryApplyRefToValue(property, component, targetType))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (TryApplyRefToValue(property, newValue, targetType))
                    {
                        return;
                    }
                }

                if (newValue != null)
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
                else
                {
                    fixButton.SetDisplayElement(false);
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