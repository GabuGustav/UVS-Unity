using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "land")]
    public class DrivingAssistModule : VehicleEditorModuleBase
    {
        private FloatField _stopAssistSpeed;
        private FloatField _spinKillBrakeTorque;
        private FloatField _rpmStopThreshold;
        private FloatField _reverseEngageSpeed;
        private FloatField _reverseExitSpeed;
        private Toggle _autoFlipEnabled;
        private FloatField _flipMaxSpeed;
        private FloatField _flipAngleThreshold;
        private FloatField _flipTorque;
        private FloatField _flipCooldown;

        public override string ModuleId => "assist";
        public override string DisplayName => "Driving Assist";
        public override int Priority => 62;
        public override bool RequiresVehicle => true;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            return config != null && config.vehicleType == VehicleConfig.VehicleType.Land;
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

            var header = new Label("Driving Assist")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            _stopAssistSpeed = new FloatField("Stop Assist Speed (m/s)");
            _stopAssistSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_stopAssistSpeed);

            _spinKillBrakeTorque = new FloatField("Spin Kill Brake Torque");
            _spinKillBrakeTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_spinKillBrakeTorque);

            _rpmStopThreshold = new FloatField("RPM Stop Threshold");
            _rpmStopThreshold.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_rpmStopThreshold);

            _reverseEngageSpeed = new FloatField("Reverse Engage Speed (m/s)");
            _reverseEngageSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_reverseEngageSpeed);

            _reverseExitSpeed = new FloatField("Reverse Exit Speed (m/s)");
            _reverseExitSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_reverseExitSpeed);

            _autoFlipEnabled = new Toggle("Auto Flip Enabled");
            _autoFlipEnabled.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_autoFlipEnabled);

            _flipMaxSpeed = new FloatField("Flip Max Speed (m/s)");
            _flipMaxSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_flipMaxSpeed);

            _flipAngleThreshold = new FloatField("Flip Angle Threshold (deg)");
            _flipAngleThreshold.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_flipAngleThreshold);

            _flipTorque = new FloatField("Flip Torque");
            _flipTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_flipTorque);

            _flipCooldown = new FloatField("Flip Cooldown (s)");
            _flipCooldown.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_flipCooldown);

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
            var a = config.drivingAssist;
            _stopAssistSpeed.value = a.stopAssistSpeed;
            _spinKillBrakeTorque.value = a.spinKillBrakeTorque;
            _rpmStopThreshold.value = a.rpmStopThreshold;
            _reverseEngageSpeed.value = a.reverseEngageSpeed;
            _reverseExitSpeed.value = a.reverseExitSpeed;
            _autoFlipEnabled.value = a.autoFlipEnabled;
            _flipMaxSpeed.value = a.flipMaxSpeed;
            _flipAngleThreshold.value = a.flipAngleThreshold;
            _flipTorque.value = a.flipTorque;
            _flipCooldown.value = a.flipCooldown;
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var a = _context.CurrentConfig.drivingAssist;
            a.stopAssistSpeed = _stopAssistSpeed.value;
            a.spinKillBrakeTorque = _spinKillBrakeTorque.value;
            a.rpmStopThreshold = _rpmStopThreshold.value;
            a.reverseEngageSpeed = _reverseEngageSpeed.value;
            a.reverseExitSpeed = _reverseExitSpeed.value;
            a.autoFlipEnabled = _autoFlipEnabled.value;
            a.flipMaxSpeed = _flipMaxSpeed.value;
            a.flipAngleThreshold = _flipAngleThreshold.value;
            a.flipTorque = _flipTorque.value;
            a.flipCooldown = _flipCooldown.value;

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
