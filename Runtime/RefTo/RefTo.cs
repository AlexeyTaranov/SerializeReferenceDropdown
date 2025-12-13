using System;
using UnityEngine;

[Serializable]
public class RefTo<TRefType, THostType>
    where TRefType : class
    where THostType : UnityEngine.Object
{
    [SerializeField] private THostType _host;
    [SerializeField] private long _referenceId;

    private WeakReference<TRefType> _cache;

    public THostType Host => _host;
    public long ReferenceId => _referenceId;

    public TRefType Get()
    {
        if (_host != null)
        {
#if !DISABLE_REFTO_CACHE
            if (_cache?.TryGetTarget(out var cache) == true)
            {
                return cache;
            }
#endif
            var value = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(_host, _referenceId);
            var castObject = value as TRefType;
#if !DISABLE_REFTO_CACHE
            _cache = new WeakReference<TRefType>(castObject);
#endif
            return value as TRefType;
        }

        return null;
    }

    internal RefTo(THostType host, long referenceId)
    {
        _host = host;
        _referenceId = referenceId;
    }

    public RefTo<TRefType, THostType> CopyWithNewHost(THostType host)
    {
        return new RefTo<TRefType, THostType>(host, _referenceId);
    }
}

//Two different generic types - https://github.com/AlexeyTaranov/SerializeReferenceDropdown/issues/55
[Serializable]
public sealed class RefTo<TRefType>
    where TRefType : class
{
    [SerializeField] private UnityEngine.Object _host;
    [SerializeField] private long _referenceId;

    private WeakReference<TRefType> _cache;

    public UnityEngine.Object Host => _host;
    public long ReferenceId => _referenceId;

    public TRefType Get()
    {
        if (_host != null)
        {
#if !DISABLE_REFTO_CACHE
            if (_cache?.TryGetTarget(out var cache) == true)
            {
                return cache;
            }
#endif
            var value = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(_host, _referenceId);
            var castObject = value as TRefType;
#if !DISABLE_REFTO_CACHE
            _cache = new WeakReference<TRefType>(castObject);
#endif
            return value as TRefType;
        }

        return null;
    }

    internal RefTo(UnityEngine.Object host, long referenceId)
    {
        _host = host;
        _referenceId = referenceId;
    }

    public RefTo<TRefType> CopyWithNewHost(UnityEngine.Object host)
    {
        return new RefTo<TRefType>(_host, _referenceId);
    }
}