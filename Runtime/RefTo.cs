using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2022_3_OR_NEWER
[Serializable]
public class RefTo<TRefType> where TRefType : class
{
    [SerializeField] private Object _host;
    [SerializeField] private long _referenceId;

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
