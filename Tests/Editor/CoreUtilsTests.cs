using NUnit.Framework;
using SerializeReferenceDropdown.Editor.Dropdown;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class CoreUtilsTests
    {
        [Test]
        public void ExtractTypeFromString_ValidString_ReturnsType()
        {
            var typeString = "UnityEngine.CoreModule UnityEngine.GameObject";
            var type = TypeUtils.ExtractTypeFromString(typeString);
            Assert.AreEqual(typeof(GameObject), type);
        }

        [Test]
        public void ExtractTypeFromString_InvalidString_ReturnsNull()
        {
            var typeString = "InvalidAssembly InvalidType";
            var type = TypeUtils.ExtractTypeFromString(typeString);
            Assert.IsNull(type);
        }
        
        [Test]
        public void IsFinalAssignableType_AbstractClass_ReturnsFalse()
        {
            Assert.IsFalse(TypeUtils.IsFinalAssignableType(typeof(AbstractClass)));
        }

        [Test]
        public void IsFinalAssignableType_Interface_ReturnsFalse()
        {
            Assert.IsFalse(TypeUtils.IsFinalAssignableType(typeof(ITestInterface)));
        }

        [Test]
        public void IsFinalAssignableType_ConcreteClass_ReturnsTrue()
        {
            Assert.IsTrue(TypeUtils.IsFinalAssignableType(typeof(ConcreteClass)));
        }

        [Test]
        public void GetAssignableSerializeReferenceTypes_FindsDerivedTypes()
        {
            var types = TypeUtils.GetAssignableSerializeReferenceTypes(typeof(ITestInterface));
            Assert.Contains(typeof(ConcreteClass), types);
            Assert.Contains(null, types);
        }

        [Test]
        public void PrettifyTypeName_RemovesNamespaces()
        {
            var prettyName = PropertyDrawerTypesUtils.GetTypeName(typeof(UnityEngine.UIElements.Button));
            Assert.AreEqual("Button", prettyName);
        }

        private class TestScriptableObject : ScriptableObject
        {
            [SerializeReference] public ITestInterface Reference;
        }

        private interface ITestInterface { }
        private abstract class AbstractClass : ITestInterface { }
        private class ConcreteClass : ITestInterface { }
    }
}
