using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor
{
    public class GenericTypeCreateWindow : EditorWindow
    {
        private const int InvalidIndex = -1;
        private Type inputGenericType;
        private SerializedProperty property;
        private Action<Type> onSelectNewGenericType;

        private IReadOnlyList<Type> genericParameters;
        private List<Type[]> types;
        private List<string[]> typeNames;
        private int[] selectedIndexes;

        private Button generateGenericButton;
        private IReadOnlyList<Button> typeButtons;

        private static Type[] SystemObjectTypes;

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
            this.property = property;

            genericParameters = inputGenericType.GetGenericArguments();
            selectedIndexes = new int[genericParameters.Count];
            types = new List<Type[]>();
            typeNames = new List<string[]>();
            for (int i = 0; i < selectedIndexes.Length; i++)
            {
                selectedIndexes[i] = InvalidIndex;
                var genericParam = genericParameters[i];
                Type[] targetTypes;
                if (genericParam.GetInterfaces().Length == 0)
                {
                    SystemObjectTypes ??= GetAllSystemObjectTypes();
                    targetTypes = SystemObjectTypes;
                }
                else
                {
                    var typesCollection = TypeCache.GetTypesDerivedFrom(genericParam);
                    targetTypes = typesCollection.Where(t => SystemObjectTypes.Contains(t)).ToArray();
                }


                types.Add(targetTypes);
                var names = targetTypes.Select(t => t.FullName).ToArray();
                typeNames.Add(names);
            }


            Type[] GetAllSystemObjectTypes()
            {
                var assemblies = CompilationPipeline.GetAssemblies();
                var playerAssemblies = assemblies.Where(t => t.flags.HasFlag(AssemblyFlags.EditorAssembly) == false)
                    .Select(t => t.name).ToArray();
                var baseType = typeof(object);
                var typesCollection = TypeCache.GetTypesDerivedFrom(baseType);
                var customTypes = typesCollection.Where(IsValidTypeForGenericParameter).OrderBy(t => t.FullName);
                
                var typesList = new List<Type>();
                typesList.AddRange(TypeUtils.GetBuiltInUnitySerializeTypes());
                typesList.AddRange(customTypes);
                return typesList.ToArray();


                bool IsValidTypeForGenericParameter(Type t)
                {
                    var isUnityObjectType = t.IsSubclassOf(typeof(UnityEngine.Object));

                    var isFinalSerializeType = !isUnityObjectType && !t.IsAbstract && !t.IsInterface &&
                                               !t.IsGenericType &&
                                               t.IsSerializable;
                    var isEnum = t.IsEnum;
                    
                    var isTargetType = playerAssemblies.Any(asm => t.Assembly.FullName.StartsWith(asm));
                    return isTargetType && (isFinalSerializeType || isEnum);
                }
            }
        }

        private void OnGUI()
        {
            if (IsDisposedProperty())
            {
                Close();
            }
        }

        private bool IsDisposedProperty()
        {
            if (property == null)
            {
                return true;
            }

            var propertyType = property.GetType();
            var isValidField = propertyType.GetProperty("isValid", BindingFlags.NonPublic | BindingFlags.Instance);
            var isValidValue = isValidField?.GetValue(property);
            return isValidValue != null && (bool)isValidValue == false;
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
            typeButtons = parameterButtons;

            for (int i = 0; i < genericParameters.Count; i++)
            {
                var index = i;
                var currentParam = genericParameters[i];
                var paramName = $"[{i}] {currentParam.Name}";
                var button = new Button();

                button.clickable.clicked += () => ShowTypesForParamIndex(index, button);

                var leftLabel = new TextElement();
                leftLabel.text = paramName;

                var group = new Box();
                group.style.flexDirection = FlexDirection.Row;
                group.style.alignItems = Align.Center;
                group.Add(leftLabel);
                group.Add(button);
                parameterButtons.Add(button);
                rootVisualElement.Add(group);
                RefreshGenericParameterButton(i);
            }
        }

        private void CreateGenerateGenericTypeButton()
        {
            generateGenericButton = new Button();
            generateGenericButton.text = "Generate";
            generateGenericButton.clickable.clicked += GenerateGenericType;
            rootVisualElement.Add(generateGenericButton);

            RefreshGenerateGenericButton();
        }

        private void GenerateGenericType()
        {
            var parameterTypes = new Type[selectedIndexes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var typeIndex = selectedIndexes[i];
                var type = types[i][typeIndex];
                parameterTypes[i] = type;
            }

            var newGenericType = inputGenericType.MakeGenericType(parameterTypes);
            onSelectNewGenericType.Invoke(newGenericType);
            Close();
        }

        private void ShowTypesForParamIndex(int genericParamIndex, Button selectedButton)
        {
            var currentTypeNames = typeNames[genericParamIndex];

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
            var button = typeButtons[parameterIndex];
            var selectedIndex = selectedIndexes[parameterIndex];
            var buttonText = selectedIndex == InvalidIndex ? "Select Type" : typeNames[parameterIndex][selectedIndex];
            button.text = buttonText;
        }


        private void RefreshGenerateGenericButton()
        {
            var anyInactiveType = selectedIndexes.Any(t => t == InvalidIndex);
            generateGenericButton.SetEnabled(!anyInactiveType);
        }
    }
}