# SerializeReferenceDropdown

[![Unity 2019+](https://img.shields.io/badge/Unity-2023%2B-blue.svg)](#)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)


## Unity Editor dropdown for [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html "SerializeReference") Attribute.
### RefTo (Reference to SerializeReference).


### Features:
- [Select type for Serialize Reference](#select-type-for-serialize-reference)
- [Copy Paste context menu](#copypaste-context-menu)
- [Generic Serialize References](#generics)
- [Keep Data with new type](#keep-data-with-new-type)
- [Fix cross references](#fix-cross-references)
- [Open source file](#open-source-file)
- [Highlight Missing Types](#highlight-missing-types)
- [Modify Type Name YAML](#modify-type-name-yaml)
- [RefTo type: References to Serialize Reference](#refto-type-references-to-serialize-reference)
- [Search Tool](#search-tool) 
- [Rider Integration](#rider-integration)

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

## Select type for Serialize Reference

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

## Generics
#### Unity 2023.2+

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

## Keep Data with new type

https://github.com/user-attachments/assets/aa641d95-ae4a-4e6c-92a3-4ec6554833de

## Fix cross references

https://github.com/user-attachments/assets/196cc3fe-0866-490c-99eb-14108a57f50f

## Open source file

https://github.com/user-attachments/assets/e7a5fe26-6df1-4c03-8688-a07b8219c41d

## Highlight Missing Types
<img width="550" height="100" alt="image" src="https://github.com/user-attachments/assets/b94d17aa-9c36-498c-bf94-0a2e08f80686" />

WIP: ScriptableObject - OK, everything else - broken.

Bug - https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-129100

## Modify Type Name YAML
You can modify type inside file - avoid Unity API. Perfect for fix missing references.

https://github.com/user-attachments/assets/e0c60c15-8619-4d29-a5c2-551eecb3b67a

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

## Search Tool

https://github.com/user-attachments/assets/fbf5460c-5ef8-4fde-b888-7943ac12378e


##  Rider Integration

Rider plugin available here - https://github.com/AlexeyTaranov/SerializeReferenceDropdownIntegration

<img width="436" height="208" alt="528988697-e26e63d6-ef0b-433d-a4e1-e3c8077411d8" src="https://github.com/user-attachments/assets/c21de35c-8f6d-4224-9a5f-00b824037a94" />

