using System;
using UnityEngine;

#if UNITY_2023_2_OR_NEWER
[Serializable]
public sealed class RefTo<TRefType, THostType>
    where TRefType : class
    where THostType : UnityEngine.Object
{
    [SerializeField] private THostType _host;
    [SerializeField] private long _referenceId;

    private TRefType _cache;
    private bool _isCached;

    public THostType Host => _host;
    public long ReferenceId => _referenceId;

    public TRefType Get()
    {
        if (_host != null)
        {
#if!DISABLE_REFTO_CACHE
            if (_isCached)
            {
                return _cache;
            }
#endif
            var value = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(_host, _referenceId);
            var castObject = value as TRefType;
            _cache = castObject;
            _isCached = true;
            return value as TRefType;
        }

        return null;
    }

    public RefTo(THostType host, long referenceId)
    {
        _host = host;
        _referenceId = referenceId;
    }

    public RefTo<TRefType, THostType> CopyWithNewHost(THostType host)
    {
        return new RefTo<TRefType, THostType>(host, _referenceId);
    }
}
#endif