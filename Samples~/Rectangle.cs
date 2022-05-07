using System;
using UnityEngine;

namespace SRD.Sample
{
    [Serializable]
    public class Rectangle : IShape
    {
        [SerializeField] private float _sideA;

        [SerializeField] private float _sideB;

        public float GetArea()
        {
            return _sideA * _sideB;
        }
    }
}