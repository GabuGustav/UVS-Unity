using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UVS.Editor.Core;
using System.Linq;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "land")]
    public class TrackDriveModule : VehicleEditorModuleBase
    {
        private FloatField _trackTorqueMultiplier;
        private FloatField _trackBrakeStrength;
        private FloatField _steerBlend;
        private FloatField _trackDifferentialStrength;
        private FloatField _maxTrackSpeed;
        private FloatField _trackDrag;

        public override string ModuleId => "trackdrive";
        public override string DisplayName => "Track Drive";
        public override int Priority => 58;
        public override bool RequiresVehicle => true;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            if (config == null || config.vehicleType != VehicleConfig.VehicleType.Land) return false;
            bool hasTrackRoles = config.wheels != null && config.wheels.Any(w =>
                w.role == VehicleConfig.WheelRole.TrackLeft || w.role == VehicleConfig.WheelRole.TrackRight);
            bool isTank = config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Tank;
            return hasTrackRoles || isTank;
        }

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

            var header = new Label("Track Drive Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            _trackTorqueMultiplier = new FloatField("Track Torque Multiplier");
            _trackTorqueMultiplier.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trackTorqueMultiplier);

            _trackBrakeStrength = new FloatField("Track Brake Strength");
            _trackBrakeStrength.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trackBrakeStrength);

            _steerBlend = new FloatField("Steer Blend (0..1)");
            _steerBlend.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_steerBlend);

            _trackDifferentialStrength = new FloatField("Track Differential Strength");
            _trackDifferentialStrength.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trackDifferentialStrength);

            _maxTrackSpeed = new FloatField("Max Track Speed (m/s)");
            _maxTrackSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_maxTrackSpeed);

            _trackDrag = new FloatField("Track Drag");
            _trackDrag.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trackDrag);

            return container;
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
                LoadFromConfig(_context.CurrentConfig);
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
                LoadFromConfig(config);
        }

        private void LoadFromConfig(VehicleConfig config)
        {
            var t = config.trackDrive;
            _trackTorqueMultiplier.value = t.trackTorqueMultiplier;
            _trackBrakeStrength.value = t.trackBrakeStrength;
            _steerBlend.value = t.steerBlend;
            _trackDifferentialStrength.value = t.trackDifferentialStrength;
            _maxTrackSpeed.value = t.maxTrackSpeed;
            _trackDrag.value = t.trackDrag;
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var t = _context.CurrentConfig.trackDrive;
            t.trackTorqueMultiplier = _trackTorqueMultiplier.value;
            t.trackBrakeStrength = _trackBrakeStrength.value;
            t.steerBlend = _steerBlend.value;
            t.trackDifferentialStrength = _trackDifferentialStrength.value;
            t.maxTrackSpeed = _maxTrackSpeed.value;
            t.trackDrag = _trackDrag.value;

            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null) return ValidationResult.Warning("No vehicle loaded");
            return ValidationResult.Success();
        }
    }
}
