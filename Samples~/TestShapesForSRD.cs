using UnityEngine;

namespace SRD.Sample
{
    public class TestShapesForSRD : MonoBehaviour
    {
        [SRD] [SerializeReference] private IShape _singleShape;

        [SRD] [SerializeReference] private IShape[] _shapesArray;

        [SerializeField] private AnyType<IShape> _anyShape;
    }
}