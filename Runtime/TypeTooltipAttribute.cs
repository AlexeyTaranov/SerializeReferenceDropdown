using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class TypeTooltipAttribute : TooltipAttribute
{
    public TypeTooltipAttribute(string tooltip) : base(tooltip)
    {
    }
}
