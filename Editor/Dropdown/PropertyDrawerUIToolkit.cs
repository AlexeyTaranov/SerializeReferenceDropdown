using System;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    public class PropertyDrawerUIToolkit
    {
        private readonly SerializedProperty property;
        private readonly VisualElement root;
        private readonly IReadOnlyList<Type> assignableTypes;
        private readonly SerializeReferenceDropdownAttribute srdAttribute;
        private readonly string assetPath;

        private Button selectTypeButton;
        private Button openSourceFIleButton;
        private Button showSearchToolButton;
        private Button fixCrossRefButton;
        private Button modifyDirectType;

        private StyleColor defaultSelectTypeTextColor;

        private static Dictionary<PropertyDrawerUIToolkit, SerializedProperty> _allPropertyDrawers =
            new Dictionary<PropertyDrawerUIToolkit, SerializedProperty>();

        private static Object pingObject;
        private static Object previousSelection;
        private static long pingRefId;

        private Action _pingSelf;

        private string propertyPath => property.propertyPath;
        private Object targetObject => property.serializedObject.targetObject;

        public PropertyDrawerUIToolkit(SerializedProperty property, IReadOnlyList<Type> assignableTypes,
            VisualElement root, SerializeReferenceDropdownAttribute srdAttribute)
        {
            this.property = property;
            this.assignableTypes = assignableTypes;
            this.root = root;
            this.srdAttribute = srdAttribute;
            assetPath = AssetDatabase.GetAssetPath(targetObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetObject);
            }

            AddSelfToAllProperties();
        }

        private void AddSelfToAllProperties()
        {
            using var pooled = ListPool<PropertyDrawerUIToolkit>.Get(out var destroyedPropertyDrawers);
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


        public static void PingSerializeReference(Object selectionObject, long refId)
        {
            previousSelection = Selection.activeObject;
            Selection.activeObject = selectionObject;
            pingObject = selectionObject;
            pingRefId = refId;
            PingAll();

            EditorApplication.delayCall += () =>
            {
                previousSelection = null;
                pingObject = null;
                pingRefId = -1;
            };
        }

        private static void PingAll()
        {
            foreach (var pair in _allPropertyDrawers)
            {
                if (pair.Value != null)
                {
                    pair.Key.PingSelf();
                }
            }
        }

        private void PingSelf()
        {
            _pingSelf.Invoke();
        }

        public void CreateUIToolkitLayout()
        {
            var treeAssetPath = Path.Combine(Paths.PackageLayouts, "SerializeReferenceDropdown.uxml");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treeAssetPath);
            root.Add(visualTreeAsset.Instantiate());
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);

            selectTypeButton = root.Q<Button>("type-select");
            selectTypeButton.clickable.clicked += () =>
            {
                var propertyRect = propertyField.contentRect;
                ShowDropdown(propertyRect);
            };
            defaultSelectTypeTextColor = selectTypeButton.style.color;

            fixCrossRefButton = root.Q<Button>("fix-cross-references");
            fixCrossRefButton.clickable.clicked += () => { PropertyDrawerCrossReferences.FixCrossReference(property); };

            openSourceFIleButton = root.Q<Button>("open-source-file");
            openSourceFIleButton.SetDisplayElement(false);
            openSourceFIleButton.clicked += () =>
            {
                PropertyDrawerTypesUtils.OpenSourceFile(property.managedReferenceValue.GetType());
            };

            modifyDirectType = root.Q<Button>("modify-direct-type");
            showSearchToolButton = root.Q<Button>("show-search-tool");
            modifyDirectType.clicked += ModifyDirectType;

            showSearchToolButton.SetDisplayElement(false);
            showSearchToolButton.clicked += () =>
            {
                var type = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
                PropertyDrawerTypesUtils.ShowSearchTool(type);
            };

            root.TrackSerializedObjectValue(property.serializedObject, RefreshDropdown);
            RefreshDropdown(property.serializedObject);

            var needPingOnFirstShowPropertyDrawer =
                previousSelection != pingObject &&
                previousSelection != null &&
                property.managedReferenceId == pingRefId;

            if (needPingOnFirstShowPropertyDrawer)
            {
                EditorApplication.delayCall += PingImpl;
            }

            _pingSelf = PingNow;
        }

        private void ShowDropdown(Rect propertyRect)
        {
            var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                assignableTypes,
                type =>
                {
                    PropertyDrawerTypesUtils.WriteNewInstanceByType(type, property, propertyRect,
                        registerUndo: true);
                });
            var buttonMatrix = selectTypeButton.worldTransform;
            var position = new Vector3(buttonMatrix.m03, buttonMatrix.m13, buttonMatrix.m23);
            var buttonRect = new Rect(position, selectTypeButton.contentRect.size);
            dropdown.Show(buttonRect);
        }

        private void RefreshDropdown(SerializedObject so)
        {
            if (so == null)
            {
                return;
            }

            var prop = so.FindProperty(propertyPath);
            var selectedType = TypeUtils.ExtractTypeFromString(prop.managedReferenceFullTypename);
            var selectedTypeName = PropertyDrawerTypesUtils.GetTypeName(selectedType);
            var tooltipText = $"Type Full Name: {selectedType?.FullName}";
            var isMissingType =
                PropertyDrawerTypesUtils.TryGetMissingType(property, assetPath, out var missingType);
            var nullIsError = srdAttribute.Flags.HasFlag(SRDFlags.NotNull) && selectedType == null;
            if (isMissingType)
            {
                selectedTypeName = $"MISSING TYPE: {missingType.className}";
                tooltipText = missingType.GetDetailData();
            }

            var needShowErrorStyle = isMissingType || nullIsError;
            if (needShowErrorStyle)
            {
                selectTypeButton.AddToClassList("error-bg");
            }
            else
            {
                selectTypeButton.RemoveFromClassList("error-bg");
            }

            selectTypeButton.text = selectedTypeName;
            selectTypeButton.tooltip = tooltipText;
            selectTypeButton.style.color = defaultSelectTypeTextColor;
            
            var canModifyDirectType = string.IsNullOrEmpty(assetPath) == false;
            modifyDirectType.SetDisplayElement(canModifyDirectType && selectedType != null);

            openSourceFIleButton.SetDisplayElement(property.managedReferenceValue != null);

            var activeSearchTool = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableSearchTool;
            showSearchToolButton.SetDisplayElement(selectedType != null && activeSearchTool);
            fixCrossRefButton.SetDisplayElement(false);

            if (PropertyDrawerCrossReferences.IsHaveSameOtherSerializeReference(property, out var isNewElement))
            {
                fixCrossRefButton.SetDisplayElement(true);
                var color = PropertyDrawerCrossReferences.GetColorForEqualSerializeReference(property);
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
                        PropertyDrawerGlobalCaches.DropCaches();
                    }
                }
            }
        }

        private void ModifyDirectType()
        {
            var prop = property.serializedObject.FindProperty(propertyPath);
            if (PropertyDrawerTypesUtils.TryGetMissingType(property, assetPath, out var missingType))
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
                            PropertyDrawerGlobalCaches.DropCaches();
                        }
                    });
            }
        }

        private void PingNow()
        {
            if (pingObject == property.serializedObject.targetObject &&
                property.managedReferenceId == pingRefId)
            {
                PingImpl();
            }
        }

        private void PingImpl()
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
    }
}