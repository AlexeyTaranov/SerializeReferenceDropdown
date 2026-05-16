using System;
using System.Collections.Generic;
using NUnit.Framework;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class SOUtilsTests
    {
        private TestScriptableObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _testObject = ScriptableObject.CreateInstance<TestScriptableObject>();
            _testObject.data = new TestClass();
            _testObject.dataList = new List<ITestInterface> { new TestClass(), new TestClass() };
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
        public void TraverseSO_VisitsAllManagedReferenceProperties()
        {
            var visitedPaths = new HashSet<string>();
            SOUtils.TraverseSO(_testObject, (property) =>
            {
                visitedPaths.Add(property.propertyPath);
                return false; // Continue traversal
            });

            Assert.IsTrue(visitedPaths.Contains("data"));
            Assert.IsTrue(visitedPaths.Contains("dataList.Array.data[0]"));
            Assert.IsTrue(visitedPaths.Contains("dataList.Array.data[1]"));
            Assert.AreEqual(3, visitedPaths.Count);
        }

        private class TestScriptableObject : ScriptableObject
        {
            [SerializeReference] public ITestInterface data;
            [SerializeReference] public List<ITestInterface> dataList;
        }

        private interface ITestInterface { }
        [Serializable]
        private class TestClass : ITestInterface { }
    }
}
