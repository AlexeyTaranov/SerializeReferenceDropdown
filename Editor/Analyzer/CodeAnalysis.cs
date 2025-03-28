using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Analyzer
{
    public static class CodeAnalysis
    {
        public static (string filePath, int lineNumber, int columnNumber) GetSourceFileLocation(Type targetType)
        {
            var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                using var reader = new StreamReader(file);
                var currentLine = 0;

                var foundNamespace = String.Empty;
                while (reader.ReadLine() is { } line)
                {
                    currentLine++;

                    var namespaceMatch = Regex.Match(line, @"\bnamespace\s+([\w.]+)");
                    if (namespaceMatch.Success)
                    {
                        foundNamespace = namespaceMatch.Groups[1].Value;
                    }

                    var className = targetType.Name;
                    var classMatch = Regex.Match(line, @"\bclass\s+" + className + @"\b");
                    
                    if (classMatch.Success && targetType.Namespace == foundNamespace)
                    {
                        var columnNumber = classMatch.Index + 1;
                        var relativePath = file.Replace(Application.dataPath, "");
                        relativePath = $"Assets{relativePath}";
                        return (relativePath, currentLine, columnNumber);
                    }
                }
            }

            return default;
        }
    }
}