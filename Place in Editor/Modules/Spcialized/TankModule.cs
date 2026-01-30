using UnityEngine;
using UnityEngine.UIElements;
using System;
using UVS.Editor.Core;
using Unity.VisualScripting;

namespace UVS.Editor.Modules.Specialized
{
    /// <summary>
    /// Tank-specific vehicle configuration module
    /// Handles turret systems, armor configuration, and weapon systems
    /// </summary>
    public class TankModule : VehicleEditorModuleBase
    {
        #region Module Properties
        public override string ModuleId => "tank";
        public override string DisplayName => "Tank Systems";
        public override int Priority => 50;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => true;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => true;
        public override bool IsVTOLModule => false;
        #endregion

        #region UI Fields
        // Turret Configuration
        private FloatField _turretRotationSpeed;
        private FloatField _turretElevationSpeed;
        private FloatField _turretMaxElevation;
        private FloatField _turretMinElevation;
        private Toggle _turretStabilization;

        // Armor Configuration
        private IntegerField _frontArmorThickness;
        private IntegerField _sideArmorThickness;
        private IntegerField _rearArmorThickness;
        private IntegerField _turretArmorThickness;
        private EnumField _armorType;

        // Weapon Systems
        private IntegerField _mainCaliber;
        private FloatField _mainReloadTime;
        private IntegerField _ammoCapacity;
        private Toggle _autoLoader;
        private EnumField _ammoType;

        // Track System
        private FloatField _trackWidth;
        private FloatField _trackLength;
        private IntegerField _roadWheels;
        private Toggle _trackStabilization;

        // Crew Configuration
        private IntegerField _crewCount;
        private TextField _crewPositions;
        private Toggle _crewInjurySimulation;
        #endregion

        #region Module Implementation
        protected override VisualElement CreateModuleUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 10;

            // Turret Systems Section
            var turretSection = CreateSection("Turret Systems", true);

            _turretRotationSpeed = new FloatField("Rotation Speed (°/s)") { value = 45f };
            _turretRotationSpeed.RegisterValueChangedCallback(OnTurretFloatConfigChanged);
            turretSection.contentContainer.Add(_turretRotationSpeed);

            _turretElevationSpeed = new FloatField("Elevation Speed (°/s)") { value = 25f };
            _turretElevationSpeed.RegisterValueChangedCallback(OnTurretFloatConfigChanged);
            turretSection.contentContainer.Add(_turretElevationSpeed);

            _turretMaxElevation = new FloatField("Max Elevation (°)") { value = 20f };
            _turretMaxElevation.RegisterValueChangedCallback(OnTurretFloatConfigChanged);
            turretSection.contentContainer.Add(_turretMaxElevation);

            _turretMinElevation = new FloatField("Min Elevation (°)") { value = -10f };
            _turretMinElevation.RegisterValueChangedCallback(OnTurretFloatConfigChanged);
            turretSection.contentContainer.Add(_turretMinElevation);

            _turretStabilization = new Toggle("Gun Stabilization") { value = true };
            _turretStabilization.RegisterValueChangedCallback(OnTurretBoolConfigChanged);
            turretSection.contentContainer.Add(_turretStabilization);

            root.Add(turretSection);

            // Armor Configuration Section
            var armorSection = CreateSection("Armor Configuration", false);

            _frontArmorThickness = new IntegerField("Front Armor (mm)") { value = 120 };
            _frontArmorThickness.RegisterValueChangedCallback(OnArmorIntConfigChanged);
            armorSection.contentContainer.Add(_frontArmorThickness);

            _sideArmorThickness = new IntegerField("Side Armor (mm)") { value = 80 };
            _sideArmorThickness.RegisterValueChangedCallback(OnArmorIntConfigChanged);
            armorSection.contentContainer.Add(_sideArmorThickness);

            _rearArmorThickness = new IntegerField("Rear Armor (mm)") { value = 40 };
            _rearArmorThickness.RegisterValueChangedCallback(OnArmorIntConfigChanged);
            armorSection.contentContainer.Add(_rearArmorThickness);

            _turretArmorThickness = new IntegerField("Turret Armor (mm)") { value = 150 };
            _turretArmorThickness.RegisterValueChangedCallback(OnArmorIntConfigChanged);
            armorSection.contentContainer.Add(_turretArmorThickness);

            _armorType = new EnumField("Armor Type", ArmorType.RolledHomogeneousSteel);
            _armorType.RegisterValueChangedCallback(OnArmorEnumConfigChanged);
            armorSection.contentContainer.Add(_armorType);

            root.Add(armorSection);

            // Weapon Systems Section
            var weaponSection = CreateSection("Weapon Systems", false);

            _mainCaliber = new IntegerField("Main Gun Caliber (mm)") { value = 120 };
            _mainCaliber.RegisterValueChangedCallback(OnWeaponIntConfigChanged);
            weaponSection.contentContainer.Add(_mainCaliber);

            _mainReloadTime = new FloatField("Reload Time (s)") { value = 8.5f };
            _mainReloadTime.RegisterValueChangedCallback(OnWeaponFloatConfigChanged);
            weaponSection.contentContainer.Add(_mainReloadTime);

            _ammoCapacity = new IntegerField("Ammo Capacity") { value = 40 };
            _ammoCapacity.RegisterValueChangedCallback(OnWeaponIntConfigChanged);
            weaponSection.contentContainer.Add(_ammoCapacity);

            _autoLoader = new Toggle("Auto Loader") { value = true };
            _autoLoader.RegisterValueChangedCallback(OnWeaponBoolConfigChanged);
            weaponSection.contentContainer.Add(_autoLoader);

            _ammoType = new EnumField("Ammo Type", AmmoType.APFSDS);
            _ammoType.RegisterValueChangedCallback(OnWeaponEnumConfigChanged);
            weaponSection.contentContainer.Add(_ammoType);

            root.Add(weaponSection);

            // Track System Section
            var trackSection = CreateSection("Track System", false);

            _trackWidth = new FloatField("Track Width (m)") { value = 0.65f };
            _trackWidth.RegisterValueChangedCallback(OnTrackFloatConfigChanged);
            trackSection.contentContainer.Add(_trackWidth);

            _trackLength = new FloatField("Track Length (m)") { value = 4.2f };
            _trackLength.RegisterValueChangedCallback(OnTrackFloatConfigChanged);
            trackSection.contentContainer.Add(_trackLength);

            _roadWheels = new IntegerField("Road Wheels") { value = 7 };
            _roadWheels.RegisterValueChangedCallback(OnTrackIntConfigChanged);
            trackSection.contentContainer.Add(_roadWheels);

            _trackStabilization = new Toggle("Track Stabilization") { value = true };
            _trackStabilization.RegisterValueChangedCallback(OnTrackBoolConfigChanged);
            trackSection.contentContainer.Add(_trackStabilization);

            root.Add(trackSection);

            // Crew Configuration Section
            var crewSection = CreateSection("Crew Configuration", false);

            _crewCount = new IntegerField("Crew Count") { value = 4 };
            _crewCount.RegisterValueChangedCallback(OnCrewIntConfigChanged);
            crewSection.contentContainer.Add(_crewCount);

            _crewPositions = new TextField("Crew Positions") { value = "Commander, Gunner, Loader, Driver" };
            _crewPositions.RegisterValueChangedCallback(OnCrewStringConfigChanged);
            crewSection.contentContainer.Add(_crewPositions);

            _crewInjurySimulation = new Toggle("Crew Injury Simulation") { value = true };
            _crewInjurySimulation.RegisterValueChangedCallback(OnCrewBoolConfigChanged);
            crewSection.contentContainer.Add(_crewInjurySimulation);

            root.Add(crewSection);

            return root;
        }

        protected override ValidationResult ValidateModule()
        {
            var result = new ValidationResult();

            // Turret validation
            if (_turretRotationSpeed.value > 60)
                result.AddError("Turret rotation speed exceeds mechanical limits (max 60°/s)");
            if (_turretRotationSpeed.value < 10)
                result.AddWarning("Turret rotation speed may be too slow for modern combat");

            if (_turretElevationSpeed.value > 40)
                result.AddError("Turret elevation speed exceeds safe limits");

            // Armor validation
            if (_frontArmorThickness.value > 300)
                result.AddWarning("Excessive armor thickness may impact mobility");
            if (_frontArmorThickness.value < 50)
                result.AddWarning("Front armor may be insufficient for modern threats");

            // Weapon validation
            if (_mainCaliber.value > 150)
                result.AddWarning("Large caliber may cause excessive recoil and wear");
            if (_mainReloadTime.value < 3 && !_autoLoader.value)
                result.AddError("Manual reload time cannot be less than 3 seconds");

            if (_ammoCapacity.value > 60)
                result.AddWarning("Large ammo capacity may impact internal space and crew safety");

            // Track validation
            if (_roadWheels.value < 4)
                result.AddError("Insufficient road wheels for proper weight distribution");
            if (_roadWheels.value > 10)
                result.AddWarning("Too many road wheels may increase maintenance complexity");

            // Crew validation
            if (_crewCount.value < 3)
                result.AddError("Insufficient crew for effective tank operation");
            if (_crewCount.value > 6)
                result.AddWarning("Large crew may reduce operational efficiency");

            return result;
        }

        protected override void OnModuleActivated()
        {
            _context.Console.LogInfo($"Tank module activated for vehicle: {_context.Config?.VehicleID ?? "Unknown"}");
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            // Update UI fields with new config values
            if (config != null)
            {
                // This would typically load from the config object
                // For now, we'll just log the change
                _context.Console.LogInfo($"Tank configuration updated for {config.VehicleID}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnTurretFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                // Update config with turret settings
                _context.Config.SetFloat("turret_rotation_speed", _turretRotationSpeed.value);
                _context.Config.SetFloat("turret_elevation_speed", _turretElevationSpeed.value);
                _context.Config.SetFloat("turret_max_elevation", _turretMaxElevation.value);
                _context.Config.SetFloat("turret_min_elevation", _turretMinElevation.value);
                _context.Config.SetBool("turret_stabilization", _turretStabilization.value);

                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnTurretBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("turret_stabilization", _turretStabilization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnArmorIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("front_armor_thickness", _frontArmorThickness.value);
                _context.Config.SetInt("side_armor_thickness", _sideArmorThickness.value);
                _context.Config.SetInt("rear_armor_thickness", _rearArmorThickness.value);
                _context.Config.SetInt("turret_armor_thickness", _turretArmorThickness.value);
                _context.Config.SetEnum("armor_type", (ArmorType)_armorType.value);

                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnArmorEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("armor_type", (ArmorType)_armorType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnWeaponIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("main_caliber", _mainCaliber.value);
                _context.Config.SetInt("ammo_capacity", _ammoCapacity.value);
                _context.Config.SetBool("auto_loader", _autoLoader.value);
                _context.Config.SetEnum("ammo_type", (AmmoType)_ammoType.value);

                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnWeaponFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("main_reload_time", _mainReloadTime.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnWeaponBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("auto_loader", _autoLoader.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnWeaponEnumConfigChanged(ChangeEvent<Enum> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetEnum("ammo_type", (AmmoType)_ammoType.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnTrackIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("road_wheels", _roadWheels.value);
                _context.Config.SetBool("track_stabilization", _trackStabilization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnTrackFloatConfigChanged(ChangeEvent<float> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetFloat("track_width", _trackWidth.value);
                _context.Config.SetFloat("track_length", _trackLength.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnTrackBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("track_stabilization", _trackStabilization.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnCrewIntConfigChanged(ChangeEvent<int> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetInt("crew_count", _crewCount.value);
                _context.Config.SetBool("crew_injury_simulation", _crewInjurySimulation.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnCrewStringConfigChanged(ChangeEvent<string> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetString("crew_positions", _crewPositions.value);
                _context.NotifyConfigChanged(_context.Config);
            }
        }

        private void OnCrewBoolConfigChanged(ChangeEvent<bool> evt)
        {
            if (_context.Config != null)
            {
                _context.Config.SetBool("crew_injury_simulation", _crewInjurySimulation.value);
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
    public enum ArmorType
    {
        RolledHomogeneousSteel,
        Composite,
        Reactive,
        ActiveProtection,
        CompositeReactive
    }

    public enum AmmoType
    {
        APFSDS,      // Armor-Piercing Fin-Stabilized Discarding Sabot
        HEAT,        // High-Explosive Anti-Tank
        HE,          // High-Explosive
        APHE,        // Armor-Piercing High-Explosive
        Canister,    // Anti-personnel
        Smoke        // Smoke rounds
    }
    #endregion
}