using System;
using NUnit.Framework;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class ReflectionUtilsTests
    {
        private TestScriptableObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _testObject = ScriptableObject.CreateInstance<TestScriptableObject>();
            _testObject.simpleValue = 42;
            _testObject.nested = new NestedClass { nestedValue = 100 };
            _testObject.arrayValue = new int[] { 1, 2, 3 };
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void GetTarget_SimpleProperty_ReturnsValue()
        {
            var so = new SerializedObject(_testObject);
            var property = so.FindProperty("simpleValue");
            var target = property.GetTarget();
            Assert.AreEqual(42, target);
        }

        [Test]
        public void GetTarget_NestedProperty_ReturnsValue()
        {
            var so = new SerializedObject(_testObject);
            var property = so.FindProperty("nested.nestedValue");
            var target = property.GetTarget();
            Assert.AreEqual(100, target);
        }

        [Test]
        public void GetTarget_ArrayElement_ReturnsValue()
        {
            var so = new SerializedObject(_testObject);
            var property = so.FindProperty("arrayValue").GetArrayElementAtIndex(1);
            var target = property.GetTarget();
            Assert.AreEqual(2, target);
        }

        private class TestScriptableObject : ScriptableObject
        {
            public int simpleValue;
            public NestedClass nested;
            public int[] arrayValue;
        }

        [Serializable]
        private class NestedClass
        {
            public int nestedValue;
        }
    }
}
