using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Test.TestScene
{
    public class TestShapesForSRD : MonoBehaviour
    {
        [SerializeReference] 
        private IShape _shape;
        
        [SerializeReference] 
        private List<IShape> _shapes;

        private void OnValidate()
        {
            LogAllShapesArea();
        }

        private void LogAllShapesArea()
        {
            string GetShapeAreaString(IShape shape)
            {
                return $"Area of {shape?.GetType().Name} : {shape?.GetArea()}";
            }
            
            Debug.Log(GetShapeAreaString(_shape));

            if (_shapes != null)
            {
                var shapesAreaSb = new StringBuilder();
                shapesAreaSb.AppendLine("Shapes area");
                _shapes.ForEach((shape => shapesAreaSb.AppendLine(GetShapeAreaString(shape))));
                Debug.Log(shapesAreaSb);
            }
        }
    }
}
