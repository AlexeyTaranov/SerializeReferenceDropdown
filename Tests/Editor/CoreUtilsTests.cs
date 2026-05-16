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
        public void ExtractTypeFromString_GenericString_ReturnsConstructedType()
        {
            var typeString = $"{typeof(GenericData<>).Assembly.GetName().Name} {typeof(GenericData<>).FullName}[[System.Int32, mscorlib]]";
            var type = TypeUtils.ExtractTypeFromString(typeString);
            Assert.AreEqual(typeof(GenericData<int>), type);
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
        public void GetAssignableSerializeReferenceTypes_GenericInterface_FindsOpenGenericType()
        {
            var types = TypeUtils.GetAssignableSerializeReferenceTypes(typeof(IGenericTarget<int>));

            Assert.Contains(typeof(DirectGeneric<>), types);
        }

        [Test]
        public void GetAssignableSerializeReferenceTypes_GenericBaseClass_FindsOpenGenericChildType()
        {
            var types = TypeUtils.GetAssignableSerializeReferenceTypes(typeof(GenericBase<int>));

            Assert.Contains(typeof(GenericBaseChild<>), types);
        }

        [Test]
        public void GetConcreteGenericType_DirectInterfaceMapping_ReturnsConstructedType()
        {
            var type = TypeUtils.GetConcreteGenericType(typeof(IGenericTarget<int>), typeof(DirectGeneric<>));

            Assert.AreEqual(typeof(DirectGeneric<int>), type);
        }

        [Test]
        public void GetConcreteGenericType_ReorderedInterfaceMapping_ReturnsConstructedType()
        {
            var type = TypeUtils.GetConcreteGenericType(typeof(IReorderedTarget<int, string>), typeof(ReorderedGeneric<,>));

            Assert.AreEqual(typeof(ReorderedGeneric<string, int>), type);
        }

        [Test]
        public void GetConcreteGenericType_PartialInterfaceMapping_ReturnsNull()
        {
            var type = TypeUtils.GetConcreteGenericType(typeof(IGenericTarget<int>), typeof(PartialGeneric<,>));

            Assert.IsNull(type);
        }

        [Test]
        public void TryGetGenericArgumentsFromTargetType_PartialInterfaceMapping_ReturnsMappedArguments()
        {
            var isMapped = TypeUtils.TryGetGenericArgumentsFromTargetType(typeof(IGenericTarget<int>), typeof(PartialGeneric<,>),
                out var genericArguments);

            Assert.IsTrue(isMapped);
            Assert.IsNull(genericArguments[0]);
            Assert.AreEqual(typeof(int), genericArguments[1]);
        }

        [Test]
        public void GetConcreteGenericType_BaseClassMapping_ReturnsConstructedType()
        {
            var type = TypeUtils.GetConcreteGenericType(typeof(GenericBase<int>), typeof(GenericBaseChild<>));

            Assert.AreEqual(typeof(GenericBaseChild<int>), type);
        }

        [Test]
        public void PrettifyTypeName_RemovesNamespaces()
        {
            var prettyName = PropertyDrawerTypesUtils.GetTypeName(typeof(UnityEngine.UIElements.Button));
            Assert.AreEqual("Button", prettyName);
        }

        [Test]
        public void PrettifyTypeName_GenericType_ShowsGenericArguments()
        {
            var prettyName = PropertyDrawerTypesUtils.GetTypeName(typeof(GenericData<int>));
            Assert.AreEqual("Generic Data<int>", prettyName);
        }

        private class TestScriptableObject : ScriptableObject
        {
            [SerializeReference] public ITestInterface Reference;
        }

        private interface ITestInterface { }
        private abstract class AbstractClass : ITestInterface { }
        private class ConcreteClass : ITestInterface { }
        private class GenericData<T> { }
        private interface IGenericTarget<T> { }
        private interface IReorderedTarget<TFirst, TSecond> { }
        private class DirectGeneric<T> : IGenericTarget<T> { }
        private class ReorderedGeneric<TFirst, TSecond> : IReorderedTarget<TSecond, TFirst> { }
        private class PartialGeneric<TFree, TTarget> : IGenericTarget<TTarget> { }
        private class GenericBase<T> { }
        private class GenericBaseChild<T> : GenericBase<T> { }
    }
}
