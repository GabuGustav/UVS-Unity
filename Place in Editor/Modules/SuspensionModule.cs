using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class SuspensionModule : VehicleEditorModuleBase
    {
        private FloatField _springStiffnessField;
        private FloatField _damperStiffnessField;
        private FloatField _antiRollBarStiffnessField;

        public override string ModuleId => "suspension";
        public override string DisplayName => "Suspension";
        public override int Priority => 70;

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

            var headerLabel = new Label("Suspension Configuration")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            _springStiffnessField = new FloatField("Spring Stiffness");
            _springStiffnessField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.suspension.springStiffness = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_springStiffnessField);

            _damperStiffnessField = new FloatField("Damper Stiffness");
            _damperStiffnessField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.suspension.damperStiffness = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_damperStiffnessField);

            _antiRollBarStiffnessField = new FloatField("Anti-Roll Bar Stiffness");
            _antiRollBarStiffnessField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.suspension.antiRollBarStiffness = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            container.Add(_antiRollBarStiffnessField);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var suspension = _context.CurrentConfig.suspension;

            if (suspension.springStiffness <= 0)
                return ValidationResult.Error($"Invalid spring stiffness: {suspension.springStiffness}");

            if (suspension.damperStiffness <= 0)
                return ValidationResult.Error($"Invalid damper stiffness: {suspension.damperStiffness}");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _springStiffnessField.value = config.suspension.springStiffness;
                _damperStiffnessField.value = config.suspension.damperStiffness;
                _antiRollBarStiffnessField.value = config.suspension.antiRollBarStiffness;
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                _springStiffnessField.value = _context.CurrentConfig.suspension.springStiffness;
                _damperStiffnessField.value = _context.CurrentConfig.suspension.damperStiffness;
                _antiRollBarStiffnessField.value = _context.CurrentConfig.suspension.antiRollBarStiffness;
            }
        }

        public override void OnModuleGUI() { }
    }
}