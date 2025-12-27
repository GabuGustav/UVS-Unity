using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UVS.Editor.Core;
using System;

namespace UVS.Editor.Modules
{
    public class LowriderModule : VehicleEditorModuleBase
    {
        public override string ModuleId => "lowrider";
        public override string DisplayName => "Lowrider Hydraulics";
        public override int Priority => 50;
        public override bool RequiresVehicle => true;

        private Toggle _enableToggle;
        private FloatField _hopForceField;
        private FloatField _slamForceField;
        private FloatField _bounceFreqField;
        private FloatField _bounceAmpField;
        private FloatField _tiltSpeedField;
        private FloatField _maxTiltField;
        private FloatField _danceSpeedField;
        private Toggle _danceToggle;
        private Toggle _springsToggle;
        private IntegerField _coilCountField;
        private FloatField _springThicknessField;
        private ColorField _springColorField;

        private Button _hopAllBtn;
        private Button _frontHopBtn;
        private Button _rearHopBtn;
        private Button _leftTiltBtn;
        private Button _rightTiltBtn;
        private Button _slamBtn;
        private Button _danceBtn;

        private bool _isDancing;
        private float _danceTime;

        protected override VisualElement CreateModuleUI()
        {
            var root = new VisualElement
            {
                style =
                {
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 20,
                    paddingBottom = 20
                }
            };

            var title = new Label("Lowrider Hydraulics")
            {
                style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 15 }
            };
            root.Add(title);

            _enableToggle = new Toggle("Enable Lowrider Hydraulics")
            {
                style = { marginBottom = 15 }
            };
            _enableToggle.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.lowrider.enableHydraulics = evt.newValue;
                    UpdateUIState();
                    RefreshPreview();
                }
            });
            root.Add(_enableToggle);

            var bounceFoldout = new Foldout { text = "Bounce & Hop", value = true };
            _hopForceField = CreateFloatField(bounceFoldout, "Hop Force", 15000f);
            _slamForceField = CreateFloatField(bounceFoldout, "Slam Force", 20000f);
            _bounceFreqField = CreateFloatField(bounceFoldout, "Bounce Frequency", 4f);
            _bounceAmpField = CreateFloatField(bounceFoldout, "Bounce Amplitude", 0.4f);
            root.Add(bounceFoldout);

            var tiltFoldout = new Foldout { text = "Tilt & Dance", value = true };
            _tiltSpeedField = CreateFloatField(tiltFoldout, "Tilt Speed", 8f);
            _maxTiltField = CreateFloatField(tiltFoldout, "Max Tilt Angle", 15f);
            _danceToggle = new Toggle("Enable Auto Dance Mode") { value = true };
            _danceToggle.RegisterValueChangedCallback(evt => UpdateConfig());
            tiltFoldout.Add(_danceToggle);
            _danceSpeedField = CreateFloatField(tiltFoldout, "Dance Speed", 3f);
            root.Add(tiltFoldout);

            var springsFoldout = new Foldout { text = "Visual Coiled Springs", value = true };
            _springsToggle = new Toggle("Show Coiled Springs") { value = true };
            _springsToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig();
                RefreshPreview();
            });
            springsFoldout.Add(_springsToggle);

            _coilCountField = new IntegerField("Coil Count") { value = 8 };
            _coilCountField.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig();
                RefreshPreview();
            });
            springsFoldout.Add(_coilCountField);

            _springThicknessField = CreateFloatField(springsFoldout, "Spring Thickness", 0.08f);
            _springColorField = new ColorField("Spring Color") { value = new Color(1f, 0.8f, 0f) };
            _springColorField.RegisterValueChangedCallback(evt =>
            {
                UpdateConfig();
                RefreshPreview();
            });
            springsFoldout.Add(_springColorField);
            root.Add(springsFoldout);

            var testFoldout = new Foldout { text = "Live Preview Tests", value = true };
            _hopAllBtn = new Button(() => TestHop(1f)) { text = "Hop All Wheels" };
            testFoldout.Add(_hopAllBtn);
            _frontHopBtn = new Button(() => TestHop(1.3f)) { text = "Front Hop" };
            testFoldout.Add(_frontHopBtn);
            _rearHopBtn = new Button(() => TestHop(1.3f)) { text = "Rear Hop" };
            testFoldout.Add(_rearHopBtn);
            _leftTiltBtn = new Button(() => TestTilt(true)) { text = "Three-Wheel Left" };
            testFoldout.Add(_leftTiltBtn);
            _rightTiltBtn = new Button(() => TestTilt(false)) { text = "Three-Wheel Right" };
            testFoldout.Add(_rightTiltBtn);
            _slamBtn = new Button(() => TestHop(-1.5f)) { text = "Slam Down" };
            testFoldout.Add(_slamBtn);
            _danceBtn = new Button(ToggleDancePreview) { text = "Start Dance Mode" };
            testFoldout.Add(_danceBtn);
            root.Add(testFoldout);

            return root;
        }

        private FloatField CreateFloatField(VisualElement parent, string label, float defaultValue)
        {
            var field = new FloatField(label) { value = defaultValue };
            field.RegisterValueChangedCallback(evt => UpdateConfig());
            parent.Add(field);
            return field;
        }

        private void UpdateUIState()
        {
            bool enabled = _enableToggle?.value ?? false;
            var allElements = CreateModuleUI().Query<VisualElement>().ToList();
            foreach (var elem in allElements)
            {
                if (elem != _enableToggle)
                    elem.SetEnabled(enabled);
            }
        }

        private void RefreshPreview()
        {
            // Safe way: trigger repaint via the window
            if (EditorWindow.HasOpenInstances<VehicleEditorWindow>())
            {
                var window = EditorWindow.GetWindow<VehicleEditorWindow>();
                window.RefreshPreview();
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                LoadFromConfig(_context.CurrentConfig);
                UpdateUIState();
            }
        }

        private void LoadFromConfig(VehicleConfig config)
        {
            var l = config.lowrider;
            _enableToggle.value = l.enableHydraulics;
            _hopForceField.value = l.hopForce;
            _slamForceField.value = l.slamForce;
            _bounceFreqField.value = l.bounceFrequency;
            _bounceAmpField.value = l.bounceAmplitude;
            _tiltSpeedField.value = l.tiltSpeed;
            _maxTiltField.value = l.maxTiltAngle;
            _danceToggle.value = l.enableDanceMode;
            _danceSpeedField.value = l.danceSpeed;
            _springsToggle.value = l.showCoiledSprings;
            _coilCountField.value = (int)l.springCoilCount;
            _springThicknessField.value = l.springThickness;
            _springColorField.value = l.springColor;
        }

        private void UpdateConfig()
        {
            if (_context?.CurrentConfig == null) return;

            var l = _context.CurrentConfig.lowrider;
            l.hopForce = _hopForceField.value;
            l.slamForce = _slamForceField.value;
            l.bounceFrequency = _bounceFreqField.value;
            l.bounceAmplitude = _bounceAmpField.value;
            l.tiltSpeed = _tiltSpeedField.value;
            l.maxTiltAngle = _maxTiltField.value;
            l.enableDanceMode = _danceToggle.value;
            l.danceSpeed = _danceSpeedField.value;
            l.showCoiledSprings = _springsToggle.value;
            l.springCoilCount = _coilCountField.value;
            l.springThickness = _springThicknessField.value;
            l.springColor = _springColorField.value;

            _context.NotifyConfigChanged(_context.CurrentConfig);

            // ADD THESE TWO LINES TO FORCE SAVE TO DISK
            EditorUtility.SetDirty(_context.CurrentConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshPreview();
        }

        private void TestHop(float multiplier)
        {
            // Simple visual bounce via repaint loop
            RefreshPreview();
        }

        private void TestTilt(bool left)
        {
            RefreshPreview();
        }

        private void ToggleDancePreview()
        {
            _isDancing = !_isDancing;
            _danceBtn.text = _isDancing ? "Stop Dance Mode" : "Start Dance Mode";

            if (_isDancing)
                EditorApplication.update += DancePreviewUpdate;
            else
                EditorApplication.update -= DancePreviewUpdate;
        }

        private void DancePreviewUpdate()
        {
            _danceTime += Time.unscaledDeltaTime;
            float wave = Mathf.Sin(_danceTime * _danceSpeedField.value * 2f * Mathf.PI) * _bounceAmpField.value;
            RefreshPreview();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null && _context?.CurrentConfig == config)
            {
                LoadFromConfig(config);
                UpdateUIState();
            }
        }

        protected override void OnPrefabChanged(GameObject prefab)
        {
            // Nothing needed
        }

        protected override ValidationResult ValidateModule()
        {
            if (!_enableToggle.value) return ValidationResult.Success();
            if (_hopForceField.value <= 0) return ValidationResult.Error("Hop Force must be > 0");
            return ValidationResult.Success();
        }
    }
}