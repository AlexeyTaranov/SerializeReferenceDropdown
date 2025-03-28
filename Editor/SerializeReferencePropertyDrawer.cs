using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferencePropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;
        private Rect propertyRect;

        //TODO Need find better solution for check ui update and traverse all serialized properties
        private static bool _isDirtyUIToolkit;

        //TODO: need to find better solution
        private static Dictionary<Object, HashSet<string>> _targetObjectAndSerializeReferences =
            new Dictionary<Object, HashSet<string>>();

        //TODO Make better unique colors for equal references
        private static Color GetColorForEqualSerializedReference(SerializedProperty property)
        {
            var refId = ManagedReferenceUtility.GetManagedReferenceIdForObject(property.serializedObject.targetObject,
                property.managedReferenceValue);
            var refsArray = ManagedReferenceUtility.GetManagedReferenceIds(property.serializedObject.targetObject);
            var index = Array.FindIndex(refsArray, t => t == refId);
            var hue = (float)index / refsArray.Length;
            return Color.HSVToRGB(hue, 0.8f, 0.8f);
        }

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
            bool isNew = true;
            var uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SerializeReferenceDropdown.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();

            var selectTypeButton = root.Q<Button>("typeSelect");
            selectTypeButton.clickable.clicked += ShowDropdown;
            var fixCrossRefButton = root.Q<Button>("fixCrossReferences");
            fixCrossRefButton.clickable.clicked += () =>
            {
                MakeDirtyUIToolkit();
                FixCrossReference(property);
            };

            var propertyPath = property.propertyPath;
            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, UpdateDropdown);
            UpdateDropdown(property.serializedObject);
            isNew = false;

            void ShowDropdown()
            {
                var dropdown = new AdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index =>
                    {
                        MakeDirtyUIToolkit();
                        WriteNewInstanceByIndexType(index, property);
                    });
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
                if (isNew == false && _isDirtyUIToolkit == false)
                {
                    return;
                }
                selectTypeButton.style.color = new StyleColor(Color.white);
                fixCrossRefButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                if (IsHaveSameOtherSerializeReference(property))
                {
                    fixCrossRefButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                    var color = GetColorForEqualSerializedReference(property);
                    selectTypeButton.style.color = color;
                }
            }

            void MakeDirtyUIToolkit()
            {
                _isDirtyUIToolkit = true;
                EditorApplication.delayCall += () =>
                {
                    _isDirtyUIToolkit = false;
                };
            }
        }

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float FixButtonWidth = 40f;
            assignableTypes ??= GetAssignableTypes(property);

            var isHaveOtherReference = IsHaveSameOtherSerializeReference(property);

            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            var dropdownRect = GetDropdownIMGUIRect(rect);

            EditorGUI.EndDisabledGroup();

            var dropdownTypeContent = new GUIContent(
                text: GetTypeName(referenceType),
                tooltip: GetTypeTooltip(referenceType));

            var style = EditorStyles.miniPullDown;
            if (isHaveOtherReference)
            {
                var uniqueColor = GetColorForEqualSerializedReference(property);
                style = new GUIStyle(EditorStyles.miniPullDown)
                    { normal = new GUIStyleState() { textColor = uniqueColor } };
            }

            if (EditorGUI.DropdownButton(dropdownRect, dropdownTypeContent, FocusType.Keyboard, style))
            {
                var dropdown = new AdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property));
                dropdown.Show(dropdownRect);
            }

            if (isHaveOtherReference)
            {
                if (GUI.Button(GetFixCrossReferencesRect(dropdownRect), "Fix"))
                {
                    FixCrossReference(property);
                }
            }

            EditorGUI.PropertyField(rect, property, label, true);


            Rect GetDropdownIMGUIRect(Rect mainRect)
            {
                var dropdownOffset = EditorGUIUtility.labelWidth;
                Rect rect = new Rect(mainRect);
                rect.width -= dropdownOffset;
                rect.x += dropdownOffset;
                rect.height = EditorGUIUtility.singleLineHeight;
                if (isHaveOtherReference)
                {
                    rect.width -= FixButtonWidth;
                }

                return rect;
            }

            Rect GetFixCrossReferencesRect(Rect rectIn)
            {
                var newRect = rectIn;
                newRect.x += rectIn.width + EditorGUIUtility.standardVerticalSpacing;
                newRect.width = FixButtonWidth - EditorGUIUtility.standardVerticalSpacing;
                return newRect;
            }
        }

        private bool IsHaveSameOtherSerializeReference(SerializedProperty property)
        {
            if (property.managedReferenceValue == null)
            {
                return false;
            }

            var target = property.serializedObject.targetObject;
            if (_targetObjectAndSerializeReferences.TryGetValue(target, out var serializeReferencePaths) == false)
            {
                serializeReferencePaths = new HashSet<string>();
                _targetObjectAndSerializeReferences.Add(target, serializeReferencePaths);
            }

            // Can't find this path in serialized object. Example - new element in array
            if (serializeReferencePaths.Contains(property.propertyPath) == false)
            {
                var paths = FindAllSerializeReferencePathsInTargetObject(property);
                serializeReferencePaths.Clear();
                foreach (var path in paths)
                {
                    serializeReferencePaths.Add(path);
                }
            }

            foreach (var referencePath in serializeReferencePaths)
            {
                if (property.propertyPath == referencePath)
                {
                    continue;
                }

                using var otherProperty = property.serializedObject.FindProperty(referencePath);
                if (otherProperty != null)
                {
                    if (otherProperty.managedReferenceId == property.managedReferenceId)
                    {
                        return true;
                    }
                }
                else
                {
                    //TODO null property???
                }
            }

            return false;
        }

        private HashSet<string> FindAllSerializeReferencePathsInTargetObject(SerializedProperty property)
        {
            using var iterator = property.serializedObject.GetIterator();
            iterator.NextVisible(true);
            var paths = new HashSet<string>();
            PropertyUtils.TraverseProperty(iterator, string.Empty, FillAllPaths);
            return paths;

            bool FillAllPaths(SerializedProperty serializeReferenceProperty)
            {
                paths.Add(serializeReferenceProperty.propertyPath);
                return false;
            }
        }

        private void FixCrossReference(SerializedProperty property)
        {
            var json = JsonUtility.ToJson(property.managedReferenceValue);
            CreateAndApplyNewInstanceFromType(property.managedReferenceValue.GetType(), property);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
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
                    CreateAndApplyNewInstanceFromType(concreteGenericType, property);
                }
                else
                {
                    GenericTypeCreateWindow.ShowCreateTypeMenu(property, propertyRect, newType,
                        (type) => CreateAndApplyNewInstanceFromType(type, property));
                }
            }
            else
            {
                CreateAndApplyNewInstanceFromType(newType, property);
            }
        }

        private void CreateAndApplyNewInstanceFromType(Type type, SerializedProperty property)
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