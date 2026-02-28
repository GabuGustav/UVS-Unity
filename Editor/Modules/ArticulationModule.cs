using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "land", categoryId: "articulated_truck")]
    [VehicleModuleSupport(typeId: "land", categoryId: "semi_truck")]
    [VehicleModuleSupport(typeId: "land", categoryId: "tractor")]
    public class ArticulationModule : VehicleEditorModuleBase
    {
        private ObjectField _tractorHitch;
        private ObjectField _trailerHitch;
        private FloatField _yawLimit;
        private FloatField _damping;
        private FloatField _spring;
        private Toggle _detachable;

        public override string ModuleId => "articulation";
        public override string DisplayName => "Articulation";
        public override int Priority => 52;
        public override bool RequiresVehicle => true;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style = { paddingLeft = 20, paddingRight = 20, paddingTop = 20, paddingBottom = 20 }
            };

            container.Add(new Label("Articulation Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            });

            _tractorHitch = new ObjectField("Tractor Hitch")
            {
                objectType = typeof(Transform)
            };
            _tractorHitch.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_tractorHitch);

            _trailerHitch = new ObjectField("Trailer Hitch")
            {
                objectType = typeof(Transform)
            };
            _trailerHitch.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trailerHitch);

            _yawLimit = new FloatField("Hitch Yaw Limit (deg)");
            _yawLimit.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_yawLimit);

            _damping = new FloatField("Hitch Damping");
            _damping.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_damping);

            _spring = new FloatField("Hitch Spring");
            _spring.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_spring);

            _detachable = new Toggle("Detachable");
            _detachable.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_detachable);

            return container;
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            var a = config.articulation;
            _tractorHitch.value = a.tractorHitch;
            _trailerHitch.value = a.trailerHitch;
            _yawLimit.value = a.hitchYawLimit;
            _damping.value = a.hitchDamping;
            _spring.value = a.hitchSpring;
            _detachable.value = a.detachable;
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

            var a = _context.CurrentConfig.articulation;
            if (a.tractorHitch == null || a.trailerHitch == null)
                return ValidationResult.Warning("Assign tractor and trailer hitch transforms");

            return ValidationResult.Success();
        }

        private void SaveConfig()
        {
            if (_context?.CurrentConfig == null) return;
            var a = _context.CurrentConfig.articulation;
            a.tractorHitch = _tractorHitch.value as Transform;
            a.trailerHitch = _trailerHitch.value as Transform;
            a.hitchYawLimit = _yawLimit.value;
            a.hitchDamping = _damping.value;
            a.hitchSpring = _spring.value;
            a.detachable = _detachable.value;
            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }
    }
}
