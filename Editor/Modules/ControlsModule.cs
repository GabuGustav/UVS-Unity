using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.InputSystem;
using UVS.Editor.Core;
using System;
using System.Linq;

namespace UVS.Editor.Modules
{
    public class ControlsModule : VehicleEditorModuleBase
    {
        private ObjectField _defaultProfileField;
        private ObjectField _overrideProfileField;
        private VisualElement _actionsContainer;
        private Button _createDefaultButton;
        private Button _autoMapButton;
        private Button _validateButton;

        public override string ModuleId => "controls";
        public override string DisplayName => "Controls";
        public override int Priority => 16;
        public override bool RequiresVehicle => true;

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

            var header = new Label("Control Profiles")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            _defaultProfileField = new ObjectField("Default Profile")
            {
                objectType = typeof(VehicleInputProfile),
                value = GetOrCreateDefaultProfile(false)
            };
            _defaultProfileField.SetEnabled(false);
            container.Add(_defaultProfileField);

            _createDefaultButton = new Button(() =>
            {
                var profile = GetOrCreateDefaultProfile(true);
                _defaultProfileField.value = profile;
                BuildActionsUI();
            })
            { text = "Create Default Profile" };
            container.Add(_createDefaultButton);

            _overrideProfileField = new ObjectField("Override Profile")
            {
                objectType = typeof(VehicleInputProfile)
            };
            _overrideProfileField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig == null) return;
                _context.CurrentConfig.inputProfileOverride = evt.newValue as VehicleInputProfile;
                EditorUtility.SetDirty(_context.CurrentConfig);
                _context.NotifyConfigChanged(_context.CurrentConfig);
                BuildActionsUI();
            });
            container.Add(_overrideProfileField);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 8 } };
            _autoMapButton = new Button(AutoMapFromVehicleControls) { text = "Auto-Map From VehicleControls" };
            _validateButton = new Button(ValidateControls) { text = "Validate Controls" };
            buttonRow.Add(_autoMapButton);
            buttonRow.Add(_validateButton);
            container.Add(buttonRow);

            _actionsContainer = new VisualElement { style = { marginTop = 12 } };
            container.Add(_actionsContainer);

            return container;
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
                _overrideProfileField.value = _context.CurrentConfig.inputProfileOverride;

            _defaultProfileField.value = GetOrCreateDefaultProfile(false);
            BuildActionsUI();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            _overrideProfileField.value = config.inputProfileOverride;
            BuildActionsUI();
        }

        private VehicleInputProfile GetOrCreateDefaultProfile(bool createIfMissing)
        {
            var profile = VehicleInputProfile.GetDefault();
            if (profile != null) return profile;

            // Try to locate any existing profile
            var found = AssetDatabase.FindAssets("t:VehicleInputProfile")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VehicleInputProfile>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(p => p != null);

            if (found != null && !createIfMissing) return found;

            if (!createIfMissing) return null;

            var resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var assetPath = $"{resourcesPath}/{VehicleInputProfile.DefaultResourcePath}.asset";
            var newProfile = ScriptableObject.CreateInstance<VehicleInputProfile>();
            AssetDatabase.CreateAsset(newProfile, assetPath);
            AssetDatabase.SaveAssets();
            return newProfile;
        }

        private VehicleInputProfile GetActiveProfile()
        {
            if (_context?.CurrentConfig != null && _context.CurrentConfig.inputProfileOverride != null)
                return _context.CurrentConfig.inputProfileOverride;

            return GetOrCreateDefaultProfile(false);
        }

        private void BuildActionsUI()
        {
            _actionsContainer.Clear();

            var profile = GetActiveProfile();
            if (profile == null)
            {
                _actionsContainer.Add(new Label("No input profile available. Create a default profile first."));
                return;
            }

            AddActionField("Throttle", profile, p => p.throttle, (p, v) => p.throttle = v);
            AddActionField("Brake", profile, p => p.brake, (p, v) => p.brake = v);
            AddActionField("Steer", profile, p => p.steer, (p, v) => p.steer = v);
            AddActionField("Handbrake", profile, p => p.handbrake, (p, v) => p.handbrake = v);

            AddActionField("Pitch", profile, p => p.pitch, (p, v) => p.pitch = v);
            AddActionField("Roll", profile, p => p.roll, (p, v) => p.roll = v);
            AddActionField("Yaw", profile, p => p.yaw, (p, v) => p.yaw = v);
            AddActionField("Vertical", profile, p => p.vertical, (p, v) => p.vertical = v);

            AddActionField("Hop", profile, p => p.hop, (p, v) => p.hop = v);
            AddActionField("Front Lift", profile, p => p.frontLift, (p, v) => p.frontLift = v);
            AddActionField("Rear Lift", profile, p => p.rearLift, (p, v) => p.rearLift = v);
            AddActionField("Left Tilt", profile, p => p.leftTilt, (p, v) => p.leftTilt = v);
            AddActionField("Right Tilt", profile, p => p.rightTilt, (p, v) => p.rightTilt = v);
            AddActionField("Slam", profile, p => p.slam, (p, v) => p.slam = v);

            AddActionField("Recover", profile, p => p.recover, (p, v) => p.recover = v);
        }

        private void AddActionField(string label, VehicleInputProfile profile,
            Func<VehicleInputProfile, InputActionReference> getter,
            Action<VehicleInputProfile, InputActionReference> setter)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(InputActionReference),
                value = getter(profile)
            };

            field.RegisterValueChangedCallback(evt =>
            {
                setter(profile, evt.newValue as InputActionReference);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            });

            _actionsContainer.Add(field);
        }

        private void AutoMapFromVehicleControls()
        {
            var profile = GetActiveProfile();
            if (profile == null)
            {
                LogError("No input profile available.");
                return;
            }

            var asset = LoadVehicleControlsAsset();
            if (asset == null)
            {
                LogError("VehicleControls.inputactions not found.");
                return;
            }

            Map(profile, asset, "Throttle", r => profile.throttle = r);
            Map(profile, asset, "Brake", r => profile.brake = r);
            Map(profile, asset, "Steer", r => profile.steer = r);
            Map(profile, asset, "Handbrake", r => profile.handbrake = r);

            Map(profile, asset, "Pitch", r => profile.pitch = r);
            Map(profile, asset, "Roll", r => profile.roll = r);
            Map(profile, asset, "Yaw", r => profile.yaw = r);
            Map(profile, asset, "Vertical", r => profile.vertical = r);

            Map(profile, asset, "Hop", r => profile.hop = r);
            Map(profile, asset, "FrontLift", r => profile.frontLift = r);
            Map(profile, asset, "RearLift", r => profile.rearLift = r);
            Map(profile, asset, "LeftTilt", r => profile.leftTilt = r);
            Map(profile, asset, "RightTilt", r => profile.rightTilt = r);
            Map(profile, asset, "Slam", r => profile.slam = r);
            Map(profile, asset, "Recover", r => profile.recover = r);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            BuildActionsUI();
            LogMessage("Mapped actions from VehicleControls.");
        }

        private InputActionAsset LoadVehicleControlsAsset()
        {
            var path = "Assets/Input/VehicleControls.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset != null) return asset;

            var guid = AssetDatabase.FindAssets("VehicleControls t:InputActionAsset").FirstOrDefault();
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void Map(VehicleInputProfile profile, InputActionAsset asset, string actionName, Action<InputActionReference> assign)
        {
            var action = asset.FindAction($"Vehicle/{actionName}", throwIfNotFound: false);
            if (action == null)
            {
                LogWarning($"Action not found: {actionName}");
                return;
            }

            var reference = InputActionReference.Create(action);
            AssetDatabase.AddObjectToAsset(reference, profile);
            assign(reference);
        }

        private void ValidateControls()
        {
            var profile = GetActiveProfile();
            if (profile == null)
            {
                LogError("No input profile available.");
                return;
            }

            if (profile.throttle == null) LogWarning("Missing Throttle action");
            if (profile.brake == null) LogWarning("Missing Brake action");
            if (profile.steer == null) LogWarning("Missing Steer action");

            if (_context?.CurrentConfig == null) return;
            var type = _context.CurrentConfig.vehicleType;
            if (type == VehicleConfig.VehicleType.Air)
            {
                if (profile.pitch == null) LogWarning("Missing Pitch action");
                if (profile.roll == null) LogWarning("Missing Roll action");
                if (profile.yaw == null) LogWarning("Missing Yaw action");
            }
            if (type == VehicleConfig.VehicleType.Water)
            {
                if (profile.throttle == null) LogWarning("Missing Throttle action");
                if (profile.brake == null) LogWarning("Missing Brake action");
                if (profile.steer == null) LogWarning("Missing Steer action");
            }
        }

        protected override ValidationResult ValidateModule()
{
    var result = ValidationResult.Success();

    var profile = GetActiveProfile();
    if (profile == null)
    {
        result.AddError("No input profile assigned.");
        return result;
    }

    if (profile.throttle == null) result.AddWarning("Throttle action not assigned.");
    if (profile.brake == null) result.AddWarning("Brake action not assigned.");
    if (profile.steer == null) result.AddWarning("Steer action not assigned.");

    if (_context?.CurrentConfig != null)
    {
        var type = _context.CurrentConfig.vehicleType;
        if (type == VehicleConfig.VehicleType.Air)
        {
            if (profile.pitch == null) result.AddWarning("Pitch action not assigned.");
            if (profile.roll == null) result.AddWarning("Roll action not assigned.");
            if (profile.yaw == null) result.AddWarning("Yaw action not assigned.");
        }
    }

    return result;
}

    }
}
