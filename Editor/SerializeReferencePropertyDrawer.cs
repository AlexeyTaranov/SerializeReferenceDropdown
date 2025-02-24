using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferencePropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;
        private Rect propertyRect;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            propertyRect = rect;

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawIMGUITypeDropdown(rect, property, label);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawUIToolkitTypeDropdown(root, property);
            }
            else
            {
                root.Add(new PropertyField(property));
            }

            return root;
        }

        private void DrawUIToolkitTypeDropdown(VisualElement root, SerializedProperty property)
        {
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SerializeReferenceDropdown.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            var selectTypeButton = root.Q<Button>();
            selectTypeButton.clickable.clicked += ShowDropdown;
            var propertyPath = property.propertyPath;
            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, UpdateDropdown);
            UpdateDropdown(property.serializedObject);

            void ShowDropdown()
            {
                var dropdown = new AdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index => { WriteNewInstanceByIndexType(index, property); });
                var buttonMatrix = selectTypeButton.worldTransform;
                var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
                var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }

            void UpdateDropdown(SerializedObject so)
            {
                var prop = so.FindProperty(propertyPath);
                propertyField.BindProperty(prop);
                var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                selectTypeButton.text = selectedTypeName;
            }
        }

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);
            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            var dropdownRect = GetDropdownIMGUIRect(rect);

            EditorGUI.EndDisabledGroup();

            var dropdownTypeContent = new GUIContent(
                text: GetTypeName(referenceType),
                tooltip: GetTypeTooltip(referenceType));
            if (EditorGUI.DropdownButton(dropdownRect, dropdownTypeContent, FocusType.Keyboard))
            {
                var dropdown = new AdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property));
                dropdown.Show(dropdownRect);
            }

            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(rect, property, label, true);
            EditorGUI.indentLevel--;

            Rect GetDropdownIMGUIRect(Rect mainRect)
            {
                var dropdownOffset = EditorGUIUtility.labelWidth;
                Rect rect = new Rect(mainRect);
                rect.width -= dropdownOffset;
                rect.x += dropdownOffset;
                rect.height = EditorGUIUtility.singleLineHeight;

                return rect;
            }
        }

        private string GetTypeName(Type type)
        {
            if (type == null)
            {
                return NullName;
            }

            var typesWithNames = TypeCache.GetTypesWithAttribute(typeof(SerializeReferenceDropdownNameAttribute));
            if (typesWithNames.Contains(type))
            {
                var dropdownNameAttribute = type.GetCustomAttribute<SerializeReferenceDropdownNameAttribute>();
                return dropdownNameAttribute.Name;
            }

            if (type.IsGenericType)
            {
                var genericNames = type.GenericTypeArguments.Select(t => t.Name);
                var genericParamNames = " [" + string.Join(",", genericNames) + "]";
                var genericName = ObjectNames.NicifyVariableName(type.Name) + genericParamNames;
                return genericName;
            }

            return ObjectNames.NicifyVariableName(type.Name);
        }

        private string GetTypeTooltip(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithTooltip = TypeCache.GetTypesWithAttribute(typeof(TypeTooltipAttribute));
            if (typesWithTooltip.Contains(type))
            {
                var tooltipAttribute = type.GetCustomAttribute<TypeTooltipAttribute>();
                return tooltipAttribute.tooltip;
            }

            return String.Empty;
        }

        private List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var derivedTypes = TypeCache.GetTypesDerivedFrom(propertyType);
            var nonUnityTypes = derivedTypes.Where(IsAssignableNonUnityType).ToList();
            nonUnityTypes.Insert(0, null);
            if (propertyType.IsGenericType && propertyType.IsInterface)
            {
                var allTypes = TypeUtils.GetAllTypesInCurrentDomain().Where(IsAssignableNonUnityType)
                    .Where(t => t.IsGenericType);

                var assignableGenericTypes = allTypes.Where(IsImplementedGenericInterfacesFromGenericProperty);
                nonUnityTypes.AddRange(assignableGenericTypes);
            }

            return nonUnityTypes;

            bool IsAssignableNonUnityType(Type type)
            {
                return TypeUtils.IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }

            bool IsImplementedGenericInterfacesFromGenericProperty(Type type)
            {
                var interfaces = type.GetInterfaces().Where(t => t.IsGenericType);
                var isImplementedInterface = interfaces.Any(t =>
                    t.GetGenericTypeDefinition() == propertyType.GetGenericTypeDefinition());
                return isImplementedInterface;
            }
        }

        private void WriteNewInstanceByIndexType(int typeIndex, SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);

            if (newType?.IsGenericType == true)
            {
                var concreteGenericType = TypeUtils.GetConcreteGenericType(propertyType, newType);
                if (concreteGenericType != null)
                {
                    CreateAndApplyNewInstanceFromType(concreteGenericType);
                }
                else
                {
                    GenericTypeCreateWindow.ShowCreateTypeMenu(property, propertyRect, newType,
                        CreateAndApplyNewInstanceFromType);
                }
            }
            else
            {
                CreateAndApplyNewInstanceFromType(newType);
            }


            void CreateAndApplyNewInstanceFromType(Type type)
            {
                object newObject;
                if (type?.GetConstructor(Type.EmptyTypes) != null)
                {
                    newObject = Activator.CreateInstance(type);
                }
                else
                {
                    newObject = type != null ? FormatterServices.GetUninitializedObject(type) : null;
                }

                ApplyValueToProperty(newObject);
            }

            void ApplyValueToProperty(object value)
            {
                var targets = property.serializedObject.targetObjects;
                // Multiple object edit.
                //One Serialized Object for multiple Objects work sometimes incorrectly  
                foreach (var target in targets)
                {
                    using var so = new SerializedObject(target);
                    var targetProperty = so.FindProperty(property.propertyPath);
                    targetProperty.managedReferenceValue = value;
                    so.ApplyModifiedProperties();
                    so.Update();
                }
            }
        }
    }
}