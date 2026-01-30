using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SerializeReferenceDropdown.Editor.YAMLEdit;
using UnityEditor;
using YamlDotNet.RepresentationModel;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class MissingTypeUtils
    {
        public static string GetDetailData(this ManagedReferenceMissingType missingTypeData)
        {
            return FormatDetailData(missingTypeData.assemblyName, missingTypeData.namespaceName, missingTypeData.className, missingTypeData.referenceId, missingTypeData.serializedData);
        }

        public static string FormatDetailData(string assemblyName, string namespaceName, string className, long referenceId, string serializedData)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ASM: {assemblyName}");
            sb.AppendLine($"Namespace: {namespaceName}");
            sb.AppendLine($"Class: {className}");
            sb.AppendLine($"RefID: {referenceId}");
            sb.AppendFormat("\n{0}", serializedData);
            return sb.ToString();
        }

        private static string ConvertPropertyPath(string path) => path.Replace(".Array.data", string.Empty);

        //With missing types - invalid and real managedReferenceId available only on yaml from file
        public static IReadOnlyList<(string propertyPath, long refId)> GetMissingPropertyPaths(SerializedProperty property, string assetPath)
        {
            var missingTypes =
                SerializationUtility.GetManagedReferencesWithMissingTypes(property.serializedObject.targetObject);

            var allSerializeReferencePaths = FindAllSerializeReferencePathsInTargetObject();
            var missingPaths = new List<(string propertyPath, long refId)>();

            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(assetPath)));
            var doc = yaml.Documents.FirstOrDefault();

            foreach (var path in allSerializeReferencePaths)
            {
                var shortPath = ConvertPropertyPath(path);
                var subPathElements = shortPath.Split('.');
                var yamlPath = Path.Combine("MonoBehaviour", Path.Combine(subPathElements));
                var propertyNode = doc.RootNode.ReadNodeByPath(yamlPath);
                if (propertyNode is YamlMappingNode map)
                {
                    var rid = map.Children[new YamlScalarNode("rid")].ToString();
                    if (TryGetMissingReference(rid, out var missingProperty))
                    {
                        missingPaths.Add((path, missingProperty.referenceId));
                    }
                }
            }

            return missingPaths;

            HashSet<string> FindAllSerializeReferencePathsInTargetObject()
            {
                var paths = new HashSet<string>();
                SOUtils.TraverseSO(property.serializedObject.targetObject, FillAllPaths);
                return paths;

                bool FillAllPaths(SerializedProperty serializeReferenceProperty)
                {
                    paths.Add(serializeReferenceProperty.propertyPath);
                    return false;
                }
            }

            bool TryGetMissingReference(string referenceId, out ManagedReferenceMissingType missingType)
            {
                foreach (var checkType in missingTypes)
                {
                    if (checkType.referenceId.ToString() == referenceId)
                    {
                        missingType = checkType;
                        return true;
                    }
                }

                missingType = default;
                return false;
            }
        }
    }
}