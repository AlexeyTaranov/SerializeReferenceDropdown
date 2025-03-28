using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Preferences
{
    public class SerializeReferenceToolsSettingsProvider : SettingsProvider
    {
        private const string Path = "Preferences/Serialize Reference Tools";

        private readonly SerializeReferenceToolsUserPreferences preferences;

        private SerializedObject serializedObject;

        public SerializeReferenceToolsSettingsProvider(string path, SettingsScope scopes,
            IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            label = "Serialize Reference Tool";
            preferences = SerializeReferenceToolsUserPreferences.GetOrLoadSettings();
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SerializeReferenceToolsSettingsProvider(Path, SettingsScope.User);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            preferences.DisableCrossReferencesCheck = GUILayout.Toggle(preferences.DisableCrossReferencesCheck,
                "Disable Cross References Check");
            preferences.DisableOpenSourceFile = GUILayout.Toggle(preferences.DisableOpenSourceFile, "Disable Open Source File");
            preferences.CopyDataWithNewType =
                GUILayout.Toggle(preferences.CopyDataWithNewType, "Copy Data With New Type");

            if (EditorGUI.EndChangeCheck())
            {
                preferences.SaveToEditorPrefs();
            }
        }
    }
}