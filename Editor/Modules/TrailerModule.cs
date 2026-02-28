using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    [VehicleModuleSupport(typeId: "land", categoryId: "articulated_truck")]
    [VehicleModuleSupport(typeId: "land", categoryId: "semi_truck")]
    public class TrailerModule : VehicleEditorModuleBase
    {
        private Toggle _hasTrailer;
        private Toggle _poweredTrailer;
        private FloatField _trailerMass;
        private FloatField _trailerBrake;

        public override string ModuleId => "trailer";
        public override string DisplayName => "Trailer";
        public override int Priority => 53;
        public override bool RequiresVehicle => true;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style = { paddingLeft = 20, paddingRight = 20, paddingTop = 20, paddingBottom = 20 }
            };

            container.Add(new Label("Trailer Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            });

            _hasTrailer = new Toggle("Has Trailer");
            _hasTrailer.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_hasTrailer);

            _poweredTrailer = new Toggle("Powered Trailer");
            _poweredTrailer.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_poweredTrailer);

            _trailerMass = new FloatField("Trailer Mass (kg)");
            _trailerMass.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trailerMass);

            _trailerBrake = new FloatField("Trailer Brake Strength");
            _trailerBrake.RegisterValueChangedCallback(_ => SaveConfig());
            container.Add(_trailerBrake);

            return container;
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            var t = config.trailer;
            _hasTrailer.value = t.hasTrailer;
            _poweredTrailer.value = t.poweredTrailer;
            _trailerMass.value = t.trailerMass;
            _trailerBrake.value = t.trailerBrakeStrength;
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
            var t = _context.CurrentConfig.trailer;
            t.hasTrailer = _hasTrailer.value;
            t.poweredTrailer = _poweredTrailer.value;
            t.trailerMass = _trailerMass.value;
            t.trailerBrakeStrength = _trailerBrake.value;
            EditorUtility.SetDirty(_context.CurrentConfig);
            _context.NotifyConfigChanged(_context.CurrentConfig);
        }
    }
}
