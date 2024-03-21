using System;
using SRD.Sample.Generics;
using UnityEngine;

namespace SRD.Sample
{
    public class TestShapesForSRD : MonoBehaviour
    {
        [SerializeReference, SerializeReferenceDropdown]
        private IShape _singleShape;

        [SerializeReference, SerializeReferenceDropdown]
        private IShape[] _shapesArray;

        [SerializeReference, SerializeReferenceDropdown]
        private ISimpleGenericData<int> _intData;
        
        [SerializeReference, SerializeReferenceDropdown]
        private ISimpleGenericData<Data> _classData;
    }

    [Serializable]
    public class Data
    {
        public string Name;
        public int Index;
    }
}