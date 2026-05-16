using UnityEngine;

[System.Flags]
public enum SRDFlags
{
    Default = 0,
    NotNull = 1 << 0
}


public class SerializeReferenceDropdownAttribute : PropertyAttribute
{
    public SRDFlags Flags;

    public SerializeReferenceDropdownAttribute(SRDFlags flags = SRDFlags.Default)
    {
        Flags = flags;
    }
}