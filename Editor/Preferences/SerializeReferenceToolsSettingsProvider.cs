using System.Collections.Generic;
using SerializeReferenceDropdown.Editor.SearchTool;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
            label = "Serialize Reference Tools";
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

            EditorGUILayout.LabelField("Experimental", EditorStyles.boldLabel);
            preferences.EnableCrossReferencesCheck = GUILayout.Toggle(preferences.EnableCrossReferencesCheck,
                "Enable Cross References Check");
            preferences.EnableSearchTool = GUILayout.Toggle(preferences.EnableSearchTool, "Enable Search Tool");
            

            if (EditorGUI.EndChangeCheck())
            {
                preferences.SaveToEditorPrefs();
            }
        }
    }
}