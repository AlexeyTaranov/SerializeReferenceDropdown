using System;
using System.Text;
using UnityEngine;

public class TestShapesForSRD : MonoBehaviour
{
    
    [SRD]
    [SerializeReference]
    private IShape _singleShape;
    
    [SRD]
    [SerializeReference]
    private IShape[] _shapesArray;
    
    [SRD]
    [SerializeField]
    private int _nonSRDAttribute;
    
    [SRD]
    [SerializeReference]
    private int _refToInt;

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
            
        Debug.Log(GetShapeAreaString(_singleShape));
        
        if (_shapesArray != null)
        {
            var shapesAreaSb = new StringBuilder();
            shapesAreaSb.AppendLine("Shapes area");
            Array.ForEach(_shapesArray,shape => shapesAreaSb.AppendLine(GetShapeAreaString(shape)));
            Debug.Log(shapesAreaSb);
        }
    }
}
