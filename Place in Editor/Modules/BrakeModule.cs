using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class BrakesModule : VehicleEditorModuleBase
    {
        private FloatField _frontDiscDiameterField;
        private FloatField _rearDiscDiameterField;
        private Toggle _absToggle;

        public override string ModuleId => "brakes";
        public override string DisplayName => "Brakes";
        public override int Priority => 60;

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

            var headerLabel = new Label("Brake Configuration")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            _frontDiscDiameterField = new FloatField("Front Disc Diameter (m)");
            _frontDiscDiameterField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.brakes.frontDiscDiameter = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_frontDiscDiameterField);

            _rearDiscDiameterField = new FloatField("Rear Disc Diameter (m)");
            _rearDiscDiameterField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.brakes.rearDiscDiameter = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_rearDiscDiameterField);

            _absToggle = new Toggle("Anti-lock Braking System (ABS)");
            _absToggle.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.brakes.abs = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_absToggle);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var brakes = _context.CurrentConfig.brakes;

            if (brakes.frontDiscDiameter <= 0)
                return ValidationResult.Error($"Invalid front disc diameter: {brakes.frontDiscDiameter}");

            if (brakes.rearDiscDiameter <= 0)
                return ValidationResult.Error($"Invalid rear disc diameter: {brakes.rearDiscDiameter}");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _frontDiscDiameterField.value = config.brakes.frontDiscDiameter;
                _rearDiscDiameterField.value = config.brakes.rearDiscDiameter;
                _absToggle.value = config.brakes.abs;
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                _frontDiscDiameterField.value = _context.CurrentConfig.brakes.frontDiscDiameter;
                _rearDiscDiameterField.value = _context.CurrentConfig.brakes.rearDiscDiameter;
                _absToggle.value = _context.CurrentConfig.brakes.abs;
            }
        }

        public override void OnModuleGUI() { }
    }
}