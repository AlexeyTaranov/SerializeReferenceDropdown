using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    public partial class RefToPropertyDrawer
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

        private void DrawUIToolkit(VisualElement root, SerializedProperty property)
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "RefTo.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);

            var (_, refToTargetType, hostType, _, _) = RefToExtensions.GetInspectorValues(property);

            var objectField = root.Q<ObjectField>();
            var propertyPath = property.propertyPath;
            var targetObject = property.serializedObject.targetObject;
            var refToCreateFields = TypeCache.GetFieldsWithAttribute<RefToCreateOnDragToFieldAttribute>();
            var boxedType = property.boxedValue?.GetType();
            var needCreateOnDragToField = refToCreateFields.Any(t => t.FieldType == boxedType);

            var pingButton = root.Q<Button>("ping");

            var fixButton = root.Q<Button>("fix-missing-references");
            fixButton.SetDisplayElement(false);
            fixButton.clicked += FixMissingReference;

            pingButton.clicked += () =>
            {
                using var localSo = new SerializedObject(targetObject);
                using var localProperty = localSo.FindProperty(propertyPath);
                var (host, id) = RefToExtensions.GetRefToFieldsFromProperty(localProperty);
                if (host != null)
                {
                    EditorGUIUtility.PingObject(host);
                    PropertyDrawerUIToolkit.PingSerializeReference(host, id);
                }
            };

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
                refLabel.text = refTypeName;
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
            }

            void ApplyRefToFromObject(ChangeEvent<Object> evt)
            {
                var newValue = evt.newValue;
                if (newValue == null)
                {
                    RefToExtensions.ResetRefTo(property);
                    return;
                }

                var assignedNewValue = UnityObjectIterator(newValue, hostType,
                    o => TryApplyRefToValue(property, o, refToTargetType));
                if (assignedNewValue)
                {
                    return;
                }

                if (needCreateOnDragToField)
                {
                    var fieldMatrix = objectField.worldTransform;
                    var position = new Vector3(fieldMatrix.m03, fieldMatrix.m13, fieldMatrix.m23);
                    var dropdownRect = new Rect(position, objectField.contentRect.size);
                    if (TryApplyNewInstanceToDragObject(refToTargetType, hostType, property, newValue, dropdownRect,
                            propertyField.contentRect))
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
                if (TryApplyRefToValue(property, prevObject, refToTargetType))
                {
                    objectField.SetValueWithoutNotify(prevObject);
                }
                else
                {
                    fixButton.SetDisplayElement(false);
                }
            }
        }
    }
}