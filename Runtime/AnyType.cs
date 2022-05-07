using System;
using UnityEngine;

[Serializable]
public sealed class AnyType<T> where T : class
{
    [SerializeField] private bool _isUnityObjectReference;
    [SerializeField] private UnityEngine.Object _unityObject;
    [SerializeReference,SerializeReferenceDropdown] private T _nativeObject;

    public T Get()
    {
        if (_isUnityObjectReference)
        {
            return _nativeObject;
        }
        if (_unityObject != null)
        {
            return _unityObject as T;
        }

        return null;
    }

    public void Set(T value)
    {
        if (value is UnityEngine.Object unityObject)
        {
            _isUnityObjectReference = true;
            _unityObject = unityObject;
        }
        else
        {
            _isUnityObjectReference = false;
            _nativeObject = value;
        }
    }
}
