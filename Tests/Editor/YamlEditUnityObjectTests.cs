using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SerializeReferenceDropdown.Editor.YAMLEdit;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class YamlEditUnityObjectTests
    {
        private static readonly string TestFolderParent = Path.Combine("Packages", "com.alexeytaranov.serializereferencedropdown", "Tests", "Editor");
        private static readonly string TestFolderName = "TestData";
        private static readonly string TestDataPath = Path.Combine(TestFolderParent, TestFolderName);

        private string _testAssetPath;
        private TestYamlObject _testObject;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestDataPath))
            {
                AssetDatabase.CreateFolder(TestFolderParent, TestFolderName);
            }

            _testAssetPath = Path.Combine(TestDataPath, "TestYamlAsset.asset");
            _testObject = ScriptableObject.CreateInstance<TestYamlObject>();
            _testObject.Reference = new TestYamlA { Value = 10 };
            AssetDatabase.CreateAsset(_testObject, _testAssetPath);
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testAssetPath))
            {
                AssetDatabase.DeleteAsset(_testAssetPath);
            }

            if (AssetDatabase.IsValidFolder(TestDataPath))
            {
                AssetDatabase.DeleteAsset(TestDataPath);
            }

            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void TryModifyReferenceInFile_ExistingRid_ModifiesType()
        {
            // Arrange
            long fileId = 0;
            string guid;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_testObject, out guid, out fileId))
            {
                Assert.Fail("Failed to get fileId for test object");
            }

            var yamlContent = File.ReadAllText(_testAssetPath);
            var ridMatch = Regex.Match(yamlContent, @"rid: (\d+)");
            if (!ridMatch.Success)
            {
                Assert.Fail("Failed to find rid in YAML");
            }

            long rid = long.Parse(ridMatch.Groups[1].Value);

            var newValue = new TypeData
            {
                ClassName = "NewClass",
                Namespace = "NewNamespace",
                AssemblyName = "NewAssembly"
            };

            // Act
            bool result = YamlEditUnityObject.TryModifyReferenceInFile(_testAssetPath, fileId, rid, newValue);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(_testAssetPath);
            Assert.IsTrue(modifiedContent.Contains("class: NewClass, ns: NewNamespace, asm: NewAssembly"));
            Assert.IsTrue(modifiedContent.Contains($"rid: {rid}"));
        }

        [Test]
        public void TryModifyReferenceInFile_NonExistingRid_ReturnsFalse()
        {
            // Arrange
            long fileId = 0;
            string guid;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_testObject, out guid, out fileId);

            var newValue = new TypeData { ClassName = "New", Namespace = "New", AssemblyName = "New" };

            // Act
            bool result = YamlEditUnityObject.TryModifyReferenceInFile(_testAssetPath, fileId, 00000, newValue);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TryModifyReferenceInFile_WrongFileId_ReturnsFalse()
        {
            // Arrange
            var newValue = new TypeData { ClassName = "New", Namespace = "New", AssemblyName = "New" };

            // Act
            bool result = YamlEditUnityObject.TryModifyReferenceInFile(_testAssetPath, 99999, 98765, newValue);

            // Assert
            Assert.IsFalse(result);
        }
    }
}