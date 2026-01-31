using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UVS.Editor.Core;

namespace UVS.Editor.Modules.Specialized
{
    /// <summary>
    /// Vehicle deformation and damage simulation module
    /// Handles real-time mesh deformation, progressive damage, and repair systems
    /// </summary>
    public class DeformationModule : VehicleEditorModuleBase
    {
        #region Module Properties
        public override string ModuleId => "deformation";
        public override string DisplayName => "Damage & Deformation";
        public override int Priority => 80;
        public override bool RequiresVehicle => true;

        public override bool CanActivateWithConfig(VehicleConfig config)
        {
            // Deformation available for all vehicle types
            return config != null;
        }
        #endregion

        #region UI Fields
        // Deformation System
        private Toggle _enableDeformation;
        private FloatField _deformationStrength;
        private FloatField _deformationRadius;
        private EnumField _deformationType;
        private FloatField _meshDetail;

        // Damage System
        private Toggle _enableDamage;
        private FloatField _damageThreshold;
        private FloatField _damageMultiplier;
        private EnumField _damageModel;
        private Toggle _progressiveDamage;

        // Material Properties
        private FloatField _materialStrength;
        private FloatField _materialDuctility;
        private FloatField _materialHardness;
        private EnumField _materialType;
        private FloatField _corrosionResistance;

        // Repair System
        private Toggle _enableRepair;
        private FloatField _repairSpeed;
        private FloatField _repairCost;
        private EnumField _repairMethod;
        private Toggle _visualRepair;

        // Visual Effects
        private Toggle _showDamage;
        private ColorField _damageColor;
        private FloatField _damageOpacity;
        private EnumField _damageTexture;
        private Toggle _particleEffects;

        // Performance
        private IntegerField _maxDeformations;
        private FloatField _updateFrequency;
        private Toggle _lodDeformation;
        private IntegerField _lodLevels;
        private Toggle _cullingOptimization;

        // Safety Systems
        private Toggle _safetyOverride;
        private FloatField _maxDeformationLimit;
        private Toggle _integrityMonitoring;
        private FloatField _failureThreshold;
        private Toggle _emergencyStop;
        #endregion

        #region Module Implementation
        protected override VisualElement CreateModuleUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 10;

            // Deformation System Section
            var deformationSection = CreateSection("Deformation System", true);

            _enableDeformation = new Toggle("Enable Deformation") { value = true };
            _enableDeformation.RegisterValueChangedCallback(OnDeformationBoolConfigChanged);
            deformationSection.contentContainer.Add(_enableDeformation);

            _deformationStrength = new FloatField("Deformation Strength") { value = 1.0f };
            _deformationStrength.RegisterValueChangedCallback(OnDeformationFloatConfigChanged);
            deformationSection.contentContainer.Add(_deformationStrength);

            _deformationRadius = new FloatField("Deformation Radius") { value = 0.5f };
            _deformationRadius.RegisterValueChangedCallback(OnDeformationFloatConfigChanged);
            deformationSection.contentContainer.Add(_deformationRadius);

            _deformationType = new EnumField("Deformation Type", DeformationType.Mesh) { value = DeformationType.Mesh };
            _deformationType.RegisterValueChangedCallback(OnDeformationEnumConfigChanged);
            deformationSection.contentContainer.Add(_deformationType);

            _meshDetail = new FloatField("Mesh Detail") { value = 0.8f };
            _meshDetail.RegisterValueChangedCallback(OnDeformationFloatConfigChanged);
            deformationSection.contentContainer.Add(_meshDetail);

            root.Add(deformationSection);

            // Damage System Section
            var damageSection = CreateSection("Damage System", false);

            _enableDamage = new Toggle("Enable Damage") { value = true };
            _enableDamage.RegisterValueChangedCallback(OnDamageBoolConfigChanged);
            damageSection.contentContainer.Add(_enableDamage);

            _damageThreshold = new FloatField("Damage Threshold") { value = 50f };
            _damageThreshold.RegisterValueChangedCallback(OnDamageFloatConfigChanged);
            damageSection.contentContainer.Add(_damageThreshold);

            _damageMultiplier = new FloatField("Damage Multiplier") { value = 1.2f };
            _damageMultiplier.RegisterValueChangedCallback(OnDamageFloatConfigChanged);
            damageSection.contentContainer.Add(_damageMultiplier);

            _damageModel = new EnumField("Damage Model", DamageModel.Realistic) { value = DamageModel.Realistic };
            _damageModel.RegisterValueChangedCallback(OnDamageEnumConfigChanged);
            damageSection.contentContainer.Add(_damageModel);

            _progressiveDamage = new Toggle("Progressive Damage") { value = true };
            _progressiveDamage.RegisterValueChangedCallback(OnDamageBoolConfigChanged);
            damageSection.contentContainer.Add(_progressiveDamage);

            root.Add(damageSection);

            // Material Properties Section
            var materialSection = CreateSection("Material Properties", false);

            _materialStrength = new FloatField("Material Strength") { value = 400f };
            _materialStrength.RegisterValueChangedCallback(OnMaterialFloatConfigChanged);
            materialSection.contentContainer.Add(_materialStrength);

            _materialDuctility = new FloatField("Material Ductility") { value = 0.6f };
            _materialDuctility.RegisterValueChangedCallback(OnMaterialFloatConfigChanged);
            materialSection.contentContainer.Add(_materialDuctility);

            _materialHardness = new FloatField("Material Hardness") { value = 200f };
            _materialHardness.RegisterValueChangedCallback(OnMaterialFloatConfigChanged);
            materialSection.contentContainer.Add(_materialHardness);

            _materialType = new EnumField("Material Type", MaterialType.Steel) { value = MaterialType.Steel };
            _materialType.RegisterValueChangedCallback(OnMaterialEnumConfigChanged);
            materialSection.contentContainer.Add(_materialType);

            _corrosionResistance = new FloatField("Corrosion Resistance") { value = 0.7f };
            _corrosionResistance.RegisterValueChangedCallback(OnMaterialFloatConfigChanged);
            materialSection.contentContainer.Add(_corrosionResistance);

            root.Add(materialSection);

            // Repair System Section
            var repairSection = CreateSection("Repair System", false);

            _enableRepair = new Toggle("Enable Repair") { value = true };
            _enableRepair.RegisterValueChangedCallback(OnRepairBoolConfigChanged);
            repairSection.contentContainer.Add(_enableRepair);

            _repairSpeed = new FloatField("Repair Speed") { value = 1.0f };
            _repairSpeed.RegisterValueChangedCallback(OnRepairFloatConfigChanged);
            repairSection.contentContainer.Add(_repairSpeed);

            _repairCost = new FloatField("Repair Cost") { value = 100f };
            _repairCost.RegisterValueChangedCallback(OnRepairFloatConfigChanged);
            repairSection.contentContainer.Add(_repairCost);

            _repairMethod = new EnumField("Repair Method", RepairMethod.Welding) { value = RepairMethod.Welding };
            _repairMethod.RegisterValueChangedCallback(OnRepairEnumConfigChanged);
            repairSection.contentContainer.Add(_repairMethod);

            _visualRepair = new Toggle("Visual Repair") { value = true };
            _visualRepair.RegisterValueChangedCallback(OnRepairBoolConfigChanged);
            repairSection.contentContainer.Add(_visualRepair);

            root.Add(repairSection);

            // Visual Effects Section
            var visualSection = CreateSection("Visual Effects", false);

            _showDamage = new Toggle("Show Damage") { value = true };
            _showDamage.RegisterValueChangedCallback(OnVisualBoolConfigChanged);
            visualSection.contentContainer.Add(_showDamage);

            _damageColor = new ColorField("Damage Color") { value = Color.red };
            _damageColor.RegisterValueChangedCallback(OnVisualColorConfigChanged);
            visualSection.contentContainer.Add(_damageColor);

            _damageOpacity = new FloatField("Damage Opacity") { value = 0.7f };
            _damageOpacity.RegisterValueChangedCallback(OnVisualFloatConfigChanged);
            visualSection.contentContainer.Add(_damageOpacity);

            _damageTexture = new EnumField("Damage Texture", DamageTexture.Scratches) { value = DamageTexture.Scratches };
            _damageTexture.RegisterValueChangedCallback(OnVisualEnumConfigChanged);
            visualSection.contentContainer.Add(_damageTexture);

            _particleEffects = new Toggle("Particle Effects") { value = true };
            _particleEffects.RegisterValueChangedCallback(OnVisualBoolConfigChanged);
            visualSection.contentContainer.Add(_particleEffects);

            root.Add(visualSection);

            // Performance Section
            var performanceSection = CreateSection("Performance", false);

            _maxDeformations = new IntegerField("Max Deformations") { value = 1000 };
            _maxDeformations.RegisterValueChangedCallback(OnPerformanceIntConfigChanged);
            performanceSection.contentContainer.Add(_maxDeformations);

            _updateFrequency = new FloatField("Update Frequency") { value = 30f };
            _updateFrequency.RegisterValueChangedCallback(OnPerformanceFloatConfigChanged);
            performanceSection.contentContainer.Add(_updateFrequency);

            _lodDeformation = new Toggle("LOD Deformation") { value = true };
            _lodDeformation.RegisterValueChangedCallback(OnPerformanceBoolConfigChanged);
            performanceSection.contentContainer.Add(_lodDeformation);

            _lodLevels = new IntegerField("LOD Levels") { value = 4 };
            _lodLevels.RegisterValueChangedCallback(OnPerformanceIntConfigChanged);
            performanceSection.contentContainer.Add(_lodLevels);

            _cullingOptimization = new Toggle("Culling Optimization") { value = true };
            _cullingOptimization.RegisterValueChangedCallback(OnPerformanceBoolConfigChanged);
            performanceSection.contentContainer.Add(_cullingOptimization);

            root.Add(performanceSection);

            // Safety Systems Section
            var safetySection = CreateSection("Safety Systems", false);

            _safetyOverride = new Toggle("Safety Override") { value = false };
            _safetyOverride.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_safetyOverride);

            _maxDeformationLimit = new FloatField("Max Deformation Limit") { value = 0.8f };
            _maxDeformationLimit.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_maxDeformationLimit);

            _integrityMonitoring = new Toggle("Integrity Monitoring") { value = true };
            _integrityMonitoring.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_integrityMonitoring);

            _failureThreshold = new FloatField("Failure Threshold") { value = 0.9f };
            _failureThreshold.RegisterValueChangedCallback(OnSafetyFloatConfigChanged);
            safetySection.contentContainer.Add(_failureThreshold);

            _emergencyStop = new Toggle("Emergency Stop") { value = true };
            _emergencyStop.RegisterValueChangedCallback(OnSafetyBoolConfigChanged);
            safetySection.contentContainer.Add(_emergencyStop);

            root.Add(safetySection);

            return root;
        }

        protected override ValidationResult ValidateModule()
        {
            var result = new ValidationResult();

            // Deformation validation
            if (_deformationStrength.value > 2.0f)
                result.AddError("Excessive deformation strength may cause mesh corruption");
            if (_deformationStrength.value < 0.1f)
                result.AddWarning("Low deformation strength may not show visible damage");

            if (_deformationRadius.value > 2.0f)
                result.AddWarning("Large deformation radius may impact performance");
            if (_deformationRadius.value < 0.1f)
                result.AddWarning("Small deformation radius may not capture damage properly");

            if (_meshDetail.value > 1.0f)
                result.AddError("Mesh detail cannot exceed 1.0");
            if (_meshDetail.value < 0.1f)
                result.AddWarning("Low mesh detail may cause visual artifacts");

            // Damage validation
            if (_damageThreshold.value < 10f)
                result.AddWarning("Low damage threshold may cause excessive damage");
            if (_damageThreshold.value > 200f)
                result.AddError("High damage threshold may prevent damage detection");

            if (_damageMultiplier.value > 3.0f)
                result.AddWarning("High damage multiplier may cause unrealistic damage");
            if (_damageMultiplier.value < 0.5f)
                result.AddWarning("Low damage multiplier may not show significant damage");

            // Material validation
            if (_materialStrength.value > 1000f)
                result.AddWarning("Very high material strength may be unrealistic");
            if (_materialStrength.value < 100f)
                result.AddWarning("Low material strength may cause excessive damage");

            if (_materialDuctility.value > 1.0f)
                result.AddError("Material ductility cannot exceed 1.0");
            if (_materialDuctility.value < 0.1f)
                result.AddWarning("Low ductility may cause brittle fracture");

            if (_materialHardness.value > 500f)
                result.AddWarning("Very high hardness may be unrealistic");

            if (_corrosionResistance.value > 1.0f)
                result.AddError("Corrosion resistance cannot exceed 1.0");

            // Repair validation
            if (_repairSpeed.value > 5.0f)
                result.AddWarning("Very high repair speed may be unrealistic");
            if (_repairSpeed.value < 0.1f)
                result.AddWarning("Low repair speed may frustrate players");

            if (_repairCost.value < 0f)
                result.AddError("Repair cost cannot be negative");

            // Visual validation
            if (_damageOpacity.value > 1.0f)
                result.AddError("Opacity cannot exceed 1.0");
            if (_damageOpacity.value < 0.1f)
                result.AddWarning("Low opacity may not show damage clearly");

            // Performance validation
            if (_maxDeformations.value > 5000)
                result.AddWarning("High deformation count may impact performance");
            if (_maxDeformations.value < 100)
                result.AddWarning("Low deformation count may limit damage variety");

            if (_updateFrequency.value > 60f)
                result.AddWarning("High update frequency may impact performance");
            if (_updateFrequency.value < 10f)
                result.AddWarning("Low update frequency may cause visual lag");

            if (_lodLevels.value > 8)
                result.AddWarning("Many LOD levels may increase memory usage");

            // Safety validation
            if (_maxDeformationLimit.value > 1.0f)
                result.AddError("Deformation limit cannot exceed 1.0");
            if (_maxDeformationLimit.value < 0.1f)
                result.AddWarning("Low deformation limit may prevent significant damage");

            if (_failureThreshold.value > 1.0f)
                result.AddError("Failure threshold cannot exceed 1.0");
            if (_failureThreshold.value < 0.5f)
                result.AddWarning("Low failure threshold may cause premature failure");

            return result;
        }

        protected override void OnModuleActivated()
        {
            _context.Console.LogInfo($"Deformation module activated for vehicle: {_context.Config?.VehicleID ?? "Unknown"}");
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _context.Console.LogInfo($"Deformation configuration updated for {config.VehicleID}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnDeformationBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("enable_deformation", _enableDeformation.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnDeformationFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("deformation_strength", _deformationStrength.value);
                _context.Config.SetFloat("deformation_radius", _deformationRadius.value);
                _context.Config.SetFloat("mesh_detail", _meshDetail.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnDeformationEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("deformation_type", (DeformationType)_deformationType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnDamageBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("enable_damage", _enableDamage.value);
                _context.Config.SetBool("progressive_damage", _progressiveDamage.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnDamageFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("damage_threshold", _damageThreshold.value);
                _context.Config.SetFloat("damage_multiplier", _damageMultiplier.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnDamageEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("damage_model", (DamageModel)_damageModel.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnMaterialFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("material_strength", _materialStrength.value);
                _context.Config.SetFloat("material_ductility", _materialDuctility.value);
                _context.Config.SetFloat("material_hardness", _materialHardness.value);
                _context.Config.SetFloat("corrosion_resistance", _corrosionResistance.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnMaterialEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("material_type", (MaterialType)_materialType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnRepairBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("enable_repair", _enableRepair.value);
                _context.Config.SetBool("visual_repair", _visualRepair.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnRepairFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("repair_speed", _repairSpeed.value);
                _context.Config.SetFloat("repair_cost", _repairCost.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnRepairEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("repair_method", (RepairMethod)_repairMethod.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVisualBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("show_damage", _showDamage.value);
                _context.Config.SetBool("particle_effects", _particleEffects.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVisualFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("damage_opacity", _damageOpacity.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVisualColorConfigChanged(ChangeEvent<Color> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetColor("damage_color", _damageColor.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnVisualEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("damage_texture", (DamageTexture)_damageTexture.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnPerformanceIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("max_deformations", _maxDeformations.value);
                _context.Config.SetInt("lod_levels", _lodLevels.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnPerformanceFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("update_frequency", _updateFrequency.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnPerformanceBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("lod_deformation", _lodDeformation.value);
                _context.Config.SetBool("culling_optimization", _cullingOptimization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("safety_override", _safetyOverride.value);
                _context.Config.SetBool("integrity_monitoring", _integrityMonitoring.value);
                _context.Config.SetBool("emergency_stop", _emergencyStop.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnSafetyFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("max_deformation_limit", _maxDeformationLimit.value);
                _context.Config.SetFloat("failure_threshold", _failureThreshold.value);
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
    public enum DeformationType
    {
        Mesh,
        Vertex,
        Texture,
        Hybrid
    }

    public enum DamageModel
    {
        Simple,
        Realistic,
        Advanced,
        Custom
    }

    public enum MaterialType
    {
        Steel,
        Aluminum,
        Composite,
        Titanium,
        CarbonFiber,
        Plastic,
        Wood
    }

    public enum RepairMethod
    {
        Welding,
        Replacement,
        Bonding,
        Patching,
        Magic
    }

    public enum DamageTexture
    {
        Scratches,
        Dents,
        Cracks,
        Burns,
        Rust,
        Custom
    }
    #endregion
}