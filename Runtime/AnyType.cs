using System;
using UnityEngine;

[Serializable]
public sealed class AnyType<T> where T : class
{
    [SerializeField] private bool isUnityObjectReference;
    [SerializeField] private UnityEngine.Object unityObject;
    [SerializeReference,SerializeReferenceDropdown] private T nativeObject;

    public T Get()
    {
        if (isUnityObjectReference)
        {
            return nativeObject;
        }
        if (unityObject != null)
        {
            return unityObject as T;
        }

        return null;
    }

    public void Set(T value)
    {
        if (value is UnityEngine.Object unityObjectCast)
        {
            isUnityObjectReference = true;
            this.unityObject = unityObjectCast;
        }
        else
        {
            isUnityObjectReference = false;
            nativeObject = value;
        }
    }
}
