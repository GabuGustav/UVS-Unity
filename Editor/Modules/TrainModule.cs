using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "rail")]
    public class TrainModule : VehicleEditorModuleBase
    {
        private FloatField _maxSpeed;
        private FloatField _accel;
        private FloatField _brake;
        private FloatField _trackGauge;
        private Toggle _useSignals;

        public override string ModuleId => "train";
        public override string DisplayName => "Train";
        public override int Priority => 54;
        public override bool RequiresVehicle => true;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style = { paddingLeft = 20, paddingRight = 20, paddingTop = 20, paddingBottom = 20 }
            };

            container.Add(new Label("Train Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            });

            _maxSpeed = new FloatField("Max Speed (m/s)");
            _maxSpeed.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_maxSpeed);

            _accel = new FloatField("Acceleration (m/s²)");
            _accel.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_accel);

            _brake = new FloatField("Brake (m/s²)");
            _brake.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_brake);

            _trackGauge = new FloatField("Track Gauge (m)");
            _trackGauge.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trackGauge);

            _useSignals = new Toggle("Use Signals");
            _useSignals.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_useSignals);

            return container;
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            var t = config.train;
            _maxSpeed.value = t.maxSpeed;
            _accel.value = t.accel;
            _brake.value = t.brake;
            _trackGauge.value = t.trackGauge;
            _useSignals.value = t.useSignals;
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
                OnConfigChanged(_context.CurrentConfig);
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");
            return ValidationResult.Success();
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var t = _context.CurrentConfig.train;
            t.maxSpeed = _maxSpeed.value;
            t.accel = _accel.value;
            t.brake = _brake.value;
            t.trackGauge = _trackGauge.value;
            t.useSignals = _useSignals.value;
            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }
    }
}
