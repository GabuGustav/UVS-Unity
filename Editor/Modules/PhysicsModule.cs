using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class PhysicsModule : VehicleEditorModuleBase
    {
        public override string ModuleId => "physics";
        public override string DisplayName => "Physics";
        public override int Priority => 55;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;

        private readonly List<Action<VehicleConfig>> _loaders = new();

        private Foldout _airplaneSection;
        private Foldout _helicopterSection;
        private Foldout _constructionSection;
        private Label _classificationLabel;

        protected override VisualElement CreateModuleUI()
        {
            var root = new ScrollView { style = { flexGrow = 1 } };

            _classificationLabel = new Label("Physics settings will adapt to the vehicle classification.")
            {
                style = { marginBottom = 6, color = Color.white }
            };
            root.Add(_classificationLabel);

            _airplaneSection = CreateSection("Plane Physics", true);
            CreateAirplaneSection(_airplaneSection);
            root.Add(_airplaneSection);

            _helicopterSection = CreateSection("Helicopter Physics", false);
            CreateHelicopterSection(_helicopterSection);
            root.Add(_helicopterSection);

            _constructionSection = CreateSection("Construction Vehicle Physics", false);
            CreateConstructionSection(_constructionSection);
            root.Add(_constructionSection);

            return root;
        }

        protected override void OnModuleActivated()
        {
            var cfg = _context?.CurrentConfig;
            if (cfg == null) return;
            LoadFromConfig(cfg);
            UpdateVisibility(cfg);
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;
            LoadFromConfig(config);
            UpdateVisibility(config);
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
        }

        private void UpdateVisibility(VehicleConfig config)
        {
            bool isPlane = IsPlane(config);
            bool isHeli = IsHelicopter(config);
            bool isConstruction = IsConstruction(config);

            _airplaneSection.style.display = isPlane ? DisplayStyle.Flex : DisplayStyle.None;
            _helicopterSection.style.display = isHeli ? DisplayStyle.Flex : DisplayStyle.None;
            _constructionSection.style.display = isConstruction ? DisplayStyle.Flex : DisplayStyle.None;

            _classificationLabel.text = $"Type: {config.vehicleType} | Category: {config.GetCurrentCategory()} | Specialized: {config.GetCurrentSpecialized()}";
        }

        private bool IsPlane(VehicleConfig config)
        {
            if (config.vehicleType != VehicleConfig.VehicleType.Air) return false;
            return config.airCategory == VehicleConfig.AirVehicleCategory.Airplane ||
                   config.airCategory == VehicleConfig.AirVehicleCategory.Glider ||
                   config.airCategory == VehicleConfig.AirVehicleCategory.Standard ||
                   (config.airCategory == VehicleConfig.AirVehicleCategory.Specialized &&
                    config.specializedAir != VehicleConfig.SpecializedAirVehicleType.VTOL);
        }

        private bool IsHelicopter(VehicleConfig config)
        {
            if (config.vehicleType != VehicleConfig.VehicleType.Air) return false;
            return config.airCategory == VehicleConfig.AirVehicleCategory.Helicopter ||
                   config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL;
        }

        private bool IsConstruction(VehicleConfig config)
        {
            return config.vehicleType == VehicleConfig.VehicleType.Land &&
                   config.landCategory == VehicleConfig.LandVehicleCategory.Specialized &&
                   config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Construction;
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

        private void CreateAirplaneSection(Foldout section)
        {
            BindFloat(section, "Wing Area", c => c.airplanePhysics.wingArea, (c, v) => c.airplanePhysics.wingArea = v);
            BindFloat(section, "Lift Coefficient", c => c.airplanePhysics.liftCoefficient, (c, v) => c.airplanePhysics.liftCoefficient = v);
            BindFloat(section, "Drag Coefficient", c => c.airplanePhysics.dragCoefficient, (c, v) => c.airplanePhysics.dragCoefficient = v);
            BindFloat(section, "Max Thrust", c => c.airplanePhysics.maxThrust, (c, v) => c.airplanePhysics.maxThrust = v);
            BindFloat(section, "Stall Speed", c => c.airplanePhysics.stallSpeed, (c, v) => c.airplanePhysics.stallSpeed = v);
            BindFloat(section, "Max Bank Angle", c => c.airplanePhysics.maxBankAngle, (c, v) => c.airplanePhysics.maxBankAngle = v);
            BindFloat(section, "Pitch Stability", c => c.airplanePhysics.pitchStability, (c, v) => c.airplanePhysics.pitchStability = v);
            BindFloat(section, "Roll Stability", c => c.airplanePhysics.rollStability, (c, v) => c.airplanePhysics.rollStability = v);
            BindFloat(section, "Yaw Stability", c => c.airplanePhysics.yawStability, (c, v) => c.airplanePhysics.yawStability = v);
            BindFloat(section, "Control Surface Effect", c => c.airplanePhysics.controlSurfaceEffectiveness, (c, v) => c.airplanePhysics.controlSurfaceEffectiveness = v);
        }

        private void CreateHelicopterSection(Foldout section)
        {
            BindInt(section, "Rotor Count", c => c.helicopterPhysics.rotorCount, (c, v) => c.helicopterPhysics.rotorCount = v);
            BindFloat(section, "Main Rotor Diameter", c => c.helicopterPhysics.mainRotorDiameter, (c, v) => c.helicopterPhysics.mainRotorDiameter = v);
            BindFloat(section, "Tail Rotor Diameter", c => c.helicopterPhysics.tailRotorDiameter, (c, v) => c.helicopterPhysics.tailRotorDiameter = v);
            BindFloat(section, "Collective Pitch Range", c => c.helicopterPhysics.collectivePitchRange, (c, v) => c.helicopterPhysics.collectivePitchRange = v);
            BindFloat(section, "Cyclic Response", c => c.helicopterPhysics.cyclicResponse, (c, v) => c.helicopterPhysics.cyclicResponse = v);
            BindFloat(section, "Torque Compensation", c => c.helicopterPhysics.torqueCompensation, (c, v) => c.helicopterPhysics.torqueCompensation = v);
            BindFloat(section, "Hover Efficiency", c => c.helicopterPhysics.hoverEfficiency, (c, v) => c.helicopterPhysics.hoverEfficiency = v);
            BindFloat(section, "Max Climb Rate", c => c.helicopterPhysics.maxClimbRate, (c, v) => c.helicopterPhysics.maxClimbRate = v);
            BindFloat(section, "Max Descent Rate", c => c.helicopterPhysics.maxDescentRate, (c, v) => c.helicopterPhysics.maxDescentRate = v);
            BindFloat(section, "Max Forward Speed", c => c.helicopterPhysics.maxForwardSpeed, (c, v) => c.helicopterPhysics.maxForwardSpeed = v);
            BindFloat(section, "Max Yaw Rate", c => c.helicopterPhysics.maxYawRate, (c, v) => c.helicopterPhysics.maxYawRate = v);
        }

        private void CreateConstructionSection(Foldout section)
        {
            BindFloat(section, "Traction Coefficient", c => c.constructionPhysics.tractionCoefficient, (c, v) => c.constructionPhysics.tractionCoefficient = v);
            BindFloat(section, "Stability Assist", c => c.constructionPhysics.stabilityAssist, (c, v) => c.constructionPhysics.stabilityAssist = v);
            BindFloat(section, "Max Slope Angle", c => c.constructionPhysics.maxSlopeAngle, (c, v) => c.constructionPhysics.maxSlopeAngle = v);
            BindFloat(section, "Front Weight Distribution", c => c.constructionPhysics.weightDistributionFront, (c, v) => c.constructionPhysics.weightDistributionFront = v);
            BindFloat(section, "Hydraulic Force Multiplier", c => c.constructionPhysics.hydraulicForceMultiplier, (c, v) => c.constructionPhysics.hydraulicForceMultiplier = v);
            BindFloat(section, "Ground Pressure Limit", c => c.constructionPhysics.groundPressureLimit, (c, v) => c.constructionPhysics.groundPressureLimit = v);
            BindFloat(section, "Suspension Compliance", c => c.constructionPhysics.suspensionCompliance, (c, v) => c.constructionPhysics.suspensionCompliance = v);
            BindToggle(section, "Enable Outrigger Physics", c => c.constructionPhysics.enableOutriggerPhysics, (c, v) => c.constructionPhysics.enableOutriggerPhysics = v);
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
    }
}
