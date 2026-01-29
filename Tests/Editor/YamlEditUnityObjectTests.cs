using System.IO;
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
            long fileId = GetFileId(_testObject);
            long rid = GetRid(_testObject, nameof(TestYamlObject.Reference));

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
            long fileId = GetFileId(_testObject);
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

        [Test]
        public void TryModifyReferenceInFile_NullAssetPath_ReturnsFalse()
        {
            // Arrange
            var newValue = new TypeData { ClassName = "New", Namespace = "New", AssemblyName = "New" };

            // Act
            bool result = YamlEditUnityObject.TryModifyReferenceInFile(null, 0, 0, newValue);

            // Assert
            Assert.IsFalse(result);
        }

        private long GetFileId(Object obj)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long fileId);
            return fileId;
        }

        private long GetRid(Object obj, string propertyPath)
        {
            var so = new SerializedObject(obj);
            var sp = so.FindProperty(propertyPath);
            return sp.managedReferenceId;
        }
    }
}