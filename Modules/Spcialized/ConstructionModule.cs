using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules.Specialized
{
    /// <summary>
    /// Construction Equipment vehicle configuration module
    /// Handles hydraulic systems, arm mechanics, and stability for bulldozers, excavators, and cranes
    /// </summary>
    public class ConstructionModule : VehicleEditorModuleBase
    {
        #region Module Properties
        public override string ModuleId => "construction";
        public override string DisplayName => "Construction Equipment";
        public override int Priority => 70;
        public override bool RequiresVehicle => true;
        #endregion

        #region UI Fields
        // Equipment Type
        private EnumField _equipmentType;
        private TextField _equipmentModel;
        private FloatField _operatingWeight;
        private FloatField _groundClearance;

        // Hydraulic Systems
        private FloatField _hydraulicPressure;
        private FloatField _hydraulicFlowRate;
        private FloatField _hydraulicResponseTime;
        private Toggle _hydraulicOverloadProtection;
        private FloatField _hydraulicCoolingRate;

        // Arm/Boom Configuration
        private FloatField _armLength;
        private FloatField _armMaxAngle;
        private FloatField _armMinAngle;
        private FloatField _armExtensionSpeed;
        private FloatField _armRotationSpeed;

        // Bucket/Attachment
        private FloatField _bucketCapacity;
        private FloatField _bucketWidth;
        private FloatField _bucketForce;
        private EnumField _attachmentType;
        private FloatField _attachmentWeight;

        // Stability Systems
        private FloatField _stabilityBase;
        private FloatField _stabilityMargin;
        private Toggle _autoStabilization;
        private FloatField _outriggerExtension;
        private Toggle _outriggerAutoLevel;

        // Safety Systems
        private Toggle _loadMonitoring;
        private FloatField _maxLoadCapacity;
        private FloatField _safetyFactor;
        private Toggle _operatorPresence;
        private Toggle _emergencyStop;

        // Environmental
        private FloatField _operatingTemperature;
        private FloatField _windResistance;
        private FloatField _slopeCapability;
        private EnumField _terrainType;
        #endregion

        #region Module Implementation
        protected override VisualElement CreateModuleUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 10;

            // Equipment Type Section
            var equipmentSection = CreateSection("Equipment Configuration", true);

            _equipmentType = new EnumField("Equipment Type", EquipmentType.Excavator) { value = EquipmentType.Excavator };
            _equipmentType.RegisterValueChangedCallback(OnEquipmentEnumConfigChanged);
            equipmentSection.contentContainer.Add(_equipmentType);

            _equipmentModel = new TextField("Model Name") { value = "CAT 320D" };
            _equipmentModel.RegisterValueChangedCallback(OnEquipmentStringConfigChanged);
            equipmentSection.contentContainer.Add(_equipmentModel);

            _operatingWeight = new FloatField("Operating Weight (kg)") { value = 22000f };
            _operatingWeight.RegisterValueChangedCallback(OnEquipmentFloatConfigChanged);
            equipmentSection.contentContainer.Add(_operatingWeight);

            _groundClearance = new FloatField("Ground Clearance (mm)") { value = 450f };
            _groundClearance.RegisterValueChangedCallback(OnEquipmentFloatConfigChanged);
            equipmentSection.contentContainer.Add(_groundClearance);

            root.Add(equipmentSection);

            // Hydraulic Systems Section
            var hydraulicSection = CreateSection("Hydraulic Systems", false);

            _hydraulicPressure = new FloatField("System Pressure (bar)") { value = 280f };
            _hydraulicPressure.RegisterValueChangedCallback(OnHydraulicFloatConfigChanged);
            hydraulicSection.contentContainer.Add(_hydraulicPressure);

            _hydraulicFlowRate = new FloatField("Flow Rate (L/min)") { value = 205f };
            _hydraulicFlowRate.RegisterValueChangedCallback(OnHydraulicFloatConfigChanged);
            hydraulicSection.contentContainer.Add(_hydraulicFlowRate);

            _hydraulicResponseTime = new FloatField("Response Time (s)") { value = 0.3f };
            _hydraulicResponseTime.RegisterValueChangedCallback(OnHydraulicFloatConfigChanged);
            hydraulicSection.contentContainer.Add(_hydraulicResponseTime);

            _hydraulicOverloadProtection = new Toggle("Overload Protection") { value = true };
            _hydraulicOverloadProtection.RegisterValueChangedCallback(OnHydraulicBoolConfigChanged);
            hydraulicSection.contentContainer.Add(_hydraulicOverloadProtection);

            _hydraulicCoolingRate = new FloatField("Cooling Rate") { value = 0.8f };
            _hydraulicCoolingRate.RegisterValueChangedCallback(OnHydraulicFloatConfigChanged);
            hydraulicSection.contentContainer.Add(_hydraulicCoolingRate);

            root.Add(hydraulicSection);

            // Arm/Boom Configuration Section
            var armSection = CreateSection("Arm/Boom Configuration", false);

            _armLength = new FloatField("Arm Length (m)") { value = 6.2f };
            _armLength.RegisterValueChangedCallback(OnArmFloatConfigChanged);
            armSection.contentContainer.Add(_armLength);

            _armMaxAngle = new FloatField("Max Angle (°)") { value = 160f };
            _armMaxAngle.RegisterValueChangedCallback(OnArmFloatConfigChanged);
            armSection.contentContainer.Add(_armMaxAngle);

            _armMinAngle = new FloatField("Min Angle (°)") { value = -30f };
            _armMinAngle.RegisterValueChangedCallback(OnArmFloatConfigChanged);
            armSection.contentContainer.Add(_armMinAngle);

            _armExtensionSpeed = new FloatField("Extension Speed (m/s)") { value = 0.8f };
            _armExtensionSpeed.RegisterValueChangedCallback(OnArmFloatConfigChanged);
            armSection.contentContainer.Add(_armExtensionSpeed);

            _armRotationSpeed = new FloatField("Rotation Speed (°/s)") { value = 12f };
            _armRotationSpeed.RegisterValueChangedCallback(OnArmFloatConfigChanged);
            armSection.contentContainer.Add(_armRotationSpeed);

            root.Add(armSection);

            // Bucket/Attachment Section
            var bucketSection = CreateSection("Bucket/Attachment", false);

            _bucketCapacity = new FloatField("Bucket Capacity (m³)") { value = 1.2f };
            _bucketCapacity.RegisterValueChangedCallback(OnAttachmentFloatConfigChanged);
            bucketSection.contentContainer.Add(_bucketCapacity);

            _bucketWidth = new FloatField("Bucket Width (m)") { value = 1.1f };
            _bucketWidth.RegisterValueChangedCallback(OnAttachmentFloatConfigChanged);
            bucketSection.contentContainer.Add(_bucketWidth);

            _bucketForce = new FloatField("Breakout Force (kN)") { value = 140f };
            _bucketForce.RegisterValueChangedCallback(OnAttachmentFloatConfigChanged);
            bucketSection.contentContainer.Add(_bucketForce);

            _attachmentType = new EnumField("Attachment Type", AttachmentType.StandardBucket) { value = AttachmentType.StandardBucket };
            _attachmentType.RegisterValueChangedCallback(OnAttachmentEnumConfigChanged);
            bucketSection.contentContainer.Add(_attachmentType);

            _attachmentWeight = new FloatField("Attachment Weight (kg)") { value = 850f };
            _attachmentWeight.RegisterValueChangedCallback(OnAttachmentFloatConfigChanged);
            bucketSection.contentContainer.Add(_attachmentWeight);

            root.Add(bucketSection);

            // Stability Systems Section
            var stabilitySection = CreateSection("Stability Systems", false);

            _stabilityBase = new FloatField("Stability Base (m)") { value = 2.8f };
            _stabilityBase.RegisterValueChangedCallback(OnStabilityFloatConfigChanged);
            stabilitySection.contentContainer.Add(_stabilityBase);

            _stabilityMargin = new FloatField("Stability Margin") { value = 0.3f };
            _stabilityMargin.RegisterValueChangedCallback(OnStabilityFloatConfigChanged);
            stabilitySection.contentContainer.Add(_stabilityMargin);

            _autoStabilization = new Toggle("Auto Stabilization") { value = true };
            _autoStabilization.RegisterValueChangedCallback(OnStabilityBoolConfigChanged);
            stabilitySection.contentContainer.Add(_autoStabilization);

            _outriggerExtension = new FloatField("Outrigger Extension (m)") { value = 3.5f };
            _outriggerExtension.RegisterValueChangedCallback(OnStabilityFloatConfigChanged);
            stabilitySection.contentContainer.Add(_outriggerExtension);

            _outriggerAutoLevel = new Toggle("Auto Leveling") { value = true };
            _outriggerAutoLevel.RegisterValueChangedCallback(OnStabilityBoolConfigChanged);
            stabilitySection.contentContainer.Add(_outriggerAutoLevel);

            root.Add(stabilitySection);

            // Safety Systems Section
            var safetySection = CreateSection("Safety Systems", false);

            _loadMonitoring = new Toggle("Load Monitoring") { value = true };
            _loadMonitoring.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_loadMonitoring);

            _maxLoadCapacity = new FloatField("Max Load (kg)") { value = 3500f };
            _maxLoadCapacity.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_maxLoadCapacity);

            _safetyFactor = new FloatField("Safety Factor") { value = 1.5f };
            _safetyFactor.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_safetyFactor);

            _operatorPresence = new Toggle("Operator Presence") { value = true };
            _operatorPresence.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_operatorPresence);

            _emergencyStop = new Toggle("Emergency Stop") { value = true };
            _emergencyStop.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_emergencyStop);

            root.Add(safetySection);

            // Environmental Section
            var envSection = CreateSection("Environmental", false);

            _operatingTemperature = new FloatField("Operating Temp (°C)") { value = 45f };
            _operatingTemperature.RegisterValueChangedCallback(OnEnvFloatConfigChanged);
            envSection.contentContainer.Add(_operatingTemperature);

            _windResistance = new FloatField("Wind Resistance (km/h)") { value = 50f };
            _windResistance.RegisterValueChangedCallback(OnEnvFloatConfigChanged);
            envSection.contentContainer.Add(_windResistance);

            _slopeCapability = new FloatField("Max Slope (°)") { value = 30f };
            _slopeCapability.RegisterValueChangedCallback(OnEnvFloatConfigChanged);
            envSection.contentContainer.Add(_slopeCapability);

            _terrainType = new EnumField("Terrain Type", TerrainType.Mixed) { value = TerrainType.Mixed };
            _terrainType.RegisterValueChangedCallback(OnEnvEnumConfigChanged);
            envSection.contentContainer.Add(_terrainType);

            root.Add(envSection);

            return root;
        }

        protected override ValidationResult ValidateModule()
        {
            var result = new ValidationResult();

            // Equipment validation
            if (_operatingWeight.value > 50000)
                result.AddWarning("High operating weight may require special transport permits");
            if (_operatingWeight.value < 5000)
                result.AddWarning("Light weight may indicate insufficient stability");

            // Hydraulic validation
            if (_hydraulicPressure.value > 350)
                result.AddError("Hydraulic pressure exceeds safe operating limits");
            if (_hydraulicPressure.value < 200)
                result.AddWarning("Low hydraulic pressure may reduce performance");

            if (_hydraulicFlowRate.value > 300)
                result.AddWarning("High flow rate requires larger hydraulic system");

            // Arm validation
            if (_armLength.value > 15)
                result.AddError("Excessive arm length compromises stability");
            if (_armLength.value < 2)
                result.AddWarning("Short arm reduces operational reach");

            if (_armMaxAngle.value > 180)
                result.AddError("Arm angle exceeds mechanical limits");
            if (_armExtensionSpeed.value > 2)
                result.AddError("High extension speed creates safety risks");

            // Attachment validation
            if (_bucketCapacity.value > 5)
                result.AddWarning("Large bucket may overload hydraulic system");
            if (_bucketForce.value > 200)
                result.AddWarning("High breakout force may cause structural stress");

            // Stability validation
            if (_stabilityMargin.value < 0.2f)
                result.AddError("Insufficient stability margin - tipping risk");
            if (_stabilityMargin.value > 0.5f)
                result.AddWarning("High stability margin reduces operational efficiency");

            if (_outriggerExtension.value > 6)
                result.AddWarning("Long outriggers may be unstable on soft ground");

            // Safety validation
            if (_maxLoadCapacity.value > 10000)
                result.AddWarning("High load capacity requires enhanced stability systems");
            if (_safetyFactor.value < 1.2f)
                result.AddError("Safety factor below minimum requirements");
            if (_safetyFactor.value > 3.0f)
                result.AddWarning("Excessive safety factor reduces operational efficiency");

            // Environmental validation
            if (_operatingTemperature.value > 60)
                result.AddWarning("High operating temperature may affect hydraulic performance");
            if (_windResistance.value > 80)
                result.AddError("Wind resistance exceeds safe operating limits");
            if (_slopeCapability.value > 45)
                result.AddError("Slope capability exceeds safe operating limits");

            return result;
        }

        protected override void OnModuleActivated()
        {
            _context.Console.LogInfo($"Construction module activated for vehicle: {_context.Config?.VehicleID ?? "Unknown"}");
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _context.Console.LogInfo($"Construction configuration updated for {config.VehicleID}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnEquipmentEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("equipment_type", (EquipmentType)_equipmentType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEquipmentStringConfigChanged(ChangeEvent<string> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetString("equipment_model", _equipmentModel.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEquipmentFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("operating_weight", _operatingWeight.value);
                _context.Config.SetFloat("ground_clearance", _groundClearance.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnHydraulicFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("hydraulic_pressure", _hydraulicPressure.value);
                _context.Config.SetFloat("hydraulic_flow_rate", _hydraulicFlowRate.value);
                _context.Config.SetFloat("hydraulic_response_time", _hydraulicResponseTime.value);
                _context.Config.SetFloat("hydraulic_cooling_rate", _hydraulicCoolingRate.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnHydraulicBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("hydraulic_overload_protection", _hydraulicOverloadProtection.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnArmFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("arm_length", _armLength.value);
                _context.Config.SetFloat("arm_max_angle", _armMaxAngle.value);
                _context.Config.SetFloat("arm_min_angle", _armMinAngle.value);
                _context.Config.SetFloat("arm_extension_speed", _armExtensionSpeed.value);
                _context.Config.SetFloat("arm_rotation_speed", _armRotationSpeed.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnAttachmentFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("bucket_capacity", _bucketCapacity.value);
                _context.Config.SetFloat("bucket_width", _bucketWidth.value);
                _context.Config.SetFloat("bucket_force", _bucketForce.value);
                _context.Config.SetFloat("attachment_weight", _attachmentWeight.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnAttachmentEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("attachment_type", (AttachmentType)_attachmentType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnStabilityFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("stability_base", _stabilityBase.value);
                _context.Config.SetFloat("stability_margin", _stabilityMargin.value);
                _context.Config.SetFloat("outrigger_extension", _outriggerExtension.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnStabilityBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("auto_stabilization", _autoStabilization.value);
                _context.Config.SetBool("outrigger_auto_level", _outriggerAutoLevel.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("load_monitoring", _loadMonitoring.value);
                _context.Config.SetBool("operator_presence", _operatorPresence.value);
                _context.Config.SetBool("emergency_stop", _emergencyStop.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("max_load_capacity", _maxLoadCapacity.value);
                _context.Config.SetFloat("safety_factor", _safetyFactor.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEnvFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("operating_temperature", _operatingTemperature.value);
                _context.Config.SetFloat("wind_resistance", _windResistance.value);
                _context.Config.SetFloat("slope_capability", _slopeCapability.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnEnvEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("terrain_type", (TerrainType)_terrainType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }
        #endregion

        #region Helper Methods
        private Foldout CreateSection(string title, bool expanded)
        {
            return new Foldout()
            {
                text = title,
                value = expanded,
                style =
                {
                    marginBottom = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f))
                }
            };
        }

        public override void OnModuleGUI() { }
        #endregion
    }

    #region Supporting Enums
    public enum EquipmentType
    {
        Excavator,
        Bulldozer,
        Crane,
        WheelLoader,
        Backhoe,
        SkidSteer,
        DumpTruck,
        ConcretePump
    }

    public enum AttachmentType
    {
        StandardBucket,
        HeavyDutyBucket,
        RockBucket,
        HydraulicHammer,
        Grapple,
        Auger,
        Ripper,
        Blade,
        Hook,
        Forks
    }

    public enum TerrainType
    {
        HardSurface,
        SoftSoil,
        Rocky,
        Mixed,
        Swamp,
        Sand
    }
    #endregion
}