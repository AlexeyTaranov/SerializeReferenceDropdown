using NUnit.Framework;
using SerializeReferenceDropdown.Editor.Utils;

namespace SerializeReferenceDropdown.Editor.Tests
{
    public class CodeAnalysisTests
    {
        [Test]
        public void GetSourceFileLocation_FindsThisFile()
        {
            // We search for the location of this test class itself.
            var (filePath, lineNumber, columnNumber) = CodeAnalysis.GetSourceFileLocation(typeof(CodeAnalysisTests));

            Assert.IsNotNull(filePath, "File path should not be null");
            Assert.IsNotEmpty(filePath, "File path should not be empty");
            Assert.That(filePath, Does.EndWith("CodeAnalysisTests.cs"));
            Assert.Greater(lineNumber, 0, "Line number should be greater than 0");
        }
    }
}
