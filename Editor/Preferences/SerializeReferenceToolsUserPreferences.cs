using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Preferences
{
    public class SerializeReferenceToolsUserPreferences
    {
        private static SerializeReferenceToolsUserPreferences loadedInstance;

        private const string UserPreferencesPrefKey = "SerializeReferenceDropdown.Editor.UserPreferences";

        [SerializeField] private bool disableCrossReferencesCheck;
        [SerializeField] private bool disableOpenSourceFile;
        [SerializeField] private bool copyDataWhenAssignNewType;

        public bool DisableCrossReferencesCheck
        {
            get => disableCrossReferencesCheck;
            set => disableCrossReferencesCheck = value;
        }

        public bool DisableOpenSourceFile
        {
            get => disableOpenSourceFile;
            set => disableOpenSourceFile = value;
        }

        public bool CopyDataWithNewType
        {
            get => copyDataWhenAssignNewType;
            set => copyDataWhenAssignNewType = value;
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