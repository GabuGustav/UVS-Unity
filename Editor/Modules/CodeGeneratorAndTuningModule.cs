using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UVS.Editor.Core;
using UVS.Modules;

namespace UVS.Editor.Modules
{
    public class CodeGeneratorAndTuningModule : VehicleEditorModuleBase
    {
        public override string ModuleId => "codegen_tuning";
        public override string DisplayName => "Code Generator & Tuning";
        public override int Priority => 35;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;

        private readonly List<Action<VehicleConfig>> _loaders = new();

        private ScrollView _scroll;

        private Foldout _codeGenSection;
        private Foldout _engineSection;
        private Foldout _transmissionSection;
        private Foldout _turboSection;
        private Foldout _fuelSection;
        private Foldout _steeringSection;
        private Foldout _brakesSection;
        private Foldout _suspensionSection;
        private Foldout _bodySection;
        private Foldout _wheelsSection;
        private Foldout _measurementsSection;
        private Foldout _electronicsSection;
        private Foldout _audioSection;
        private Foldout _damageSection;
        private Foldout _performanceSection;
        private Foldout _lowriderSection;

        private VisualElement _gearRatiosContainer;
        private IntegerField _gearCountField;

        private VisualElement _wheelsContent;
        private Label _wheelsWarningLabel;
        private Button _scanWheelsButton;
        private Button _applyCollidersButton;
        private Button _measureButton;
        private Button _exportMeasurementsButton;

        private VisualElement _performanceProfilesContainer;

        protected override VisualElement CreateModuleUI()
        {
            _scroll = new ScrollView
            {
                style = { flexGrow = 1 }
            };

            _codeGenSection = CreateSection("Code Generator", true);
            CreateCodeGenSection(_codeGenSection);
            _scroll.Add(_codeGenSection);

            _engineSection = CreateSection("Engine", true);
            CreateEngineSection(_engineSection);
            _scroll.Add(_engineSection);

            _transmissionSection = CreateSection("Transmission", false);
            CreateTransmissionSection(_transmissionSection);
            _scroll.Add(_transmissionSection);

            _turboSection = CreateSection("Turbo", false);
            CreateTurboSection(_turboSection);
            _scroll.Add(_turboSection);

            _fuelSection = CreateSection("Fuel System", false);
            CreateFuelSection(_fuelSection);
            _scroll.Add(_fuelSection);

            _steeringSection = CreateSection("Steering", false);
            CreateSteeringSection(_steeringSection);
            _scroll.Add(_steeringSection);

            _brakesSection = CreateSection("Brakes", false);
            CreateBrakesSection(_brakesSection);
            _scroll.Add(_brakesSection);

            _suspensionSection = CreateSection("Suspension", false);
            CreateSuspensionSection(_suspensionSection);
            _scroll.Add(_suspensionSection);

            _bodySection = CreateSection("Body", false);
            CreateBodySection(_bodySection);
            _scroll.Add(_bodySection);

            _wheelsSection = CreateSection("Wheels & Colliders", false);
            CreateWheelsSection(_wheelsSection);
            _scroll.Add(_wheelsSection);

            _measurementsSection = CreateSection("Measurements", false);
            CreateMeasurementsSection(_measurementsSection);
            _scroll.Add(_measurementsSection);

            _electronicsSection = CreateSection("Electronics", false);
            CreateElectronicsSection(_electronicsSection);
            _scroll.Add(_electronicsSection);

            _audioSection = CreateSection("Audio Mix", false);
            CreateAudioSection(_audioSection);
            _scroll.Add(_audioSection);

            _damageSection = CreateSection("Damage", false);
            CreateDamageSection(_damageSection);
            _scroll.Add(_damageSection);

            _performanceSection = CreateSection("Performance Profiles", false);
            CreatePerformanceSection(_performanceSection);
            _scroll.Add(_performanceSection);

            _lowriderSection = CreateSection("Lowrider Hydraulics", false);
            CreateLowriderSection(_lowriderSection);
            _scroll.Add(_lowriderSection);

            return _scroll;
        }

        protected override void OnModuleActivated()
        {
            var cfg = _context?.CurrentConfig;
            if (cfg == null) return;
            LoadFromConfig(cfg);
            UpdateSectionVisibility(cfg);
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            LoadFromConfig(config);
            UpdateSectionVisibility(config);
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context?.CurrentConfig == null)
                return ValidationResult.Warning("No vehicle loaded");

            return ValidationResult.Success();
        }

        public override void OnModuleGUI() { }

        private void LoadFromConfig(VehicleConfig config)
        {
            foreach (var loader in _loaders)
                loader(config);

            RebuildGearRatios(config);
            RebuildPerformanceProfiles(config);
            DisplayWheels(config.wheels);
            UpdateWheelWarnings(config);
        }

        private void UpdateSectionVisibility(VehicleConfig config)
        {
            bool isLand = config.vehicleType == VehicleConfig.VehicleType.Land;
            bool isLowrider = isLand &&
                              config.landCategory == VehicleConfig.LandVehicleCategory.Specialized &&
                              config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Lowrider;

            bool hasWheels = HasPartType(VehicleConfig.VehiclePartType.Wheel) ||
                             (config.wheels != null && config.wheels.Count > 0);

            _wheelsSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _measurementsSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _brakesSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _suspensionSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _steeringSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _transmissionSection.style.display = isLand ? DisplayStyle.Flex : DisplayStyle.None;
            _lowriderSection.style.display = isLowrider ? DisplayStyle.Flex : DisplayStyle.None;

            _wheelsSection.SetEnabled(hasWheels);
            _measurementsSection.SetEnabled(hasWheels);
        }

        private bool HasPartType(VehicleConfig.VehiclePartType type)
        {
            if (_context?.LastScan != null &&
                _context.LastScan.TryGetValue(type, out var list) &&
                list != null && list.Count > 0)
                return true;

            if (_context?.CurrentConfig?.partClassifications != null &&
                _context.CurrentConfig.partClassifications.Any(p => p.partType == type))
                return true;

            return false;
        }

        private Foldout CreateSection(string title, bool expanded)
        {
            return new Foldout
            {
                text = title,
                value = expanded,
                style =
                {
                    marginBottom = 6,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 6,
                    paddingBottom = 6
                }
            };
        }

        private void CreateCodeGenSection(Foldout section)
        {
            var description = new Label("Generate runtime code and sync scripts for the selected vehicle.");
            section.contentContainer.Add(description);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            var openBtn = new Button(() => CodeGeneratorWindow.ShowWindow())
            {
                text = "Open Code Generator",
                style = { marginRight = 6, marginTop = 4 }
            };
            row.Add(openBtn);
            section.contentContainer.Add(row);
        }

        private void CreateEngineSection(Foldout section)
        {
            BindFloat(section, "Horsepower", c => c.engine.horsepower, (c, v) => c.engine.horsepower = v);
            BindFloat(section, "Torque (Nm)", c => c.engine.torque, (c, v) => c.engine.torque = v);
            BindInt(section, "Cylinder Count", c => c.engine.cylinderCount, (c, v) => c.engine.cylinderCount = v);
            BindFloat(section, "Displacement (L)", c => c.engine.displacement, (c, v) => c.engine.displacement = v);
            BindFloat(section, "Redline RPM", c => c.engine.redlineRPM, (c, v) => c.engine.redlineRPM = v);
            BindFloat(section, "Idle RPM", c => c.engine.idleRPM, (c, v) => c.engine.idleRPM = v);
            BindFloat(section, "Engine RPM", c => c.engine.engineRPM, (c, v) => c.engine.engineRPM = v);
            BindEnum(section, "Drivetrain", c => c.engine.drivetrain, (c, v) => c.engine.drivetrain = v);

            BindAudioClip(section, "Start Sound", c => c.engine.startClip, (c, v) => c.engine.startClip = v);
            BindAudioClip(section, "Stop Sound", c => c.engine.stopClip, (c, v) => c.engine.stopClip = v);
            BindAudioClip(section, "Idle Sound", c => c.engine.idleClip, (c, v) => c.engine.idleClip = v);
            BindAudioClip(section, "Low RPM Sound", c => c.engine.lowRpmClip, (c, v) => c.engine.lowRpmClip = v);
            BindAudioClip(section, "High RPM Sound", c => c.engine.highRpmClip, (c, v) => c.engine.highRpmClip = v);
            BindAudioClip(section, "Shift Sound", c => c.engine.shiftClip, (c, v) => c.engine.shiftClip = v);
        }

        private void CreateTransmissionSection(Foldout section)
        {
            BindEnum(section, "Transmission Type", c => c.transmission.type, (c, v) => c.transmission.type = v);

            _gearCountField = BindInt(section, "Gear Count", c => c.transmission.gearCount, (c, v) =>
            {
                c.transmission.gearCount = Mathf.Max(1, v);
                EnsureGearRatios(c);
                RebuildGearRatios(c);
            });

            BindFloat(section, "Final Drive Ratio", c => c.transmission.finalDriveRatio, (c, v) => c.transmission.finalDriveRatio = v);
            BindFloat(section, "Reverse Gear Ratio", c => c.transmission.reverseGearRatio, (c, v) => c.transmission.reverseGearRatio = v);
            BindFloat(section, "Shift Time", c => c.transmission.shiftTime, (c, v) => c.transmission.shiftTime = v);

            var ratiosLabel = new Label("Gear Ratios");
            section.contentContainer.Add(ratiosLabel);

            _gearRatiosContainer = new VisualElement { style = { marginLeft = 6 } };
            section.contentContainer.Add(_gearRatiosContainer);
        }

        private void EnsureGearRatios(VehicleConfig config)
        {
            int count = Mathf.Max(1, config.transmission.gearCount);
            if (config.transmission.gearRatios == null || config.transmission.gearRatios.Length != count)
            {
                var newRatios = new float[count];
                if (config.transmission.gearRatios != null)
                {
                    for (int i = 0; i < Math.Min(count, config.transmission.gearRatios.Length); i++)
                        newRatios[i] = config.transmission.gearRatios[i];
                }
                else
                {
                    for (int i = 0; i < count; i++)
                        newRatios[i] = 1f;
                }
                config.transmission.gearRatios = newRatios;
                EditorUtility.SetDirty(config);
            }
        }

        private void RebuildGearRatios(VehicleConfig config)
        {
            if (_gearRatiosContainer == null) return;

            _gearRatiosContainer.Clear();
            EnsureGearRatios(config);

            for (int i = 0; i < config.transmission.gearRatios.Length; i++)
            {
                int idx = i;
                var field = new FloatField($"Gear {i + 1}")
                {
                    value = config.transmission.gearRatios[i]
                };
                field.RegisterValueChangedCallback(evt =>
                {
                    config.transmission.gearRatios[idx] = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                _gearRatiosContainer.Add(field);
            }
        }

        private void CreateTurboSection(Foldout section)
        {
            BindToggle(section, "Has Turbo", c => c.turbo.hasTurbo, (c, v) => c.turbo.hasTurbo = v);
            BindFloat(section, "Boost Amount", c => c.turbo.boostAmount, (c, v) => c.turbo.boostAmount = v);
            BindFloat(section, "Spool Time", c => c.turbo.spoolTime, (c, v) => c.turbo.spoolTime = v);
            BindFloat(section, "Max Boost Pressure", c => c.turbo.maxBoostPressure, (c, v) => c.turbo.maxBoostPressure = v);
            BindFloat(section, "Boost Threshold RPM", c => c.turbo.boostThresholdRPM, (c, v) => c.turbo.boostThresholdRPM = v);
            BindAudioClip(section, "Whistle Clip", c => c.turbo.whistleClip, (c, v) => c.turbo.whistleClip = v);
            BindAudioClip(section, "Blowoff Clip", c => c.turbo.blowoffClip, (c, v) => c.turbo.blowoffClip = v);
        }

        private void CreateFuelSection(Foldout section)
        {
            BindFloat(section, "Fuel Capacity", c => c.fuelSystem.fuelCapacity, (c, v) => c.fuelSystem.fuelCapacity = v);
            BindFloat(section, "Current Fuel", c => c.fuelSystem.currentFuel, (c, v) => c.fuelSystem.currentFuel = v);
            BindFloat(section, "Consumption Rate", c => c.fuelSystem.fuelConsumptionRate, (c, v) => c.fuelSystem.fuelConsumptionRate = v);
            BindEnum(section, "Fuel Type", c => c.fuelSystem.fuelType, (c, v) => c.fuelSystem.fuelType = v);
            BindFloat(section, "Octane Rating", c => c.fuelSystem.octaneRating, (c, v) => c.fuelSystem.octaneRating = v);
            BindFloat(section, "Fuel Density", c => c.fuelSystem.fuelDensity, (c, v) => c.fuelSystem.fuelDensity = v);

            BindText(section, "Fuel Tank Path", c => c.fuelSystem.fuelTankPath, (c, v) => c.fuelSystem.fuelTankPath = v);
            BindText(section, "Fuel Pump Path", c => c.fuelSystem.fuelPumpPath, (c, v) => c.fuelSystem.fuelPumpPath = v);
            BindText(section, "Fuel Filter Path", c => c.fuelSystem.fuelFilterPath, (c, v) => c.fuelSystem.fuelFilterPath = v);
            BindText(section, "Fuel Injectors Path", c => c.fuelSystem.fuelInjectorsPath, (c, v) => c.fuelSystem.fuelInjectorsPath = v);
        }

        private void CreateSteeringSection(Foldout section)
        {
            BindFloat(section, "Max Steering Angle", c => c.steering.maxSteeringAngle, (c, v) => c.steering.maxSteeringAngle = v);
            BindFloat(section, "Steering Ratio", c => c.steering.steeringRatio, (c, v) => c.steering.steeringRatio = v);
            BindToggle(section, "Power Steering", c => c.steering.powerSteering, (c, v) => c.steering.powerSteering = v);
            BindFloat(section, "Steering Assist", c => c.steering.steeringAssist, (c, v) => c.steering.steeringAssist = v);
        }

        private void CreateBrakesSection(Foldout section)
        {
            BindFloat(section, "Front Disc Diameter (m)", c => c.brakes.frontDiscDiameter, (c, v) => c.brakes.frontDiscDiameter = v);
            BindFloat(section, "Rear Disc Diameter (m)", c => c.brakes.rearDiscDiameter, (c, v) => c.brakes.rearDiscDiameter = v);
            BindToggle(section, "ABS", c => c.brakes.abs, (c, v) => c.brakes.abs = v);
            BindFloat(section, "Brake Bias", c => c.brakes.brakeBias, (c, v) => c.brakes.brakeBias = v);
        }

        private void CreateSuspensionSection(Foldout section)
        {
            BindFloat(section, "Spring Stiffness", c => c.suspension.springStiffness, (c, v) => c.suspension.springStiffness = v);
            BindFloat(section, "Damper Stiffness", c => c.suspension.damperStiffness, (c, v) => c.suspension.damperStiffness = v);
            BindFloat(section, "Anti-Roll Bar Stiffness", c => c.suspension.antiRollBarStiffness, (c, v) => c.suspension.antiRollBarStiffness = v);
            BindFloat(section, "Suspension Travel", c => c.suspension.suspensionTravel, (c, v) => c.suspension.suspensionTravel = v);
            BindFloat(section, "Suspension Distance", c => c.suspension.suspensionDistance, (c, v) => c.suspension.suspensionDistance = v);
        }

        private void CreateBodySection(Foldout section)
        {
            BindFloat(section, "Mass (kg)", c => c.body.mass, (c, v) => c.body.mass = v);
            BindFloat(section, "Drag Coefficient", c => c.body.dragCoefficient, (c, v) => c.body.dragCoefficient = v);
            BindFloat(section, "Frontal Area (m2)", c => c.body.frontalArea, (c, v) => c.body.frontalArea = v);
            BindVector3(section, "Center of Mass Offset", c => c.body.centerOfMassOffset, (c, v) => c.body.centerOfMassOffset = v);
        }

        private void CreateWheelsSection(Foldout section)
        {
            _wheelsWarningLabel = new Label("Scan parts to detect wheels before configuring.")
            {
                style = { color = Color.yellow, marginBottom = 4 }
            };
            section.contentContainer.Add(_wheelsWarningLabel);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };

            _scanWheelsButton = new Button(ScanWheels) { text = "Scan Wheels", style = { marginRight = 6 } };
            _applyCollidersButton = new Button(ApplyWheelColliders) { text = "Apply Colliders", style = { marginRight = 6 } };
            _measureButton = new Button(MeasureVehicle) { text = "Measure Vehicle", style = { marginRight = 6 } };

            buttonRow.Add(_scanWheelsButton);
            buttonRow.Add(_applyCollidersButton);
            buttonRow.Add(_measureButton);

            section.contentContainer.Add(buttonRow);

            _wheelsContent = new ScrollView
            {
                style =
                {
                    height = 300,
                    marginTop = 6,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10
                }
            };
            section.contentContainer.Add(_wheelsContent);
        }

        private void UpdateWheelWarnings(VehicleConfig config)
        {
            bool hasWheels = HasPartType(VehicleConfig.VehiclePartType.Wheel) ||
                             (config.wheels != null && config.wheels.Count > 0);
            if (_wheelsWarningLabel != null)
                _wheelsWarningLabel.style.display = hasWheels ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void CreateMeasurementsSection(Foldout section)
        {
            _exportMeasurementsButton = new Button(ExportMeasurements) { text = "Export Measurements" };
            section.contentContainer.Add(_exportMeasurementsButton);

            BindFloat(section, "Length (m)", c => c.measurements.length, (c, v) => c.measurements.length = v);
            BindFloat(section, "Width (m)", c => c.measurements.width, (c, v) => c.measurements.width = v);
            BindFloat(section, "Height (m)", c => c.measurements.height, (c, v) => c.measurements.height = v);
            BindFloat(section, "Wheelbase (m)", c => c.measurements.wheelbase, (c, v) => c.measurements.wheelbase = v);
            BindFloat(section, "Front Track Width (m)", c => c.measurements.frontTrackWidth, (c, v) => c.measurements.frontTrackWidth = v);
            BindFloat(section, "Rear Track Width (m)", c => c.measurements.rearTrackWidth, (c, v) => c.measurements.rearTrackWidth = v);
            BindFloat(section, "Ground Clearance (m)", c => c.measurements.groundClearance, (c, v) => c.measurements.groundClearance = v);
            BindFloat(section, "Ride Height (m)", c => c.measurements.rideHeight, (c, v) => c.measurements.rideHeight = v);
            BindVector3(section, "Center of Mass Estimate", c => c.measurements.centerOfMassEstimate, (c, v) => c.measurements.centerOfMassEstimate = v);
        }

        private void CreateElectronicsSection(Foldout section)
        {
            BindToggle(section, "ABS", c => c.electronics.hasABS, (c, v) => c.electronics.hasABS = v);
            BindToggle(section, "TCS", c => c.electronics.hasTCS, (c, v) => c.electronics.hasTCS = v);
            BindToggle(section, "ESP", c => c.electronics.hasESP, (c, v) => c.electronics.hasESP = v);
            BindToggle(section, "Launch Control", c => c.electronics.hasLaunchControl, (c, v) => c.electronics.hasLaunchControl = v);
            BindToggle(section, "Cruise Control", c => c.electronics.hasCruiseControl, (c, v) => c.electronics.hasCruiseControl = v);
        }

        private void CreateAudioSection(Foldout section)
        {
            BindFloat(section, "Engine Volume", c => c.audioMix.engineVolume, (c, v) => c.audioMix.engineVolume = v);
            BindFloat(section, "Turbo Volume", c => c.audioMix.turboVolume, (c, v) => c.audioMix.turboVolume = v);
            BindFloat(section, "Exhaust Volume", c => c.audioMix.exhaustVolume, (c, v) => c.audioMix.exhaustVolume = v);
            BindFloat(section, "Tire Volume", c => c.audioMix.tireVolume, (c, v) => c.audioMix.tireVolume = v);

            BindAudioClip(section, "Collision Clip", c => c.audioMix.collisionClip, (c, v) => c.audioMix.collisionClip = v);
            BindAudioClip(section, "Gear Grind Clip", c => c.audioMix.gearGrindClip, (c, v) => c.audioMix.gearGrindClip = v);
        }

        private void CreateDamageSection(Foldout section)
        {
            BindToggle(section, "Enable Damage", c => c.damage.enableDamage, (c, v) => c.damage.enableDamage = v);
            BindFloat(section, "Max Health", c => c.damage.maxHealth, (c, v) => c.damage.maxHealth = v);
            BindFloat(section, "Collision Threshold", c => c.damage.collisionThreshold, (c, v) => c.damage.collisionThreshold = v);
            BindFloat(section, "Damage Multiplier", c => c.damage.damageMultiplier, (c, v) => c.damage.damageMultiplier = v);
            BindToggle(section, "Visual Damage", c => c.damage.visualDamage, (c, v) => c.damage.visualDamage = v);
        }

        private void CreatePerformanceSection(Foldout section)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            var addButton = new Button(AddPerformanceProfile) { text = "Add Profile", style = { marginRight = 6 } };
            row.Add(addButton);
            section.contentContainer.Add(row);

            _performanceProfilesContainer = new VisualElement { style = { marginTop = 6 } };
            section.contentContainer.Add(_performanceProfilesContainer);
        }

        private void CreateLowriderSection(Foldout section)
        {
            BindToggle(section, "Enable Hydraulics", c => c.lowrider.enableHydraulics, (c, v) => c.lowrider.enableHydraulics = v);
            BindFloat(section, "Hop Force", c => c.lowrider.hopForce, (c, v) => c.lowrider.hopForce = v);
            BindFloat(section, "Slam Force", c => c.lowrider.slamForce, (c, v) => c.lowrider.slamForce = v);
            BindFloat(section, "Tilt Speed", c => c.lowrider.tiltSpeed, (c, v) => c.lowrider.tiltSpeed = v);
            BindFloat(section, "Max Tilt Angle", c => c.lowrider.maxTiltAngle, (c, v) => c.lowrider.maxTiltAngle = v);
            BindToggle(section, "Enable Dance Mode", c => c.lowrider.enableDanceMode, (c, v) => c.lowrider.enableDanceMode = v);
            BindFloat(section, "Dance Speed", c => c.lowrider.danceSpeed, (c, v) => c.lowrider.danceSpeed = v);
            BindFloat(section, "Bounce Amplitude", c => c.lowrider.bounceAmplitude, (c, v) => c.lowrider.bounceAmplitude = v);
            BindFloat(section, "Bounce Frequency", c => c.lowrider.bounceFrequency, (c, v) => c.lowrider.bounceFrequency = v);
            BindInt(section, "Spring Coil Count", c => c.lowrider.springCoilCount, (c, v) => c.lowrider.springCoilCount = v);
            BindToggle(section, "Show Coiled Springs", c => c.lowrider.showCoiledSprings, (c, v) => c.lowrider.showCoiledSprings = v);
            BindFloat(section, "Spring Thickness", c => c.lowrider.springThickness, (c, v) => c.lowrider.springThickness = v);
            BindColor(section, "Spring Color", c => c.lowrider.springColor, (c, v) => c.lowrider.springColor = v);
        }

        private void ScanWheels()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded. Please select a vehicle first.");
                return;
            }

            if (_context.LastScan == null || !_context.LastScan.ContainsKey(VehicleConfig.VehiclePartType.Wheel))
            {
                LogError("No wheels found in last scan. Please scan parts first.");
                return;
            }

            var wheelTransforms = _context.LastScan[VehicleConfig.VehiclePartType.Wheel];
            var wheelSettings = new List<VehicleConfig.WheelSettings>();

            foreach (var wheel in wheelTransforms)
            {
                if (wheel == null) continue;

                var wheelSetting = new VehicleConfig.WheelSettings
                {
                    partPath = GetTransformPath(wheel),
                    radius = 0.35f,
                    width = 0.2f,
                    SuspensionDistance = 0.3f,
                    localPosition = wheel.localPosition
                };

                wheelSetting.isSteering = wheelSetting.localPosition.z > 0;
                wheelSetting.isPowered = wheelSetting.localPosition.z < 0;

                wheelSettings.Add(wheelSetting);
            }

            _context.CurrentConfig.wheels = wheelSettings;
            EditorUtility.SetDirty(_context.CurrentConfig);
            AssetDatabase.SaveAssets();

            DisplayWheels(wheelSettings);
            UpdateWheelWarnings(_context.CurrentConfig);
            LogMessage($"Scanned {wheelSettings.Count} wheels successfully.");
        }

        private void DisplayWheels(List<VehicleConfig.WheelSettings> wheelSettings)
        {
            if (_wheelsContent == null) return;

            _wheelsContent.Clear();

            if (wheelSettings == null || wheelSettings.Count == 0)
            {
                var noWheelsLabel = new Label("No wheels configured. Click 'Scan Wheels' to detect wheels.")
                {
                    style = { color = Color.yellow }
                };
                _wheelsContent.Add(noWheelsLabel);
                return;
            }

            var frontWheels = wheelSettings.Where(w => w.isSteering).ToList();
            var rearWheels = wheelSettings.Where(w => w.isPowered).ToList();

            if (frontWheels.Count > 0)
            {
                var frontGroup = new Foldout { text = $"Front Wheels ({frontWheels.Count})" };
                foreach (var wheel in frontWheels)
                    frontGroup.Add(CreateWheelRow(wheel));
                _wheelsContent.Add(frontGroup);
            }

            if (rearWheels.Count > 0)
            {
                var rearGroup = new Foldout { text = $"Rear Wheels ({rearWheels.Count})" };
                foreach (var wheel in rearWheels)
                    rearGroup.Add(CreateWheelRow(wheel));
                _wheelsContent.Add(rearGroup);
            }
        }

        private VisualElement CreateWheelRow(VehicleConfig.WheelSettings wheelSetting)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 5,
                    paddingBottom = 5,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
                }
            };

            var nameLabel = new Label(GetLastPathPart(wheelSetting.partPath))
            {
                style = { width = 120, color = Color.white }
            };
            row.Add(nameLabel);

            var radiusField = new FloatField("Radius") { value = wheelSetting.radius, style = { width = 90 } };
            radiusField.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.radius = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(radiusField);

            var widthField = new FloatField("Width") { value = wheelSetting.width, style = { width = 90 } };
            widthField.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.width = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(widthField);

            var suspensionField = new FloatField("Suspension") { value = wheelSetting.SuspensionDistance, style = { width = 110 } };
            suspensionField.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.SuspensionDistance = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(suspensionField);

            var steeringToggle = new Toggle("Steering") { value = wheelSetting.isSteering };
            steeringToggle.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.isSteering = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(steeringToggle);

            var poweredToggle = new Toggle("Powered") { value = wheelSetting.isPowered };
            poweredToggle.RegisterValueChangedCallback(evt =>
            {
                wheelSetting.isPowered = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
            });
            row.Add(poweredToggle);

            return row;
        }

        private void ApplyWheelColliders()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            if (_context.CurrentConfig.wheels == null || _context.CurrentConfig.wheels.Count == 0)
            {
                LogError("No wheel settings found. Please scan wheels first.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);

            try
            {
                if (!prefabRoot.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = prefabRoot.AddComponent<Rigidbody>();
                    LogMessage("Added Rigidbody to vehicle");
                }

                rb.mass = _context.CurrentConfig.body.mass > 0 ? _context.CurrentConfig.body.mass : 1200f;
                rb.linearDamping = _context.CurrentConfig.body.dragCoefficient;
                rb.angularDamping = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.centerOfMass = _context.CurrentConfig.measurements.centerOfMassEstimate;

                Transform existingWCParent = prefabRoot.transform.Find("WheelColliders");
                if (existingWCParent != null)
                    UnityEngine.Object.DestroyImmediate(existingWCParent.gameObject);

                Transform wheelCollidersParent = new GameObject("WheelColliders").transform;
                wheelCollidersParent.SetParent(prefabRoot.transform);
                wheelCollidersParent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                foreach (var wheelSetting in _context.CurrentConfig.wheels)
                {
                    Transform wheelTransform = FindChildByPath(prefabRoot.transform, wheelSetting.partPath);
                    if (wheelTransform == null) continue;

                    GameObject colliderObj = new($"{wheelTransform.name}_collider");
                    colliderObj.transform.SetParent(wheelCollidersParent, false);
                    colliderObj.transform.SetLocalPositionAndRotation(wheelTransform.localPosition, wheelTransform.localRotation);
                    var wheelCollider = colliderObj.AddComponent<WheelCollider>();
                    wheelCollider.radius = wheelSetting.radius;
                    wheelCollider.suspensionDistance = wheelSetting.SuspensionDistance;
                    wheelCollider.mass = 20f;
                    wheelCollider.center = Vector3.zero;

                    var spring = wheelCollider.suspensionSpring;
                    spring.spring = _context.CurrentConfig.suspension.springStiffness > 0 ?
                        _context.CurrentConfig.suspension.springStiffness : 35000f;
                    spring.damper = _context.CurrentConfig.suspension.damperStiffness > 0 ?
                        _context.CurrentConfig.suspension.damperStiffness : 4500f;
                    spring.targetPosition = 0.5f;
                    wheelCollider.suspensionSpring = spring;

                    ConfigureWheelFriction(wheelCollider);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                LogMessage("Wheel colliders applied successfully");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void ConfigureWheelFriction(WheelCollider wheelCollider)
        {
            var forwardFriction = wheelCollider.forwardFriction;
            forwardFriction.extremumSlip = 0.4f;
            forwardFriction.extremumValue = 1f;
            forwardFriction.asymptoteSlip = 0.8f;
            forwardFriction.asymptoteValue = 0.5f;
            forwardFriction.stiffness = 1f;
            wheelCollider.forwardFriction = forwardFriction;

            var sidewaysFriction = wheelCollider.sidewaysFriction;
            sidewaysFriction.extremumSlip = 0.2f;
            sidewaysFriction.extremumValue = 1f;
            sidewaysFriction.asymptoteSlip = 0.5f;
            sidewaysFriction.asymptoteValue = 0.75f;
            sidewaysFriction.stiffness = 1f;
            wheelCollider.sidewaysFriction = sidewaysFriction;
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
                LogMessage("Vehicle measurements updated successfully");
            }
            catch (Exception ex)
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
                System.IO.File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                LogMessage($"Measurements exported to: {path}");
            }
        }

        private void AddPerformanceProfile()
        {
            var cfg = _context?.CurrentConfig;
            if (cfg == null) return;

            if (cfg.performanceProfiles == null)
                cfg.performanceProfiles = new List<VehicleConfig.PerformanceProfile>();

            cfg.performanceProfiles.Add(new VehicleConfig.PerformanceProfile
            {
                profileName = "New Profile",
                torqueMultiplier = 1f,
                suspensionStiffnessMultiplier = 1f,
                steeringResponsiveness = 1f,
                enableAllAssists = true
            });

            EditorUtility.SetDirty(cfg);
            RebuildPerformanceProfiles(cfg);
        }

        private void RebuildPerformanceProfiles(VehicleConfig config)
        {
            if (_performanceProfilesContainer == null) return;

            _performanceProfilesContainer.Clear();
            if (config.performanceProfiles == null)
                config.performanceProfiles = new List<VehicleConfig.PerformanceProfile>();

            for (int i = 0; i < config.performanceProfiles.Count; i++)
            {
                int idx = i;
                var profile = config.performanceProfiles[i];

                var card = new VisualElement
                {
                    style =
                    {
                        marginBottom = 6,
                        paddingLeft = 6,
                        paddingRight = 6,
                        paddingTop = 6,
                        paddingBottom = 6,
                        backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
                    }
                };

                var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                var nameField = new TextField("Profile Name") { value = profile.profileName };
                nameField.RegisterValueChangedCallback(evt =>
                {
                    profile.profileName = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                header.Add(nameField);

                var removeBtn = new Button(() =>
                {
                    config.performanceProfiles.RemoveAt(idx);
                    EditorUtility.SetDirty(config);
                    RebuildPerformanceProfiles(config);
                })
                { text = "Remove", style = { marginLeft = 6 } };
                header.Add(removeBtn);

                card.Add(header);

                var torqueField = new FloatField("Torque Multiplier") { value = profile.torqueMultiplier };
                torqueField.RegisterValueChangedCallback(evt =>
                {
                    profile.torqueMultiplier = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                card.Add(torqueField);

                var suspField = new FloatField("Suspension Multiplier") { value = profile.suspensionStiffnessMultiplier };
                suspField.RegisterValueChangedCallback(evt =>
                {
                    profile.suspensionStiffnessMultiplier = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                card.Add(suspField);

                var steerField = new FloatField("Steering Responsiveness") { value = profile.steeringResponsiveness };
                steerField.RegisterValueChangedCallback(evt =>
                {
                    profile.steeringResponsiveness = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                card.Add(steerField);

                var assistToggle = new Toggle("Enable All Assists") { value = profile.enableAllAssists };
                assistToggle.RegisterValueChangedCallback(evt =>
                {
                    profile.enableAllAssists = evt.newValue;
                    EditorUtility.SetDirty(config);
                });
                card.Add(assistToggle);

                _performanceProfilesContainer.Add(card);
            }
        }

        private string GetTransformPath(Transform transform)
        {
            var names = new List<string>();
            Transform current = transform;

            while (current != null && current != _context.SelectedPrefab.transform)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private string GetLastPathPart(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
        }

        private Transform FindChildByPath(Transform root, string path)
        {
            return root.Find(path);
        }

        private FloatField BindFloat(VisualElement parent, string label, Func<VehicleConfig, float> getter, Action<VehicleConfig, float> setter)
        {
            var field = new FloatField(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private IntegerField BindInt(VisualElement parent, string label, Func<VehicleConfig, int> getter, Action<VehicleConfig, int> setter)
        {
            var field = new IntegerField(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private Toggle BindToggle(VisualElement parent, string label, Func<VehicleConfig, bool> getter, Action<VehicleConfig, bool> setter)
        {
            var field = new Toggle(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private TextField BindText(VisualElement parent, string label, Func<VehicleConfig, string> getter, Action<VehicleConfig, string> setter)
        {
            var field = new TextField(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private EnumField BindEnum<T>(VisualElement parent, string label, Func<VehicleConfig, T> getter, Action<VehicleConfig, T> setter) where T : Enum
        {
            var field = new EnumField(label, default(T));
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, (T)evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private Vector3Field BindVector3(VisualElement parent, string label, Func<VehicleConfig, Vector3> getter, Action<VehicleConfig, Vector3> setter)
        {
            var field = new Vector3Field(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private ColorField BindColor(VisualElement parent, string label, Func<VehicleConfig, Color> getter, Action<VehicleConfig, Color> setter)
        {
            var field = new ColorField(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        private ObjectField BindAudioClip(VisualElement parent, string label, Func<VehicleConfig, AudioClip> getter, Action<VehicleConfig, AudioClip> setter)
        {
            var field = new ObjectField(label) { objectType = typeof(AudioClip) };
            field.RegisterValueChangedCallback(evt =>
            {
                var cfg = _context?.CurrentConfig;
                if (cfg == null) return;
                setter(cfg, evt.newValue as AudioClip);
                EditorUtility.SetDirty(cfg);
            });
            _loaders.Add(cfg => field.value = getter(cfg));
            parent.contentContainer.Add(field);
            return field;
        }

        [Serializable]
        private class MeasurementExport
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
