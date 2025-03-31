using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class CodeAnalysis
    {
        public static (string filePath, int lineNumber, int columnNumber) GetSourceFileLocation(Type targetType)
        {
            var iterator = new FileIterator(AnalyseSourceFile)
            {
                ProgressBarLabel = "Open Source File",
                FileNameExtension = "cs",
                ProgressBarInfoPrefix = "Analyze source file data"
            };
            
            var filePath = string.Empty;
            var lineNumber = -1;
            var columnNumber = -1;

            iterator.IterateOnUnityProjectFiles();

            return (filePath, lineNumber, columnNumber);

            bool AnalyseSourceFile(string fullFilePath)
            {
                using var reader = new StreamReader(fullFilePath);
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
                        columnNumber = classMatch.Index + 1;
                        filePath = fullFilePath.Replace(Application.dataPath, "");
                        filePath = $"Assets{filePath}";
                        lineNumber = currentLine;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}