using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class BodyModule : VehicleEditorModuleBase
    {
        private FloatField _massField;
        private FloatField _dragCoefficientField;
        private FloatField _frontalAreaField;

        public override string ModuleId => "body";
        public override string DisplayName => "Body";
        public override int Priority => 80;

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

            var headerLabel = new Label("Body Configuration")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            _massField = new FloatField("Mass (kg)");
            _massField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.body.mass = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_massField);

            _dragCoefficientField = new FloatField("Drag Coefficient");
            _dragCoefficientField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.body.dragCoefficient = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_dragCoefficientField);

            _frontalAreaField = new FloatField("Frontal Area (m²)");
            _frontalAreaField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.body.frontalArea = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_frontalAreaField);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var body = _context.CurrentConfig.body;

            if (body.mass <= 0)
                return ValidationResult.Error($"Invalid mass: {body.mass}");

            if (body.dragCoefficient <= 0)
                return ValidationResult.Error($"Invalid drag coefficient: {body.dragCoefficient}");

            if (body.frontalArea <= 0)
                return ValidationResult.Error($"Invalid frontal area: {body.frontalArea}");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;

            if (_massField != null) _massField.value = config.body.mass;
            if (_dragCoefficientField != null) _dragCoefficientField.value = config.body.dragCoefficient;
            if (_frontalAreaField != null) _frontalAreaField.value = config.body.frontalArea;
        }

        protected override void OnModuleActivated()
        {
            var cfg = _context?.CurrentConfig;
            if (cfg == null) return;

            if (_massField != null) _massField.value = cfg.body.mass;
            if (_dragCoefficientField != null) _dragCoefficientField.value = cfg.body.dragCoefficient;
            if (_frontalAreaField != null) _frontalAreaField.value = cfg.body.frontalArea;
        }

        public override void OnModuleGUI() { }
    }
}
