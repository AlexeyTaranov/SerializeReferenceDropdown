using System;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Preferences
{
    [Serializable]
    public class SerializeReferenceToolsUserPreferences
    {
        private static SerializeReferenceToolsUserPreferences loadedInstance;

        private const string UserPreferencesPrefKey = "SerializeReferenceDropdown.Editor.UserPreferences";

        [SerializeField] private bool enableCrossReferencesCheck;
        [SerializeField] private bool enableSearchTool;
        [SerializeField] private bool copyDataWhenAssignNewType;
        [SerializeField] private int searchToolIntegrationPort = 11000;

        public bool EnableCrossReferencesCheck
        {
            get => enableCrossReferencesCheck;
            set => enableCrossReferencesCheck = value;
        }
        
        public bool EnableSearchTool
        {
            get => enableSearchTool;
            set => enableSearchTool = value;
        }

        public bool CopyDataWithNewType
        {
            get => copyDataWhenAssignNewType;
            set => copyDataWhenAssignNewType = value;
        }

        public int SearchToolIntegrationPort
        {
            get => searchToolIntegrationPort;
            set => searchToolIntegrationPort = value;
        }

        public static SerializeReferenceToolsUserPreferences GetOrLoadSettings()
        {
            if (loadedInstance != null)
            {
                return loadedInstance;
            }
            

            var serializedPreferences = EditorPrefs.GetString(UserPreferencesPrefKey, string.Empty);

            var prefs = string.IsNullOrEmpty(serializedPreferences) == false
                ? JsonUtility.FromJson<SerializeReferenceToolsUserPreferences>(serializedPreferences)
                : new SerializeReferenceToolsUserPreferences();

            loadedInstance = prefs;
            return prefs;
        }

        public void SaveToEditorPrefs()
        {
            EditorPrefs.SetString(UserPreferencesPrefKey, JsonUtility.ToJson(this));
        }
    }
}