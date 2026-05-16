using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public class RefToPropertyDrawerUIToolkit
    {
        private static VisualTreeAsset _visualTreeAsset;
        private static IEnumerable<FieldInfo> _refToCreateFields;

        private static VisualTreeAsset VisualTreeAsset
        {
            get
            {
                if (_visualTreeAsset == null)
                {
                    var treeAssetPath = Path.Combine(Paths.PackageLayouts, "RefTo.uxml");
                    _visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
                }
                return _visualTreeAsset;
            }
        }

        private static IEnumerable<FieldInfo> RefToCreateFields =>
            _refToCreateFields ??= TypeCache.GetFieldsWithAttribute<RefToCreateOnDragToFieldAttribute>();

        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            DrawUIToolkit(root, property);
            return root;
        }

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            root.Add(VisualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);

            var (_, refToTargetType, hostType, _, _) = RefToExtensions.GetInspectorValues(property);
            var propertyPath = property.propertyPath;
            var targetObject = property.serializedObject.targetObject;
            var boxedType = property.boxedValue?.GetType();
            var needCreateOnDragToField = RefToCreateFields.Any(t => t.FieldType == boxedType);

            var objectField = root.Q<ObjectField>();
            var pingButton = root.Q<Button>("ping");
            var fixButton = root.Q<Button>("fix-missing-references");
            var refLabel = root.Q<Label>("ref-name");

            objectField.objectType = hostType;

            SetupPingButton(pingButton, targetObject, propertyPath);
            SetupFixButton(fixButton, property, refToTargetType, objectField);
            SetupObjectField(objectField, property, refToTargetType, hostType, propertyField, needCreateOnDragToField);

            root.TrackSerializedObjectValue(property.serializedObject, RefreshDynamic);
            RefreshDynamic(property.serializedObject);

            void RefreshDynamic(SerializedObject so)
            {
                using var localProperty = so.FindProperty(propertyPath);
                if (localProperty == null)
                {
                    return;
                }

                var (refType, _, _, host, isSameType) = RefToExtensions.GetInspectorValues(localProperty);
                var refTypeName = refType == null ? "null" : refType.Name;
                refLabel.text = refTypeName;
                refLabel.tooltip = $"Reference \nType: {refType?.Name} \nNamespace: {refType?.Namespace}";
                objectField.SetValueWithoutNotify(host);

                var isErrorType = host != null && isSameType == false;
                refLabel.EnableInClassList("error-bg", isErrorType);
                fixButton.SetDisplayElement(isErrorType);
            }
        }

        private void SetupPingButton(Button button, Object targetObject, string propertyPath)
        {
            button.clicked += () =>
            {
                using var localSo = new SerializedObject(targetObject);
                using var localProperty = localSo.FindProperty(propertyPath);
                if (localProperty == null) return;

                var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(localProperty);
                if (host != null)
                {
                    EditorGUIUtility.PingObject(host);
                    PropertyDrawerUIToolkit.PingSerializeReference(host, id);
                }
            };
        }

        private void SetupFixButton(Button button, SerializedProperty property, Type refToTargetType,
            ObjectField objectField)
        {
            button.SetDisplayElement(false);
            button.clicked += () =>
            {
                var prevObject = objectField.value;
                objectField.SetValueWithoutNotify(null);
                if (RefToDrawerUtils.TryApplyRefToValue(property, prevObject, refToTargetType))
                {
                    objectField.SetValueWithoutNotify(prevObject);
                }
                else
                {
                    button.SetDisplayElement(false);
                }
            };
        }

        private void SetupObjectField(ObjectField objectField, SerializedProperty property, Type refToTargetType,
            Type hostType, PropertyField propertyField, bool needCreateOnDragToField)
        {
            objectField.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (newValue == null)
                {
                    RefToExtensions.ResetRefTo(property);
                    return;
                }

                var assignedNewValue = RefToDrawerUtils.UnityObjectIterator(newValue, hostType,
                    o => RefToDrawerUtils.TryApplyRefToValue(property, o, refToTargetType));
                if (assignedNewValue)
                {
                    return;
                }

                if (needCreateOnDragToField)
                {
                    var fieldMatrix = objectField.worldTransform;
                    var position = new Vector3(fieldMatrix.m03, fieldMatrix.m13, fieldMatrix.m23);
                    var dropdownRect = new Rect(position, objectField.contentRect.size);
                    if (RefToDrawerUtils.TryApplyNewInstanceToDragObject(refToTargetType, hostType, property, newValue,
                            dropdownRect,
                            propertyField.contentRect))
                    {
                        return;
                    }
                }

                if (newValue != null)
                {
                    objectField.SetValueWithoutNotify(evt.previousValue);
                }
            });
        }
    }
}