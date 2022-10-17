using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class SerializeReferenceDropdownNameAttribute : PropertyAttribute
{
    public readonly string Name;
    
    public SerializeReferenceDropdownNameAttribute(string name)
    {
        Name = name;
    }
}

