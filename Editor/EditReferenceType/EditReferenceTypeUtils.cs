using System.IO;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.EditReferenceType
{
    public struct TypeData
    {
        public string AssemblyName;
        public string Namespace;
        public string ClassName;
    }

    public static class EditReferenceTypeUtils
    {
        public static bool TryModifyDirectFileReferenceType(string assetPath, long rid, TypeData from, TypeData to)
        {
            var fullPath = Path.GetFullPath(assetPath);

            if (File.Exists(fullPath) == false)
            {
                return false;
            }

            var lines = File.ReadAllLines(fullPath);
            var ridStr = $"- rid: {rid.ToString()}";
            var oldTypeStr = BuildSRTypeStr(from.ClassName, from.Namespace, from.AssemblyName);
            var newTypeStr = BuildSRTypeStr(to.ClassName, to.Namespace, to.AssemblyName);
            var fullSrTypeStr = "type: {" + oldTypeStr + "}";

            var needWriteFile = false;

            for (int i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i];
                if (line.Contains(ridStr))
                {
                    var nextLine = lines[i + 1];
                    if (nextLine.Contains(fullSrTypeStr))
                    {
                        var newLine = nextLine.Replace(oldTypeStr, newTypeStr);
                        lines[i + 1] = newLine;
                        needWriteFile = true;
                        break;
                    }
                }
            }

            if (needWriteFile)
            {
                File.WriteAllLines(fullPath, lines);
                AssetDatabase.Refresh();
            }

            return needWriteFile;


            string BuildSRTypeStr(string c, string n, string a) => $"class: {c}, ns: {n}, asm: {a}";
        }
    }
}