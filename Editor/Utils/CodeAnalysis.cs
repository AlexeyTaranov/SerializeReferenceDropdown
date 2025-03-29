using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class CodeAnalysis
    {
        public static (string filePath, int lineNumber, int columnNumber) GetSourceFileLocation(Type targetType)
        {
            var iterator = new FileIterator<MonoScript>(AnalyseSourceFile)
            {
                ProgressBarLabel = "Open Source File",
                ProgressBarInfoPrefix = "Analyze source file data"
            };
            
            var filePath = string.Empty;
            var lineNumber = -1;
            var columnNumber = -1;

            iterator.IterateOnUnityAssetFiles();

            return (filePath, lineNumber, columnNumber);

            bool AnalyseSourceFile(string scriptPath)
            {
                using var reader = new StreamReader(scriptPath);
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
                        filePath = scriptPath;
                        lineNumber = currentLine;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}