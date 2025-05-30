using System;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class SOUtils
    {
        public static void RegisterUndo(SerializedProperty property, string label)
        {
            Undo.RecordObject(property.serializedObject.targetObject,
                $"{label}: {property.serializedObject.targetObject.name} - {property.propertyPath}");
        }
        
        public static void RegisterUndoMultiple(UnityEngine.Object[] objects, string label)
        {
            Undo.RecordObjects(objects, $"{label}");
        }
        
        public static bool TraverseSO(UnityEngine.Object unityObject, Func<SerializedProperty, bool> isCompleteFunc)
        {
            using var so = new SerializedObject(unityObject);
            using var iterator = so.GetIterator();
            iterator.NextVisible(true);
            return TraversePropertyImpl(iterator, isCompleteFunc);
        }

        private static bool TraversePropertyImpl(SerializedProperty property,
            Func<SerializedProperty, bool> isCompleteFunc)
        {
            using var iterator = property.Copy();

            if (CanIterate())
            {
                do
                {
                    if (iterator.propertyType == SerializedPropertyType.ManagedReference && property.isArray == false)
                    {
                        if (isCompleteFunc.Invoke(iterator))
                        {
                            return true;
                        }
                    }
                } while (CanIterate());
            }

            bool CanIterate() => iterator.Next(true) && !SerializedProperty.EqualContents(iterator, property);

            return false;
        }
    }
}