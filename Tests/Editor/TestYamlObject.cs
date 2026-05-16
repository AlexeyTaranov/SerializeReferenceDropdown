using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public interface ITestYaml
    {
    }

    [System.Serializable]
    public class TestYamlA : ITestYaml
    {
        public int Value;
    }

    [System.Serializable]
    public class GenericYamlData<T> : ITestYaml
    {
        public T Value;
    }

    [CreateAssetMenu(menuName = "SRD/TestYamlObject")]
    public class TestYamlObject : ScriptableObject
    {
        [SerializeReference, SerializeReferenceDropdown] public ITestYaml Reference;
    }
}
