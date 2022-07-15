using UnityEngine;

namespace SRD.Sample
{
    public class TestShapesForSRD : MonoBehaviour
    {
        [SerializeReferenceDropdown] [SerializeReference] private IShape _singleShape;

        [SerializeReferenceDropdown] [SerializeReference] private IShape[] _shapesArray;
    }
}
