# SerializeReferenceDropdown

## (Unity) Editor dropdown for [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html "SerializeReference") Attribute. RefTo (Reference to SerializeReference).

### Features:
- Select type for Serialize Reference
- Copy/Paste context menu
- Generic Serialize References
- RefTo type: References to Serialize Reference
- Keep Data with new type
- Fix cross references (WIP)
- Open source file
- Search Tool (WIP)
- Rider Integration (WIP)

WIP Features - available in preferences.

### Requirements:

#### Minimal:
- Unity 2019+

#### Recommended: 
- Unity 2023+
- Default UI Toolkit Inpsector

<details>
<summary>Why?</summary>

Serialization: Objects referenced from SerializeReference fields
now have stable ids, which reduces risk of conflicts when multiple
users collaborate on a scene file. This also improves support for undo
and prefabs, especially when SerializeReference is used inside arrays
and lists. There is a new format for references, with backward
compatibility support for older assets.

Serialization: SerializeReference now allow more granular handling of
missing types. SerializeReference instances for which the type is
missing will be replaced by null. Other instances will be editable and
if fields who were previously referring to the missing type are still
null the missing type will be preserved.
 </details>

### Installation:

1. Select in UPM "Add package from git URL..."
2. Install package with link.

```
https://github.com/AlexeyTaranov/SerializeReferenceDropdown.git
```

## Select type for Serialize Reference:

https://github.com/user-attachments/assets/43a6446d-1b4c-48d4-ab4b-de53dd3ba6ab

<details>
<summary>Code Example</summary>

```csharp
public class TestShapesForSRD : MonoBehaviour
{
    [SerializeReferenceDropdown]
    [SerializeReference]
    private IShape _singleShape;
    
    [SerializeReferenceDropdown]
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

 </details>

## Copy/Paste context menu

https://github.com/user-attachments/assets/96cb3d58-b9c1-4874-8048-fc55442b4446

## Generics (Unity 2023.2+)

You can use generics . Unspecified arguments need select in additional window.

![Generics](https://github.com/user-attachments/assets/2d4fa6ff-446d-472b-a570-230226bddbee)

<details>
<summary>Code Example</summary>

```csharp
public class TestShapesForSRD : MonoBehaviour
{
    [SerializeReference, SerializeReferenceDropdown]
    private ISimpleGenericData<int> _intData;
}

public interface ISimpleGenericData<TData> : IAbstractData
{
    public TData Data { get; }
}

[Serializable]
public class GenericData<TData> : ISimpleGenericData<TData>
{
    [SerializeField] private TData _data;

    public TData Data => _data;
}

[Serializable]
public class GenericKeyValuePair<TKeyData, TValueData> : ISimpleGenericData<TKeyData>, IAbstractData
{
    [SerializeField] private TKeyData _key;
    [SerializeField] private TValueData _value;
    public TKeyData Data => _key;
}
```

</details>

## RefTo type: References to Serialize Reference

This class support references to Serialize References!
You can assign RefTo with Drag and Drop or with context menu.

Inspector will help avoid not to assign wrong object types, runtime type checks - "as operator" cast.

https://github.com/user-attachments/assets/10aceba6-89c5-4582-8038-315307d0be6c

```csharp
[SerializeField] private RefTo<IShape,MonoBehaviour> _refShape;

public void Execute()
{
    var sampleString = _refShape.Get()?.ToString();
}    
```
## Keep Data with new type

https://github.com/user-attachments/assets/aa641d95-ae4a-4e6c-92a3-4ec6554833de

## Fix cross references (WIP)

https://github.com/user-attachments/assets/196cc3fe-0866-490c-99eb-14108a57f50f

## Open source file

https://github.com/user-attachments/assets/e7a5fe26-6df1-4c03-8688-a07b8219c41d

## Search Tool (WIP)

https://github.com/user-attachments/assets/fbf5460c-5ef8-4fde-b888-7943ac12378e


##  Rider Integration (WIP)

Rider plugin available here - https://github.com/AlexeyTaranov/SerializeReferenceDropdownIntegration

https://github.com/user-attachments/assets/c50cb516-50c1-4663-ab71-b3fd3ad6ce2d
