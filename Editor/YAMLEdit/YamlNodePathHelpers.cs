using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace SerializeReferenceDropdown.Editor.YAMLEdit
{
    public static class YamlNodePathHelpers
    {
        public static YamlNode ReadNodeByPath(this YamlNode startNode, string path)
        {
            var parts = Regex.Matches(path, @"[^/\[\]]+|\[\d+\]").Select(m => m.Value);

            YamlNode current = startNode;

            foreach (var rawPart in parts)
            {
                if (current is YamlMappingNode map)
                {
                    var key = new YamlScalarNode(rawPart);
                    if (!map.Children.TryGetValue(key, out current))
                    {
                        return null;
                    }

                    continue;
                }

                if (current is YamlSequenceNode sequence)
                {
                    int index = 0;
                    if (rawPart.StartsWith('[') && rawPart.EndsWith(']'))
                    {
                        var indexPart = rawPart.TrimStart('[').TrimEnd(']');
                        if (!int.TryParse(indexPart, out index))
                        {
                            return null;
                        }
                    }
                    else if (!int.TryParse(rawPart, out index))
                    {
                        return null;
                    }

                    if (index < 0 || index >= sequence.Children.Count)
                    {
                        return null;
                    }

                    current = sequence.Children[index];
                    continue;
                }

                return null;
            }

            return current;
        }
    }
}