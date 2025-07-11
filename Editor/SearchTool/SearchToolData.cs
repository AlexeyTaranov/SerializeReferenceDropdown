using System;
using System.Collections.Generic;
using System.Linq;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    [Serializable]
    public class SearchToolData
    {
        //Need sync same port between rider and unity-server
        public int IntegrationPort;
        public List<PrefabData> PrefabsData = new List<PrefabData>();
        public List<ScriptableObjectData> SOsData = new List<ScriptableObjectData>();

        [Serializable]
        public struct ReferenceIdData
        {
            public long referenceId;
            public Type objectType;
        }

        [Serializable]
        public struct ReferencePropertyData : IEquatable<ReferencePropertyData>
        {
            public string propertyPath;
            public long assignedReferenceId;
            public Type propertyType;

            public bool Equals(ReferencePropertyData other)
            {
                return propertyPath == other.propertyPath && assignedReferenceId == other.assignedReferenceId &&
                       propertyType == other.propertyType;
            }

            public override bool Equals(object obj)
            {
                return obj is ReferencePropertyData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(propertyPath, assignedReferenceId, propertyType);
            }
        }

        public interface IAssetData
        {
            public string AssetPath { get; }
            bool IsHaveCrossReferences();
        }

        [Serializable]
        public class PrefabData : IAssetData
        {
            public PrefabData(string assetPath)
            {
                AssetPath = assetPath;
            }

            public string AssetPath { get; }

            public bool IsHaveCrossReferences()
            {
                foreach (var componentData in componentsData)
                {
                    var groups = componentData.RefPropertiesData.GroupBy(t => t.assignedReferenceId);
                    if (groups.Any(t => t.Count() > 1))
                    {
                        return true;
                    }
                }

                return false;
            }

            public List<PrefabComponentData> componentsData = new List<PrefabComponentData>();
        }


        [Serializable]
        public class ScriptableObjectData : UnityObjectReferenceData, IAssetData
        {
            public string AssetPath { get; set; }

            public bool IsHaveCrossReferences()
            {
                var groups = RefPropertiesData.GroupBy(t => t.assignedReferenceId);
                if (groups.Any(t => t.Count() > 1))
                {
                    return true;
                }

                return false;
            }
        }

        public class PrefabComponentData : UnityObjectReferenceData
        {
            public long FileId;
        }

        public class UnityObjectReferenceData
        {
            public List<ReferenceIdData> RefIdsData = new List<ReferenceIdData>();
            public List<ReferencePropertyData> RefPropertiesData = new List<ReferencePropertyData>();
        }
    }
}