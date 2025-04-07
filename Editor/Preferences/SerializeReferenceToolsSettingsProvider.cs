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
        private int? startPort;

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

            preferences.CopyDataWithNewType =
                GUILayout.Toggle(preferences.CopyDataWithNewType, "Copy Data With New Type");

            EditorGUILayout.LabelField("Experimental", EditorStyles.boldLabel);
            preferences.EnableCrossReferencesCheck = GUILayout.Toggle(preferences.EnableCrossReferencesCheck,
                "Enable Cross References Check");
            preferences.EnableSearchTool = GUILayout.Toggle(preferences.EnableSearchTool, "Enable Search Tool");
            preferences.SearchToolIntegrationPort =
                EditorGUILayout.IntField("Search Tool Port", preferences.SearchToolIntegrationPort);

            if (EditorGUI.EndChangeCheck())
            {
                preferences.SaveToEditorPrefs();
            }
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            startPort = preferences.SearchToolIntegrationPort;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            if (startPort != null && startPort != preferences.SearchToolIntegrationPort)
            {
                SearchToolWindowIntegration.Run();
            }

            startPort = preferences.SearchToolIntegrationPort;
        }
    }
}