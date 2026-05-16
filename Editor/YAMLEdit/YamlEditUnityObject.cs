using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SerializeReferenceDropdown.Editor.Utils;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace SerializeReferenceDropdown.Editor.YAMLEdit
{
    public struct TypeData
    {
        public string AssemblyName;
        public string Namespace;
        public string ClassName;

        public string BuildSRTypeStr() =>
            $"class: {FormatYamlInlineValue(ClassName)}, ns: {Namespace}, asm: {AssemblyName}";

        public static TypeData FromType(Type type)
        {
            var rootType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            return new TypeData
            {
                AssemblyName = rootType.Assembly.GetName().Name,
                Namespace = rootType.Namespace,
                ClassName = BuildClassName(type)
            };
        }

        private static string BuildClassName(Type type)
        {
            if (type.IsGenericType == false)
            {
                return type.Name;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments()
                .Select(BuildGenericArgumentName);
            return $"{genericTypeDefinition.Name}[[{string.Join("],[", genericArguments)}]]";
        }

        private static string BuildGenericArgumentName(Type type)
        {
            return $"{BuildFullClassName(type)}, {type.Assembly.GetName().Name}";
        }

        private static string BuildFullClassName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var namespacePrefix = string.IsNullOrEmpty(genericTypeDefinition.Namespace)
                    ? string.Empty
                    : $"{genericTypeDefinition.Namespace}.";
                return $"{namespacePrefix}{BuildClassName(type)}";
            }

            return type.FullName;
        }

        private static string FormatYamlInlineValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOfAny(new[] { ',', '[', ']', '{', '}' }) < 0)
            {
                return value;
            }

            return $"'{value.Replace("'", "''")}'";
        }
    }

    public class YamlEditUnityObject
    {
        public static bool TryModifyReferenceInFile(string assetPath, long fileId, long rid, TypeData newValue)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                {
                    return false;
                }

                var allLines = File.ReadAllLines(assetPath);
                var refIdsNode = FindRefIdsNode(File.ReadAllText(assetPath));
                if (refIdsNode == null)
                {
                    return false;
                }

                if (TryModifyTypeInLineByNode(refIdsNode, ref allLines))
                {
                    File.WriteAllLines(assetPath, allLines);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Log.DevError(e);
                return false;
            }

            YamlNode FindRefIdsNode(string text)
            {
                var parser = new Parser(new StringReader(text));
                var yaml = new YamlStream();
                yaml.Load(parser);

                var localObjectAnchor = new AnchorName(fileId.ToString());
                var doc = yaml.Documents.FirstOrDefault(t => t.RootNode.Anchor == localObjectAnchor);
                var yamlPath = Path.Combine("MonoBehaviour", "references", "RefIds");
                var referenceNode = doc?.RootNode.ReadNodeByPath(yamlPath);
                return referenceNode;
            }

            bool TryModifyTypeInLineByNode(YamlNode targetNode, ref string[] allLines)
            {
                var startLine = targetNode.Start.Line;
                var endLine = targetNode.End.Line;
                endLine = startLine == endLine ? allLines.Length - 1 : endLine;
                var ridText = $"rid: {rid}";
                for (long i = startLine - 1; i < endLine; i++)
                {
                    var line = allLines[i];
                    if (line.Contains(ridText) && i + 1 < endLine)
                    {
                        var nextLine = allLines[i + 1];
                        var newType = newValue.BuildSRTypeStr();
                        var modifiedLine = Regex.Replace(nextLine, @"\{.*?\}", "{ " + newType + " }");
                        allLines[i + 1] = modifiedLine;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
