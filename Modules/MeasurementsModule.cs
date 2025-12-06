using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using UVS.Editor.Core;
using UVS.Modules;

namespace UVS.Editor.Modules
{
    public class MeasurementsModule : VehicleEditorModuleBase
    {
        private FloatField _lengthField;
        private FloatField _widthField;
        private FloatField _heightField;
        private FloatField _wheelbaseField;
        private FloatField _frontTrackField;
        private FloatField _rearTrackField;
        private FloatField _groundClearanceField;
        private FloatField _rideHeightField;
        private Vector3Field _centerOfMassField;
        private Button _measureButton;
        private Button _exportButton;

        public override string ModuleId => "measurements";
        public override string DisplayName => "Measurements";
        public override int Priority => 50;

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

            var headerLabel = new Label("Vehicle Measurements")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(headerLabel);

            CreateControlButtons(container);
            CreateMeasurementsSection(container);

            return container;
        }

        private void CreateControlButtons(VisualElement parent)
        {
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginBottom = 15
                }
            };

            _measureButton = new Button(MeasureVehicle)
            {
                text = "Measure Vehicle",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_measureButton);

            _exportButton = new Button(ExportMeasurements)
            {
                text = "Export Measurements",
                style = { marginRight = 10, marginBottom = 5 }
            };
            buttonContainer.Add(_exportButton);

            parent.Add(buttonContainer);
        }

        private void CreateMeasurementsSection(VisualElement parent)
        {
            var measurementsGroup = new Foldout { text = "Vehicle Dimensions" };
            measurementsGroup.AddToClassList("engine-foldout");

            _lengthField = new FloatField("Length (m)");
            _lengthField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.length = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_lengthField);

            _widthField = new FloatField("Width (m)");
            _widthField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.width = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_widthField);

            _heightField = new FloatField("Height (m)");
            _heightField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.height = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_heightField);

            _wheelbaseField = new FloatField("Wheelbase (m)");
            _wheelbaseField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.wheelbase = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_wheelbaseField);

            _frontTrackField = new FloatField("Front Track Width (m)");
            _frontTrackField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.frontTrackWidth = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_frontTrackField);

            _rearTrackField = new FloatField("Rear Track Width (m)");
            _rearTrackField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.rearTrackWidth = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_rearTrackField);

            _groundClearanceField = new FloatField("Ground Clearance (m)");
            _groundClearanceField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.groundClearance = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_groundClearanceField);

            _rideHeightField = new FloatField("Ride Height (m)");
            _rideHeightField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.rideHeight = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_rideHeightField);

            _centerOfMassField = new Vector3Field("Center of Mass Offset");
            _centerOfMassField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.measurements.centerOfMassEstimate = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            measurementsGroup.Add(_centerOfMassField);

            parent.Add(measurementsGroup);
        }

        private void MeasureVehicle()
        {
            if (_context?.CurrentConfig == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            if (_context.LastScan == null || !_context.LastScan.ContainsKey(VehicleConfig.VehiclePartType.Wheel))
            {
                LogError("No wheels found. Please scan parts first.");
                return;
            }

            try
            {
                VehicleMeasurementModule.Measure(_context.CurrentConfig, _context.LastScan[VehicleConfig.VehiclePartType.Wheel]);
                LoadMeasurements(_context.CurrentConfig.measurements);
                LogMessage("Vehicle measurements updated successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to measure vehicle: {ex.Message}");
            }
        }

        private void ExportMeasurements()
        {
            if (_context?.CurrentConfig == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            var measurements = _context.CurrentConfig.measurements;
            var export = new MeasurementExport
            {
                length = measurements.length,
                width = measurements.width,
                height = measurements.height,
                wheelbase = measurements.wheelbase,
                frontTrackWidth = measurements.frontTrackWidth,
                rearTrackWidth = measurements.rearTrackWidth,
                groundClearance = measurements.groundClearance,
                rideHeight = measurements.rideHeight,
                centerOfMassEstimate = measurements.centerOfMassEstimate
            };

            string json = JsonUtility.ToJson(export, true);
            string path = EditorUtility.SaveFilePanel(
                "Export Measurements",
                Application.dataPath,
                _context.CurrentConfig.name + "_Measurements",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                LogMessage($"Measurements exported to: {path}");
            }
        }

        private void LoadMeasurements(VehicleConfig.VehicleMeasurements measurements)
        {
            _lengthField.value = measurements.length;
            _widthField.value = measurements.width;
            _heightField.value = measurements.height;
            _wheelbaseField.value = measurements.wheelbase;
            _frontTrackField.value = measurements.frontTrackWidth;
            _rearTrackField.value = measurements.rearTrackWidth;
            _groundClearanceField.value = measurements.groundClearance;
            _rideHeightField.value = measurements.rideHeight;
            _centerOfMassField.value = measurements.centerOfMassEstimate;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            var measurements = _context.CurrentConfig.measurements;

            if (measurements.length <= 0)
                return ValidationResult.Error($"Invalid length: {measurements.length}");

            if (measurements.width <= 0)
                return ValidationResult.Error($"Invalid width: {measurements.width}");

            if (measurements.height <= 0)
                return ValidationResult.Error($"Invalid height: {measurements.height}");

            if (measurements.wheelbase <= 0)
                return ValidationResult.Error($"Invalid wheelbase: {measurements.wheelbase}");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                LoadMeasurements(config.measurements);
            }
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
            {
                LoadMeasurements(_context.CurrentConfig.measurements);
            }
        }

        public override void OnModuleGUI() { }

        [System.Serializable]
        public class MeasurementExport
        {
            public float length;
            public float width;
            public float height;
            public float wheelbase;
            public float frontTrackWidth;
            public float rearTrackWidth;
            public float groundClearance;
            public float rideHeight;
            public Vector3 centerOfMassEstimate;
        }
    }
}