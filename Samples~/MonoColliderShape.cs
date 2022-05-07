using UnityEngine;

namespace SRD.Sample
{
    public class MonoColliderShape : MonoBehaviour, IShape
    {
        [SerializeField] private ColliderShape _colliderShape;


        public float GetArea()
        {
            return _colliderShape.GetArea();
        }
    }
}