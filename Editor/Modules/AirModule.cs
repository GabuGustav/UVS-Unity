using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "air")]
    public class AirModule : VehicleEditorModuleBase
    {
        private FloatField _wingArea;
        private FloatField _liftCoefficient;
        private FloatField _dragCoefficient;
        private FloatField _airDensity;
        private FloatField _maxThrust;
        private FloatField _pitchTorque;
        private FloatField _rollTorque;
        private FloatField _yawTorque;

        public override string ModuleId => "air";
        public override string DisplayName => "Air Physics";
        public override int Priority => 55;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => false;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            return config != null && config.vehicleType == VehicleConfig.VehicleType.Air;
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

            var header = new Label("Air Physics Settings")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(header);

            _wingArea = new FloatField("Wing Area (m2)");
            _wingArea.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_wingArea);

            _liftCoefficient = new FloatField("Lift Coefficient");
            _liftCoefficient.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_liftCoefficient);

            _dragCoefficient = new FloatField("Drag Coefficient");
            _dragCoefficient.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_dragCoefficient);

            _airDensity = new FloatField("Air Density (kg/m3)");
            _airDensity.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_airDensity);

            _maxThrust = new FloatField("Max Thrust (N)");
            _maxThrust.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_maxThrust);

            _pitchTorque = new FloatField("Pitch Torque");
            _pitchTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_pitchTorque);

            _rollTorque = new FloatField("Roll Torque");
            _rollTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_rollTorque);

            _yawTorque = new FloatField("Yaw Torque");
            _yawTorque.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_yawTorque);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var a = _context.CurrentConfig.air;
            if (a.wingArea <= 0) return ValidationResult.Error("Wing area must be > 0");
            if (a.liftCoefficient <= 0) return ValidationResult.Error("Lift coefficient must be > 0");
            if (a.dragCoefficient <= 0) return ValidationResult.Error("Drag coefficient must be > 0");
            if (a.airDensity <= 0) return ValidationResult.Error("Air density must be > 0");
            if (a.maxThrust <= 0) return ValidationResult.Warning("Max thrust is low");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                LoadFromConfig(config);
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                LoadFromConfig(_context.CurrentConfig);
            }
        }

        private void LoadFromConfig(VehicleConfig config)
        {
            var a = config.air;
            _wingArea.value = a.wingArea;
            _liftCoefficient.value = a.liftCoefficient;
            _dragCoefficient.value = a.dragCoefficient;
            _airDensity.value = a.airDensity;
            _maxThrust.value = a.maxThrust;
            _pitchTorque.value = a.pitchTorque;
            _rollTorque.value = a.rollTorque;
            _yawTorque.value = a.yawTorque;
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var a = _context.CurrentConfig.air;
            a.wingArea = _wingArea.value;
            a.liftCoefficient = _liftCoefficient.value;
            a.dragCoefficient = _dragCoefficient.value;
            a.airDensity = _airDensity.value;
            a.maxThrust = _maxThrust.value;
            a.pitchTorque = _pitchTorque.value;
            a.rollTorque = _rollTorque.value;
            a.yawTorque = _yawTorque.value;

            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }

        public override void OnModuleGUI() { }
    }
}
