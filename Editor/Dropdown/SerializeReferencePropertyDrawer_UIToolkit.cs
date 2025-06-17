using System.IO;
using System.Linq;
using SerializeReferenceDropdown.Editor.EditReferenceType;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public partial class SerializeReferencePropertyDrawer : PropertyDrawer
    {
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
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SerializeReferenceDropdown.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);
            var propertyPath = property.propertyPath;

            var selectTypeButton = root.Q<Button>("type-select");
            selectTypeButton.clickable.clicked += ShowDropdown;

            var fixCrossRefButton = root.Q<Button>("fix-cross-references");
            fixCrossRefButton.clickable.clicked += () =>
            {
                FixCrossReference(property);
            };
            var openSourceFIleButton = root.Q<Button>("open-source-file");
            openSourceFIleButton.SetDisplayElement(false);
            openSourceFIleButton.clicked += () => { OpenSourceFile(property.managedReferenceValue.GetType()); };

            var modifyDirectType = root.Q<Button>("modify-direct-type");
            modifyDirectType.clicked += ModifyDirectType;

            var showSearchToolButton = root.Q<Button>("show-search-tool");
            showSearchToolButton.SetDisplayElement(false);
            showSearchToolButton.clicked += () =>
            {
                var type = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                ShowSearchTool(type);
            };

            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, RefreshDropdown);
            RefreshDropdown(property.serializedObject);

            void ShowDropdown()
            {
                var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index =>
                    {
                        WriteNewInstanceByIndexType(index, property, registerUndo: true);
                    });
                var buttonMatrix = selectTypeButton.worldTransform;
                var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
                var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }

            void RefreshDropdown(SerializedObject so)
            {
                var prop = so.FindProperty(propertyPath);
                var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                var tooltipText = $"Type Full Name: {selectedType?.FullName}";
                var nullTypeId = -2;
                var isNullValue = prop.managedReferenceId == nullTypeId || prop.managedReferenceId == 0;
                var isHaveMissingType = selectedType == null && isNullValue == false;
                if (isHaveMissingType)
                {
                    var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(so.targetObject);
                    var thisMissingType = missingTypes.FirstOrDefault(t => t.referenceId == prop.managedReferenceId);
                    selectedTypeName = string.IsNullOrEmpty(thisMissingType.className) ? "MISSING TYPE" : $"MISSING TYPE: {thisMissingType.className}";
                    tooltipText = thisMissingType.GetDetailData();
                    selectTypeButton.AddToClassList("error-bg");
                }
                else
                {
                    selectTypeButton.RemoveFromClassList("error-bg");
                }
                
                selectTypeButton.text = selectedTypeName;
                selectTypeButton.tooltip = tooltipText;

                openSourceFIleButton.SetDisplayElement(property.managedReferenceValue != null);

                var activeSearchTool = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableSearchTool;
                showSearchToolButton.SetDisplayElement(selectedType != null && activeSearchTool);
                fixCrossRefButton.SetDisplayElement(false);
                
                modifyDirectType.SetDisplayElement(selectedType != null);

                if (IsHaveSameOtherSerializeReference(property))
                {
                    fixCrossRefButton.SetDisplayElement(true);
                    var color = GetColorForEqualSerializeReference(property);
                    selectTypeButton.style.color = color;
                }
            }
            
            void ModifyDirectType()
            {
                var prop = property.serializedObject.FindProperty(propertyPath);
                var type = prop.managedReferenceValue.GetType();
                var typeData = new TypeData()
                {
                    AssemblyName = type.Assembly.GetName().Name,
                    ClassName = type.Name,
                    Namespace = type.Namespace
                };
                EditReferenceTypeWindow.ShowWindow(typeData, data =>
                {
                    var obj = property.serializedObject.targetObject;
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path))
                    {
                        path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                    }

                    if (string.IsNullOrEmpty(path))
                    {
                        Log.Error($"Can't find path for this object type: {obj.name}");
                    }
                    else
                    {
                        EditReferenceTypeUtils.TryModifyDirectFileReferenceType(path, property.managedReferenceId, typeData,
                            data);
                    }
                });
            }
        }
    }
}