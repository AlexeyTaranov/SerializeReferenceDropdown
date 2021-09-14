

# SRD
## (Unity) Editor dropdown for [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html "SerializeReference") Attribute. 

Show all and apply implemented types for property with this attribute. 

 ### Requirements:

  [Unity 2021.2.0a19](https://unity3d.com/ru/unity/alpha/2021.2.0a19) and above.


>  Serialization: Objects referenced from SerializeReference fields
> now have stable ids, which reduces risk of conflicts when multiple
> users collaborate on a scene file. This also improves support for undo
> and prefabs, especially when SerializeReference is used inside arrays
> and lists. There is a new format for references, with backward
> compatibility support for older assets.
> 
> Serialization: SerializeReference now allow more granular handling of
> missing types. SerializeReference instances for which the type is
> missing will be replaced by null. Other instances will be editable and
> if fields who were previously referring to the missing type are still
> null the missing type will be preserved. 

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
