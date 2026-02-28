using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UVS.Editor.Core;
using System;
using System.Linq;
using System.Reflection;

namespace UVS.Editor.Modules
{
    public class SettingsModule : VehicleEditorModuleBase
    {
        private UVSSettingsBase _settingsAsset;
        private Type _settingsType;
        private IMGUIContainer _imgui;
        private Button _generateButton;

        public override string ModuleId => "settings";
        public override string DisplayName => "Settings";
        public override int Priority => 14;
        public override bool RequiresVehicle => false;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style =
                {
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 20,
                    paddingBottom = 20
                }
            };

            var header = new Label("UVS Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            _generateButton = new Button(GenerateDefaultSettings) { text = "Generate Default Settings" };
            container.Add(_generateButton);

            _imgui = new IMGUIContainer(DrawSettingsGUI);
            container.Add(_imgui);

            RefreshSettingsAsset();
            return container;
        }

        protected override void OnModuleActivated()
        {
            RefreshSettingsAsset();
        }

        private void RefreshSettingsAsset()
        {
            _settingsType = FindSettingsType();
            _settingsAsset = _settingsType != null ? FindOrCreateSettingsAsset(_settingsType) : null;

            _generateButton.style.display = _settingsType == null ? DisplayStyle.Flex : DisplayStyle.None;
            _imgui.style.display = _settingsAsset != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Type FindSettingsType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => typeof(UVSSettingsBase).IsAssignableFrom(t) && !t.IsAbstract);
        }

        private UVSSettingsBase FindOrCreateSettingsAsset(Type type)
        {
            var guid = AssetDatabase.FindAssets($"t:{type.Name}").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type) as UVSSettingsBase;
            }

            var folder = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var asset = ScriptableObject.CreateInstance(type) as UVSSettingsBase;
            var path = $"{folder}/{type.Name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private void DrawSettingsGUI()
        {
            if (_settingsAsset == null || _settingsType == null)
            {
                EditorGUILayout.HelpBox("No UVS settings script found. Generate one to get started.", MessageType.Info);
                return;
            }

            var so = new SerializedObject(_settingsAsset);
            so.Update();

            var fields = _settingsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<UVSSettingAttribute>() != null);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<UVSSettingAttribute>();
                var prop = so.FindProperty(field.Name);
                if (prop == null) continue;

                string label = string.IsNullOrEmpty(attr.Label) ? ObjectNames.NicifyVariableName(field.Name) : attr.Label;
                EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
            }

            so.ApplyModifiedProperties();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            // Settings module does not depend on vehicle config
        }

        protected override ValidationResult ValidateModule()
        {
            // Always valid - global settings module
            return ValidationResult.Success();
        }

        private void GenerateDefaultSettings()
        {
            var path = "Assets/Scripts/Settings/UVSDefaultSettings.cs";
            if (!AssetDatabase.IsValidFolder("Assets/Scripts/Settings"))
                AssetDatabase.CreateFolder("Assets/Scripts", "Settings");

            if (!System.IO.File.Exists(path))
            {
                var content =
@"using UnityEngine;

public class UVSDefaultSettings : UVSSettingsBase
{
    [UVSSetting(""Master Volume"", ""Audio"", 0f, 1f)]
    public float masterVolume = 0.8f;

    [UVSSetting(""Show Debug"", ""General"")]
    public bool showDebug = false;

    [UVSSetting(""Quality Level"", ""Graphics"")]
    public int qualityLevel = 2;
}
";
                System.IO.File.WriteAllText(path, content);
                AssetDatabase.ImportAsset(path);
            }

            RefreshSettingsAsset();
        }
    }
}
