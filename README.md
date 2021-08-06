
# SRD
## (Unity) Editor dropdown for [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html "SerializeReference") Attribute. 

Show all and apply implemented types for property with this attribute. 

### Installation:
1. Select in UPM "Add package from git URL..."
2. Install package with link.
```
https://github.com/AlexeyTaranov/SRD.git
```

### Sample:
```csharp
public class TestShapesForSRD : MonoBehaviour
{
    [SRD]
    [SerializeReference]
    private IShape _singleShape;
    
    [SRD]
    [SerializeReference]
    private IShape[] _shapesArray;
}

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
```
[![](https://github.com/AlexeyTaranov/SRD/blob/master/Documentation~/SRD_Sample.gif "Sample gif")](https://github.com/AlexeyTaranov/SRD/blob/master/Documentation~/SRD_Sample.gif "Sample gif")
