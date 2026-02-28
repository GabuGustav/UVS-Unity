using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using UVS.Editor.Core;
using UVS.Shared;

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
        public override bool RequiresSpecializedCategory => false;

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
            if (_context?.Config != null)
            {
                LoadFromConfig(_context.Config);
            }
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                LoadFromConfig(config);
            }
        }
        #endregion

        #region Event Handlers
        private void OnDeformationBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.enableDeformation = _enableDeformation.value;
                SaveConfig();
            }
        }

        private void OnDeformationFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.deformationStrength = _deformationStrength.value;
                _context.Config.deformation.deformationRadius = _deformationRadius.value;
                _context.Config.deformation.meshDetail = _meshDetail.value;
                SaveConfig();
            }
        }

        private void OnDeformationEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.deformationType = (DeformationType)_deformationType.value;
                SaveConfig();
            }
        }

        private void OnDamageBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.enableDamage = _enableDamage.value;
                _context.Config.deformation.progressiveDamage = _progressiveDamage.value;
                SaveConfig();
            }
        }

        private void OnDamageFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.damageThreshold = _damageThreshold.value;
                _context.Config.deformation.damageMultiplier = _damageMultiplier.value;
                SaveConfig();
            }
        }

        private void OnDamageEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.damageModel = (DamageModel)_damageModel.value;
                SaveConfig();
            }
        }

        private void OnMaterialFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.materialStrength = _materialStrength.value;
                _context.Config.deformation.materialDuctility = _materialDuctility.value;
                _context.Config.deformation.materialHardness = _materialHardness.value;
                _context.Config.deformation.corrosionResistance = _corrosionResistance.value;
                SaveConfig();
            }
        }

        private void OnMaterialEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.materialType = (MaterialType)_materialType.value;
                SaveConfig();
            }
        }

        private void OnRepairBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.enableRepair = _enableRepair.value;
                _context.Config.deformation.visualRepair = _visualRepair.value;
                SaveConfig();
            }
        }

        private void OnRepairFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.repairSpeed = _repairSpeed.value;
                _context.Config.deformation.repairCost = _repairCost.value;
                SaveConfig();
            }
        }

        private void OnRepairEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.repairMethod = (RepairMethod)_repairMethod.value;
                SaveConfig();
            }
        }

        private void OnVisualBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.showDamage = _showDamage.value;
                _context.Config.deformation.particleEffects = _particleEffects.value;
                SaveConfig();
            }
        }

        private void OnVisualFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.damageOpacity = _damageOpacity.value;
                SaveConfig();
            }
        }

        private void OnVisualColorConfigChanged(ChangeEvent<Color> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.damageColor = _damageColor.value;
                SaveConfig();
            }
        }

        private void OnVisualEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.damageTexture = (DamageTexture)_damageTexture.value;
                SaveConfig();
            }
        }

        private void OnPerformanceIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.maxDeformations = _maxDeformations.value;
                _context.Config.deformation.lodLevels = _lodLevels.value;
                SaveConfig();
            }
        }

        private void OnPerformanceFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.updateFrequency = _updateFrequency.value;
                SaveConfig();
            }
        }

        private void OnPerformanceBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.lodDeformation = _lodDeformation.value;
                _context.Config.deformation.cullingOptimization = _cullingOptimization.value;
                SaveConfig();
            }
        }

        private void OnSafetyBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.safetyOverride = _safetyOverride.value;
                _context.Config.deformation.integrityMonitoring = _integrityMonitoring.value;
                _context.Config.deformation.emergencyStop = _emergencyStop.value;
                SaveConfig();
            }
        }

        private void OnSafetyFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.deformation.maxDeformationLimit = _maxDeformationLimit.value;
                _context.Config.deformation.failureThreshold = _failureThreshold.value;
                SaveConfig();
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

        private void LoadFromConfig(VehicleConfig config)
        {
            var d = config.deformation;
            _enableDeformation.value = d.enableDeformation;
            _deformationStrength.value = d.deformationStrength;
            _deformationRadius.value = d.deformationRadius;
            _deformationType.value = d.deformationType;
            _meshDetail.value = d.meshDetail;

            _enableDamage.value = d.enableDamage;
            _damageThreshold.value = d.damageThreshold;
            _damageMultiplier.value = d.damageMultiplier;
            _damageModel.value = d.damageModel;
            _progressiveDamage.value = d.progressiveDamage;

            _materialStrength.value = d.materialStrength;
            _materialDuctility.value = d.materialDuctility;
            _materialHardness.value = d.materialHardness;
            _materialType.value = d.materialType;
            _corrosionResistance.value = d.corrosionResistance;

            _enableRepair.value = d.enableRepair;
            _repairSpeed.value = d.repairSpeed;
            _repairCost.value = d.repairCost;
            _repairMethod.value = d.repairMethod;
            _visualRepair.value = d.visualRepair;

            _showDamage.value = d.showDamage;
            _damageColor.value = d.damageColor;
            _damageOpacity.value = d.damageOpacity;
            _damageTexture.value = d.damageTexture;
            _particleEffects.value = d.particleEffects;

            _maxDeformations.value = d.maxDeformations;
            _updateFrequency.value = d.updateFrequency;
            _lodDeformation.value = d.lodDeformation;
            _lodLevels.value = d.lodLevels;
            _cullingOptimization.value = d.cullingOptimization;

            _safetyOverride.value = d.safetyOverride;
            _maxDeformationLimit.value = d.maxDeformationLimit;
            _integrityMonitoring.value = d.integrityMonitoring;
            _failureThreshold.value = d.failureThreshold;
            _emergencyStop.value = d.emergencyStop;
        }

        private void SaveConfig()
        {
            if (_context?.Config == null) return;
            EditorUtility.SetDirty(_context.Config);
            _context.NotifyConfigChanged(_context.Config);
        }

        public override void OnModuleGUI() { }
        #endregion
    }
}
