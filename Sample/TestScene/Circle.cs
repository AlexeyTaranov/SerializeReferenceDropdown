using System;
using UnityEngine;

[Serializable]
public class Circle : IShape
{
    [SerializeField]
    private float _radius;
        
    public float GetArea()
    {
        return _radius * _radius * (float)Math.PI;
    }
}