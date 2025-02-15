using System;
using UnityEngine;

#if UNITY_2022_3_OR_NEWER
[Serializable]
public class RefTo<TRefType, THostType>
    where TRefType : class
    where THostType : UnityEngine.Object
{
    [SerializeField] private THostType _host;
    [SerializeField] private long _referenceId;

    public THostType Host => _host;

    public TRefType Get()
    {
        if (_host != null)
        {
            var value = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(_host, _referenceId);
            return value as TRefType;
        }

        return null;
    }
}
#endif