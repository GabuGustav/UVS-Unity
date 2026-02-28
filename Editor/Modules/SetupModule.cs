using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UVS.Editor.Core;
using System.Collections.Generic;

namespace UVS.Editor.Modules
{
    public class SetupModule : VehicleEditorModuleBase
    {
        private Button _autoSetupButton;
        private Button _validateButton;

        public override string ModuleId => "setup";
        public override string DisplayName => "Setup";
        public override int Priority => 15;
        public override bool RequiresVehicle => true;
        public override bool RequiresSpecializedCategory => false;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => false;

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

            var header = new Label("Physics Setup")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15
                }
            };
            container.Add(header);

            _autoSetupButton = new Button(AutoSetupPhysics) { text = "Auto-Setup Physics" };
            _validateButton = new Button(ValidateSetup) { text = "Validate Setup" };

            container.Add(_autoSetupButton);
            container.Add(_validateButton);

            return container;
        }

        protected override ValidationResult ValidateModule()
        {
            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            // No-op
        }

        protected override void OnModuleActivated()
        {
            // No-op
        }

        private void AutoSetupPhysics()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            if (string.IsNullOrEmpty(path))
            {
                LogError("Selected prefab has no asset path.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rb = prefabRoot.GetComponent<Rigidbody>() ?? prefabRoot.AddComponent<Rigidbody>();
                rb.mass = _context.CurrentConfig.body.mass > 0 ? _context.CurrentConfig.body.mass : 1200f;
                rb.linearDamping = _context.CurrentConfig.body.dragCoefficient;
                rb.angularDamping = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                var router = prefabRoot.GetComponent<VehiclePhysicsRouter>() ?? prefabRoot.AddComponent<VehiclePhysicsRouter>();
                router.config = _context.CurrentConfig;

                EnsureControllers(prefabRoot, _context.CurrentConfig);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                LogMessage("Auto-setup completed.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void EnsureControllers(GameObject root, VehicleConfig config)
        {
            // Disable all if they exist, router will manage at runtime
            bool hasTrackRoles = config.wheels != null && config.wheels.Exists(w =>
                w.role == VehicleConfig.WheelRole.TrackLeft || w.role == VehicleConfig.WheelRole.TrackRight);

            bool isTank = config.vehicleType == VehicleConfig.VehicleType.Land && config.IsSpecialized &&
                          config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Tank;
            bool isArticulated = config.vehicleType == VehicleConfig.VehicleType.Land &&
                                 (config.landCategory == VehicleConfig.LandVehicleCategory.Articulated_Truck ||
                                  config.landCategory == VehicleConfig.LandVehicleCategory.Semi_Truck ||
                                  config.landCategory == VehicleConfig.LandVehicleCategory.Tractor);
            bool isRail = config.vehicleType == VehicleConfig.VehicleType.Rail;

            EnsureComponent<LandVehicleController>(root, config.vehicleType == VehicleConfig.VehicleType.Land && !isTank && !hasTrackRoles);
            EnsureComponent<TankController>(root, isTank);
            EnsureComponent<TrackDriveController>(root, config.vehicleType == VehicleConfig.VehicleType.Land && hasTrackRoles && !isTank);
            EnsureComponent<AircraftController>(root, config.vehicleType == VehicleConfig.VehicleType.Air && !(config.IsSpecialized && config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL));
            EnsureComponent<VTOLController>(root, config.vehicleType == VehicleConfig.VehicleType.Air && config.IsSpecialized && config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL);
            EnsureComponent<BoatController>(root, config.vehicleType == VehicleConfig.VehicleType.Water);
            EnsureComponent<ArticulatedTruckController>(root, isArticulated);
            EnsureComponent<TrainController>(root, isRail);
            EnsureComponent<VehicleInputHub>(root, true);

            if (config.vehicleType == VehicleConfig.VehicleType.Land && config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Lowrider)
            {
                EnsureComponent<LowriderController>(root, true);
            }

            if (config.wheels != null && config.wheels.Count > 0)
            {
                EnsureComponent<WheelVisualSync>(root, true);
            }
        }

        private void EnsureComponent<T>(GameObject root, bool enable) where T : Behaviour
        {
            var comp = root.GetComponent<T>() ?? root.AddComponent<T>();
            comp.enabled = enable;
        }

        private void ValidateSetup()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            var root = _context.SelectedPrefab;
            var errors = new List<string>();
            var warnings = new List<string>();

            if (root.GetComponent<Rigidbody>() == null)
                errors.Add("Missing Rigidbody");

            if (root.GetComponent<VehiclePhysicsRouter>() == null)
                warnings.Add("Missing VehiclePhysicsRouter");

            switch (_context.CurrentConfig.vehicleType)
            {
                case VehicleConfig.VehicleType.Land:
                    if (root.GetComponent<WheelCollider>() == null)
                        warnings.Add("No WheelCollider found");
                    bool hasTrackRoles = _context.CurrentConfig.wheels != null && _context.CurrentConfig.wheels.Exists(w =>
                        w.role == VehicleConfig.WheelRole.TrackLeft || w.role == VehicleConfig.WheelRole.TrackRight);
                    bool isArticulated = _context.CurrentConfig.landCategory == VehicleConfig.LandVehicleCategory.Articulated_Truck ||
                                         _context.CurrentConfig.landCategory == VehicleConfig.LandVehicleCategory.Semi_Truck ||
                                         _context.CurrentConfig.landCategory == VehicleConfig.LandVehicleCategory.Tractor;

                    if (_context.CurrentConfig.IsSpecialized && _context.CurrentConfig.specializedLand == VehicleConfig.SpecializedLandVehicleType.Tank)
                    {
                        if (root.GetComponent<TankController>() == null)
                            warnings.Add("Missing TankController");
                    }
                    else if (hasTrackRoles)
                    {
                        if (root.GetComponent<TrackDriveController>() == null)
                            warnings.Add("Missing TrackDriveController");
                    }
                    else if (root.GetComponent<LandVehicleController>() == null)
                    {
                        warnings.Add("Missing LandVehicleController");
                    }

                    if (isArticulated && root.GetComponent<ArticulatedTruckController>() == null)
                        warnings.Add("Missing ArticulatedTruckController");
                    break;

                case VehicleConfig.VehicleType.Air:
                    if (_context.CurrentConfig.IsSpecialized && _context.CurrentConfig.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL)
                    {
                        if (root.GetComponent<VTOLController>() == null)
                            warnings.Add("Missing VTOLController");
                    }
                    else if (root.GetComponent<AircraftController>() == null)
                    {
                        warnings.Add("Missing AircraftController");
                    }
                    break;

                case VehicleConfig.VehicleType.Water:
                    if (root.GetComponent<BoatController>() == null)
                        warnings.Add("Missing BoatController");
                    if (_context.CurrentConfig.water.buoyancyPoints == null || _context.CurrentConfig.water.buoyancyPoints.Count == 0)
                        warnings.Add("No buoyancy points configured");
                    break;

                case VehicleConfig.VehicleType.Rail:
                    if (root.GetComponent<TrainController>() == null)
                        warnings.Add("Missing TrainController");
                    break;
            }

            if (root.GetComponent<WheelVisualSync>() == null && _context.CurrentConfig.wheels != null && _context.CurrentConfig.wheels.Count > 0)
                warnings.Add("Missing WheelVisualSync");

            foreach (var err in errors) LogError(err);
            foreach (var warn in warnings) LogWarning(warn);

            if (errors.Count == 0 && warnings.Count == 0)
                LogMessage("Setup validation passed.");
        }

        public override void OnModuleGUI() { }
    }
}
