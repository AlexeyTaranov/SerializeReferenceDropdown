using System;
using System.IO;
using SerializeReferenceDropdown.Editor.EditReferenceType;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using SerializeReferenceDropdown.Editor.YAMLEdit;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    public partial class SerializeReferencePropertyDrawer
    {
        private StyleColor defaultSelectTypeTextColor;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawUIToolkitTypeDropdown(root, property);
                AddSelfToAllProperties(property);
            }
            else
            {
                root.Add(new PropertyField(property));
            }

            return root;
        }

        private void AddSelfToAllProperties(SerializedProperty property)
        {
            using var pooled = ListPool<SerializeReferencePropertyDrawer>.Get(out var destroyedPropertyDrawers);
            foreach (var pair in _allPropertyDrawers)
            {
                try
                {
                    // Need find better solution how to check - is exist or not property drawer
                    var context = pair.Value.serializedObject.context;
                }
                catch (Exception e)
                {
                    destroyedPropertyDrawers.Add(pair.Key);
                }
            }

            foreach (var propertyDrawer in destroyedPropertyDrawers)
            {
                _allPropertyDrawers.Remove(propertyDrawer);
            }

            _allPropertyDrawers[this] = property;
        }

        private void DrawUIToolkitTypeDropdown(VisualElement root, SerializedProperty property)
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SerializeReferenceDropdown.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);
            var propertyPath = property.propertyPath;
            var targetObject = property.serializedObject.targetObject;

            var selectTypeButton = root.Q<Button>("type-select");
            selectTypeButton.clickable.clicked += ShowDropdown;
            defaultSelectTypeTextColor = selectTypeButton.style.color;

            var fixCrossRefButton = root.Q<Button>("fix-cross-references");
            fixCrossRefButton.clickable.clicked += () => { FixCrossReference(property); };

            var openSourceFIleButton = root.Q<Button>("open-source-file");
            openSourceFIleButton.SetDisplayElement(false);
            openSourceFIleButton.clicked += () => { OpenSourceFile(property.managedReferenceValue.GetType()); };

            var modifyDirectType = root.Q<Button>("modify-direct-type");
            var assetPath = AssetDatabase.GetAssetPath(targetObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetObject);
            }

            var showSearchToolButton = root.Q<Button>("show-search-tool");

            var canModifyDirectType = string.IsNullOrEmpty(assetPath) == false;
            modifyDirectType.SetDisplayElement(canModifyDirectType);
            modifyDirectType.clicked += ModifyDirectType;

            showSearchToolButton.SetDisplayElement(false);
            showSearchToolButton.clicked += () =>
            {
                var type = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                ShowSearchTool(type);
            };

            assignableTypes ??= GetAssignableTypes(property);
            root.TrackSerializedObjectValue(property.serializedObject, RefreshDropdown);
            RefreshDropdown(property.serializedObject);

            var needPingOnFirstShowPropertyDrawer = previousSelection != pingObject && previousSelection != null &&
                                                    property.managedReferenceId == pingRefId;

            if (needPingOnFirstShowPropertyDrawer)
            {
                EditorApplication.delayCall += PingImpl;
            }

            _pingSelf = PingNow;

            void PingNow()
            {
                if (pingObject == property.serializedObject.targetObject && property.managedReferenceId == pingRefId)
                {
                    PingImpl();
                }
            }

            void PingImpl()
            {
                var rootElement = root.Q<VisualElement>("root");
                var pingStyle = "ping-serialize-reference";
                if (rootElement.ClassListContains(pingStyle) == false)
                {
                    rootElement.AddToClassList(pingStyle);
                    rootElement.RegisterCallback<TransitionEndEvent>(TransitionEndEventOnce);
                }

                void TransitionEndEventOnce(TransitionEndEvent evt)
                {
                    rootElement.RemoveFromClassList(pingStyle);
                    rootElement.UnregisterCallback<TransitionEndEvent>(TransitionEndEventOnce);
                }
            }

            void ShowDropdown()
            {
                var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes,
                    type => { WriteNewInstanceByType(type, property, propertyRect, registerUndo: true); });
                var buttonMatrix = selectTypeButton.worldTransform;
                var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
                var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }

            void RefreshDropdown(SerializedObject so)
            {
                if (so == null)
                {
                    return;
                }

                var prop = so.FindProperty(propertyPath);
                var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
                var selectedTypeName = GetTypeName(selectedType);
                var tooltipText = $"Type Full Name: {selectedType?.FullName}";
                var isMissingType = TryGetMissingType(property, assetPath, out var missingType);
                if (isMissingType)
                {
                    selectedTypeName = $"MISSING TYPE: {missingType.className}";
                    tooltipText = missingType.GetDetailData();
                    selectTypeButton.AddToClassList("error-bg");
                }
                else
                {
                    selectTypeButton.RemoveFromClassList("error-bg");
                }

                selectTypeButton.text = selectedTypeName;
                selectTypeButton.tooltip = tooltipText;
                selectTypeButton.style.color = defaultSelectTypeTextColor;

                openSourceFIleButton.SetDisplayElement(property.managedReferenceValue != null);

                var activeSearchTool = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableSearchTool;
                showSearchToolButton.SetDisplayElement(selectedType != null && activeSearchTool);
                fixCrossRefButton.SetDisplayElement(false);
                modifyDirectType.SetDisplayElement(isMissingType || canModifyDirectType);

                if (IsHaveSameOtherSerializeReference(property, out var isNewElement))
                {
                    fixCrossRefButton.SetDisplayElement(true);
                    var color = GetColorForEqualSerializeReference(property);
                    selectTypeButton.style.color = color;
                    if (isNewElement)
                    {
                        //https://github.com/AlexeyTaranov/SerializeReferenceDropdown/issues/49
                        //HACK: For correct refresh duplicate references need to call some refresh inspector
                        RefreshInspector();

                        void RefreshInspector()
                        {
                            var savedValue = property.managedReferenceValue;
                            property.managedReferenceValue = null;
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            property.serializedObject.Update();
                            property.managedReferenceValue = savedValue;
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            property.serializedObject.Update();
                            DropCaches();
                        }
                    }
                }
            }

            void ModifyDirectType()
            {
                var prop = property.serializedObject.FindProperty(propertyPath);
                if (TryGetMissingType(property, assetPath, out var missingType))
                {
                    var typeData = new TypeData()
                    {
                        AssemblyName = missingType.assemblyName,
                        ClassName = missingType.className,
                        Namespace = missingType.namespaceName
                    };
                    ShowModifyWindow(typeData, missingType.referenceId);
                }
                else if (prop.managedReferenceValue != null)
                {
                    var type = prop.managedReferenceValue.GetType();
                    var typeData = new TypeData()
                    {
                        AssemblyName = type.Assembly.GetName().Name,
                        ClassName = type.Name,
                        Namespace = type.Namespace
                    };
                    ShowModifyWindow(typeData, property.managedReferenceId);
                }

                void ShowModifyWindow(TypeData typeData, long refId)
                {
                    EditReferenceTypeWindow.ShowWindow(typeData,
                        newData =>
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(targetObject, out var guid,
                                out long localId);
                            if (YamlEditUnityObject.TryModifyReferenceInFile(assetPath, localId, refId, newData))
                            {
                                AssetDatabase.ImportAsset(assetPath);
                                DropCaches();
                            }
                        });
                }
            }
        }
    }
}