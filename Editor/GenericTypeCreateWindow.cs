using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor
{
    public class GenericTypeCreateWindow : EditorWindow
    {
        private const int InvalidIndex = -1;

        private SerializedProperty serializedProperty;
        private Type inputGenericType;
        private Action<Type> onSelectNewGenericType;

        private IReadOnlyList<Type> specifiedTypesFromProperty;

        private List<IReadOnlyList<Type>> typesForParameters;
        private List<IReadOnlyList<string>> typeNamesForParameters;
        private int[] selectedIndexes;

        private IReadOnlyList<Button> parameterTypeButtons;
        private IReadOnlyList<Toggle> makeArrayTypeToggles;
        private Button generateGenericTypeButton;

        public static void ShowCreateTypeMenu(SerializedProperty property, Rect propertyRect, Type genericType,
            Action<Type> onSelectedConcreteType)
        {
            var window = GetWindow<GenericTypeCreateWindow>();
            window.titleContent = new GUIContent("SRD Generic Type Create");
            window.Show();
            window.InitWindow(property, genericType, onSelectedConcreteType);
            window.CreateElements();

            var currentRect = window.position;
            var windowPos = GUIUtility.GUIToScreenPoint(propertyRect.position);
            currentRect.position = windowPos;
            window.position = currentRect;
        }

        private void InitWindow(SerializedProperty property, Type genericType, Action<Type> onSelectedConcreteType)
        {
            inputGenericType = genericType;
            onSelectNewGenericType = onSelectedConcreteType;
            serializedProperty = property;

            var genericParams = inputGenericType.GetGenericArguments();
            selectedIndexes = new int[genericParams.Length];
            FillTypesAndNames();
            FillSpecifiedTypesFromProperty();
        }

        private string GetTypeName(Type type) => type.FullName;

        private void FillTypesAndNames()
        {
            var genericParams = inputGenericType.GetGenericArguments();
            typesForParameters = new List<IReadOnlyList<Type>>();
            typeNamesForParameters = new List<IReadOnlyList<string>>();
            for (int i = 0; i < selectedIndexes.Length; i++)
            {
                selectedIndexes[i] = InvalidIndex;
                var genericParam = genericParams[i];
                IReadOnlyList<Type> targetTypes;
                var systemObjectTypes = TypeUtils.GetAllSystemObjectTypes();
                if (genericParam.GetInterfaces().Length == 0)
                {
                    targetTypes = systemObjectTypes;
                }
                else
                {
                    var typesCollection = TypeCache.GetTypesDerivedFrom(genericParam);
                    targetTypes = typesCollection.Where(t => systemObjectTypes.Contains(t)).ToArray();
                }

                typesForParameters.Add(targetTypes);
                var names = targetTypes.Select(GetTypeName).ToArray();
                typeNamesForParameters.Add(names);
            }
        }

        private void FillSpecifiedTypesFromProperty()
        {
            var propertyType = TypeUtils.ExtractTypeFromString(serializedProperty.managedReferenceFieldTypename);
            if (propertyType.IsGenericType == false || propertyType.IsInterface == false)
            {
                return;
            }

            var genericInterfaces = inputGenericType.GetInterfaces();
            var genericInterfaceIndex = Array.FindIndex(genericInterfaces, IsSameGenericInterface);
            var genericInterface = genericInterfaces[genericInterfaceIndex];
            var genericInterfaceArgs = genericInterface.GetGenericArguments();

            var propertyGenericArgs = propertyType.GetGenericArguments();
            var genericTypeArgs = inputGenericType.GetGenericArguments();
            var specifiedTypes = new Type[genericTypeArgs.Length];
            for (int i = 0; i < genericInterfaceArgs.Length; i++)
            {
                var genericArg = genericInterfaceArgs[i];
                var specifiedType = propertyGenericArgs[i];
                var genericArgIndex = Array.FindIndex(genericTypeArgs, Match);
                specifiedTypes[genericArgIndex] = specifiedType;

                bool Match(Type type)
                {
                    return type.Name == genericArg.Name;
                }
            }

            specifiedTypesFromProperty = specifiedTypes;

            bool IsSameGenericInterface(Type type)
            {
                return type.IsGenericType && propertyType.GetGenericTypeDefinition() == type.GetGenericTypeDefinition();
            }
        }


        private void OnGUI()
        {
            if (IsDisposedProperty())
            {
                Close();
            }

            bool IsDisposedProperty()
            {
                if (serializedProperty == null)
                {
                    return true;
                }

                var propertyType = serializedProperty.GetType();
                var isValidField = propertyType.GetProperty("isValid", BindingFlags.NonPublic | BindingFlags.Instance);
                var isValidValue = isValidField?.GetValue(serializedProperty);
                return isValidValue != null && (bool)isValidValue == false;
            }
        }


        private void CreateElements()
        {
            rootVisualElement.Clear();
            CreateParameterButtons();
            CreateGenerateGenericTypeButton();
        }

        private void CreateParameterButtons()
        {
            var parameterButtons = new List<Button>();
            var arrayToggles = new List<Toggle>();
            parameterTypeButtons = parameterButtons;
            makeArrayTypeToggles = arrayToggles;

            var genericParams = inputGenericType.GetGenericArguments();
            for (int i = 0; i < genericParams.Length; i++)
            {
                var index = i;
                var currentParam = genericParams[i];
                var paramName = $"[{i}] {currentParam.Name}";
                var button = new Button();

                button.clickable.clicked += () => ShowTypesForParamIndex(index, button);

                var parameterTypeLabel = new TextElement();
                parameterTypeLabel.text = paramName;

                var makeArrayToggle = new Toggle("Make Array Type");

                var group = new Box();
                group.style.flexDirection = FlexDirection.Row;
                group.style.alignItems = Align.Center;
                group.Add(parameterTypeLabel);
                group.Add(button);
                group.Add(makeArrayToggle);

                parameterButtons.Add(button);
                arrayToggles.Add(makeArrayToggle);

                rootVisualElement.Add(group);

                RefreshGenericParameterButton(i);
            }
        }

        private void CreateGenerateGenericTypeButton()
        {
            generateGenericTypeButton = new Button();
            generateGenericTypeButton.text = "Generate";
            generateGenericTypeButton.clickable.clicked += GenerateGenericType;
            rootVisualElement.Add(generateGenericTypeButton);

            RefreshGenerateGenericButton();
        }

        private void GenerateGenericType()
        {
            var parameterTypes = new Type[selectedIndexes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Type type;
                if (specifiedTypesFromProperty[i] != null)
                {
                    type = specifiedTypesFromProperty[i];
                }
                else
                {
                    var typeIndex = selectedIndexes[i];
                    type = typesForParameters[i][typeIndex];
                    if (type.IsArray == false && makeArrayTypeToggles[i].value)
                    {
                        type = type.MakeArrayType();
                    }
                }

                parameterTypes[i] = type;
            }

            var newGenericType = inputGenericType.MakeGenericType(parameterTypes);
            try
            {
                onSelectNewGenericType.Invoke(newGenericType);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Close();
        }

        private void ShowTypesForParamIndex(int genericParamIndex, Button selectedButton)
        {
            var currentTypeNames = typeNamesForParameters[genericParamIndex];

            var dropdown = new AdvancedDropdown(new AdvancedDropdownState(), currentTypeNames,
                ApplySelectedTypeIndex);

            var buttonRect = new Rect(selectedButton.transform.position, selectedButton.transform.scale);
            dropdown.Show(buttonRect);

            void ApplySelectedTypeIndex(int selectedTypeIndex)
            {
                selectedIndexes[genericParamIndex] = selectedTypeIndex;
                RefreshGenericParameterButton(genericParamIndex);
                RefreshGenerateGenericButton();
            }
        }

        private void RefreshGenericParameterButton(int parameterIndex)
        {
            var button = parameterTypeButtons[parameterIndex];
            var specifiedType = specifiedTypesFromProperty[parameterIndex];
            if (specifiedType != null)
            {
                button.text = GetTypeName(specifiedType);
                button.SetEnabled(false);
                makeArrayTypeToggles[parameterIndex].SetEnabled(false);
                return;
            }

            var selectedIndex = selectedIndexes[parameterIndex];
            var buttonText = selectedIndex == InvalidIndex
                ? "Select Type"
                : typeNamesForParameters[parameterIndex][selectedIndex];
            button.text = buttonText;
        }


        private void RefreshGenerateGenericButton()
        {
            var specifiedTypesCount = specifiedTypesFromProperty.Count(t => t != null);
            var selectedIndexesCount = selectedIndexes.Count(t => t != InvalidIndex);
            var isSelectedAllTypes = (specifiedTypesCount + selectedIndexesCount) == selectedIndexes.Length;
            generateGenericTypeButton.SetEnabled(isSelectedAllTypes);
        }
    }
}