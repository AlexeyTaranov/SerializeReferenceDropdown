using System;
using UnityEngine;

namespace SRD.Sample
{
    [Serializable]
    [SerializeReferenceDropdownName("Collider")]
    public class ColliderShape : IShape
    {
        [SerializeField] private Collider _collider;

        public float GetArea()
        {
            var size = _collider.bounds.size;
            return size.x * size.y * size.z;
        }
    }
}
