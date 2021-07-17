# SRD
### (Unity) Editor dropdown for [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html "SerializeReference") Attribute. 

Show all implemented types for property with this attribute. 

```csharp

public interface IShape
{
}

[Serializable]
public class Circle : IShape
{
    [SerializeField]
    private float _radius;
}

[Serializable]
public class Rectangle : IShape
{
    [SerializeField]
    private float _sideA;
        
    [SerializeField]
    private float _sideB;
}

public class TestShapesForSRD : MonoBehaviour
{
    [SRD]
    [SerializeReference]
    private IShape _singleShape;
    
    [SRD]
    [SerializeReference]
    private IShape[] _shapesArray;
}

```
[![](https://github.com/AlexeyTaranov/SRD/blob/master/Documentation~/SRD_Sample.gif "Sample gif")](https://github.com/AlexeyTaranov/SRD/blob/master/Documentation~/SRD_Sample.gif "Sample gif")
