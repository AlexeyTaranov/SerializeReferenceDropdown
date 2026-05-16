using NUnit.Framework;
using SerializeReferenceDropdown.Editor.Utils;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class MissingTypeUtilsTests
    {
        [Test]
        public void GetDetailData_FormatsStringCorrectly()
        {
            var assemblyName = "TestAssembly";
            var namespaceName = "TestNamespace";
            var className = "TestClass";
            long referenceId = 12345;
            var serializedData = "some: yaml";

            var detailData = MissingTypeUtils.FormatDetailData(assemblyName, namespaceName, className, referenceId, serializedData);

            Assert.That(detailData, Does.Contain("ASM: TestAssembly"));
            Assert.That(detailData, Does.Contain("Namespace: TestNamespace"));
            Assert.That(detailData, Does.Contain("Class: TestClass"));
            Assert.That(detailData, Does.Contain("RefID: 12345"));
            Assert.That(detailData, Does.Contain("some: yaml"));
        }
    }
}
