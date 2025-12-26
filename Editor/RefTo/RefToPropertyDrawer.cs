using System;
using System.Linq;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.RefTo
{
    [CustomPropertyDrawer(typeof(RefTo<,>))]
    [CustomPropertyDrawer(typeof(RefTo<>))]
    public partial class RefToPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, false);
        }

        private bool UnityObjectIterator(Object newValue, Type hostType, Func<Object, bool> predicate)
        {
            var canUseComponents = hostType.IsAssignableFrom(typeof(MonoBehaviour));
            if (newValue is GameObject go && canUseComponents)
            {
                var components = go.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (predicate.Invoke(component))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (predicate.Invoke(newValue))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryApplyNewInstanceToDragObject(Type refToTargetType, Type hostType,
            SerializedProperty refToProperty,
            Object newValue,
            Rect dropdownRect, Rect propertyFieldRect)
        {
            var assignableTypes = TypeUtils.GetAssignableSerializeReferenceTypes(refToTargetType);
            if (assignableTypes.Any())
            {
                var foundFieldToAssign = UnityObjectIterator(newValue, hostType, o =>
                {
                    if (TryFindAvailableSerializedPropertyForRefTo(o, refToTargetType, out var modifyPropertyPath))
                    {
                        var dropdown = new SerializeReferenceAdvancedDropdown(new AdvancedDropdownState(),
                            assignableTypes,
                            type =>
                            {
                                using var so1 = new SerializedObject(o);
                                using var modifyProperty1 = so1.FindProperty(modifyPropertyPath);
                                SerializeReferencePropertyDrawer.WriteNewInstanceByType(type, modifyProperty1,
                                    propertyFieldRect, registerUndo: true);
                                EditorApplication.delayCall += () =>
                                {
                                    using var so2 = new SerializedObject(o);
                                    using var modifyProperty2 = so2.FindProperty(modifyPropertyPath);
                                    RefToExtensions.WriteRefToFromPropertyToProperty(modifyProperty2, refToProperty);
                                };
                            });


                        dropdown.Show(dropdownRect);
                        return true;
                    }

                    return false;
                });

                if (foundFieldToAssign)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryApplyRefToValue(SerializedProperty toProperty, Object newObject, Type refToTargetType)
        {
            return SOUtils.TraverseSO(newObject, TryWriteToRefTo);

            bool TryWriteToRefTo(SerializedProperty refProperty)
            {
                var refType = TypeUtils.ExtractTypeFromString(refProperty.managedReferenceFullTypename);
                if (refToTargetType.IsAssignableFrom(refType))
                {
                    RefToExtensions.WriteRefToFromPropertyToProperty(refProperty, toProperty);
                    return true;
                }

                return false;
            }
        }

        private bool TryFindAvailableSerializedPropertyForRefTo(Object newObject, Type refToTargetType,
            out string modifyPropertyPath)
        {
            string outPropertyPath = null;
            var result = SOUtils.TraverseSO(newObject, TryCheckIsAssignableToRefTo);
            modifyPropertyPath = outPropertyPath;
            return result;

            bool TryCheckIsAssignableToRefTo(SerializedProperty refProperty)
            {
                var refType = TypeUtils.ExtractTypeFromString(refProperty.managedReferenceFieldTypename);
                if (refType.IsAssignableFrom(refToTargetType))
                {
                    outPropertyPath = refProperty.propertyPath;
                    return true;
                }

                return false;
            }
        }
    }
}
