using System;
using UnityEngine;

namespace SRD.Sample.Generics
{
    public interface ISimpleGenericData<TData>
    {
        public TData Data { get; }
    }

    [Serializable]
    public class GenericData<TData> : ISimpleGenericData<TData>
    {
        [SerializeField] private TData _data;

        public TData Data => _data;
    }
}