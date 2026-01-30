#if UNITY_2019_3_OR_NEWER
using ObjectField = UnityEditor.UIElements.ObjectField;
#endif

using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class EngineModule : VehicleEditorModuleBase
    {
        private FloatField _horsepowerField;
        private FloatField _enginRPMField;
        private FloatField _torqueField;
        private IntegerField _cylinderField;
        private FloatField _displacementField;
        private FloatField _redlineField;
        private FloatField _idleField;
        private EnumField _drivetrainField;
        private ObjectField _startClipField;
        private ObjectField _stopClipField;
        private ObjectField _idleClipField;
        private ObjectField _lowRpmClipField;
        private ObjectField _highRpmClipField;
        private ObjectField _shiftClipField;

        public override string ModuleId => "engine";
        public override string DisplayName => "Engine";
        public override int Priority => 40;

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

            var headerLabel = new Label("Engine Configuration")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            CreatePerformanceSection(container);
            CreateAudioSection(container);

            return container;
        }

        private void CreatePerformanceSection(VisualElement parent)
        {
            var performanceGroup = new Foldout { text = "Performance" };
            performanceGroup.AddToClassList("engine-foldout");

            _horsepowerField = new FloatField("Horsepower");
            _horsepowerField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.horsepower = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_horsepowerField);

            _enginRPMField = new FloatField("EngineRPM");
            _enginRPMField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.engineRPM = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_enginRPMField);

            _torqueField = new FloatField("Torque (Nm)");
            _torqueField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.torque = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_torqueField);

            _cylinderField = new IntegerField("Cylinder Count");
            _cylinderField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.cylinderCount = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_cylinderField);

            _displacementField = new FloatField("Displacement (L)");
            _displacementField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.displacement = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_displacementField);

            _redlineField = new FloatField("Redline RPM");
            _redlineField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.redlineRPM = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_redlineField);

            _idleField = new FloatField("Idle RPM");
            _idleField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.idleRPM = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_idleField);

            _drivetrainField = new EnumField("Drivetrain", VehicleConfig.EngineSettings.Drivetrain.RWD);
            _drivetrainField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.drivetrain = (VehicleConfig.EngineSettings.Drivetrain)evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            performanceGroup.Add(_drivetrainField);

            parent.Add(performanceGroup);
        }

        private void CreateAudioSection(VisualElement parent)
        {
            var audioGroup = new Foldout { text = "Engine Sounds" };
            audioGroup.AddToClassList("engine-foldout");

            _startClipField = new ObjectField("Start Sound") { objectType = typeof(AudioClip) };
            _startClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.startClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_startClipField);

            _stopClipField = new ObjectField("Stop Sound") { objectType = typeof(AudioClip) };
            _stopClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.stopClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_stopClipField);

            _idleClipField = new ObjectField("Idle Sound") { objectType = typeof(AudioClip) };
            _idleClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.idleClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_idleClipField);

            _lowRpmClipField = new ObjectField("Low RPM Sound") { objectType = typeof(AudioClip) };
            _lowRpmClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.lowRpmClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_lowRpmClipField);

            _highRpmClipField = new ObjectField("High RPM Sound") { objectType = typeof(AudioClip) };
            _highRpmClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.highRpmClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_highRpmClipField);

            _shiftClipField = new ObjectField("Shift Sound") { objectType = typeof(AudioClip) };
            _shiftClipField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.engine.shiftClip = evt.newValue as AudioClip;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            audioGroup.Add(_shiftClipField);

            parent.Add(audioGroup);
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var engine = _context.CurrentConfig.engine;

            if (engine.horsepower <= 0)
                return ValidationResult.Error($"Invalid horsepower: {engine.horsepower}");

            if (engine.torque <= 0)
                return ValidationResult.Error($"Invalid torque: {engine.torque}");

            if (engine.cylinderCount <= 0)
                return ValidationResult.Error($"Invalid cylinder count: {engine.cylinderCount}");

            if (engine.displacement <= 0)
                return ValidationResult.Error($"Invalid displacement: {engine.displacement}");

            if (engine.redlineRPM <= engine.idleRPM)
                return ValidationResult.Error("Redline RPM must be greater than idle RPM");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                LoadEngineSettings(config.engine);
            }
        }

        private void LoadEngineSettings(VehicleConfig.EngineSettings engine)
        {
            _horsepowerField.value = engine.horsepower;
            _enginRPMField.value = engine.engineRPM;
            _torqueField.value = engine.torque;
            _cylinderField.value = engine.cylinderCount;
            _displacementField.value = engine.displacement;
            _redlineField.value = engine.redlineRPM;
            _idleField.value = engine.idleRPM;
            _drivetrainField.value = engine.drivetrain;
            _startClipField.value = engine.startClip;
            _stopClipField.value = engine.stopClip;
            _idleClipField.value = engine.idleClip;
            _lowRpmClipField.value = engine.lowRpmClip;
            _highRpmClipField.value = engine.highRpmClip;
            _shiftClipField.value = engine.shiftClip;
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                LoadEngineSettings(_context.CurrentConfig.engine);
            }
        }

        public override void OnModuleGUI() { }
    }
}