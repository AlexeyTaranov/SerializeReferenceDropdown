using System;
using UnityEngine;

namespace SRD.Sample
{
    [Serializable]
    public class Circle : IShape
    {
        [SerializeField] private float _radius;

        public float GetArea()
        {
            return _radius * _radius * (float)Math.PI;
        }
    }
}