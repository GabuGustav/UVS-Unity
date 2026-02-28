using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UVS.Editor.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UVS.Editor.Modules
{
    public class RuntimeControllersModule : VehicleEditorModuleBase
    {
        private readonly Dictionary<Type, Toggle> _toggles = new();
        private Button _autoAddButton;
        private Button _autoWireButton;
        private Button _validateButton;
        private Button _recommendedButton;

        public override string ModuleId => "runtime";
        public override string DisplayName => "Runtime Controllers";
        public override int Priority => 18;
        public override bool RequiresVehicle => true;

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

            var header = new Label("Runtime Controllers")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            container.Add(header);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 10 } };
            _recommendedButton = new Button(SetRecommended) { text = "Set Recommended" };
            _autoAddButton = new Button(AutoAddControllers) { text = "Auto-Add Controllers" };
            _autoWireButton = new Button(AutoWireReferences) { text = "Auto-Wire References" };
            _validateButton = new Button(ValidateRuntimeSetup) { text = "Validate Runtime Setup" };
            buttonRow.Add(_recommendedButton);
            buttonRow.Add(_autoAddButton);
            buttonRow.Add(_autoWireButton);
            buttonRow.Add(_validateButton);
            container.Add(buttonRow);

            BuildToggleList(container);
            return container;
        }

        protected override ValidationResult ValidateModule()
{
    return ValidationResult.Success();
}


        protected override void OnModuleActivated()
        {
            RefreshTogglesFromPrefab();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            RefreshTogglesFromPrefab();
        }

        private void BuildToggleList(VisualElement parent)
        {
            _toggles.Clear();
            AddToggle(parent, typeof(VehiclePhysicsRouter), "VehiclePhysicsRouter");
            AddToggle(parent, typeof(LandVehicleController), "LandVehicleController");
            AddToggle(parent, typeof(TrackDriveController), "TrackDriveController");
            AddToggle(parent, typeof(TankController), "TankController");
            AddToggle(parent, typeof(ArticulatedTruckController), "ArticulatedTruckController");
            AddToggle(parent, typeof(AircraftController), "AircraftController");
            AddToggle(parent, typeof(VTOLController), "VTOLController");
            AddToggle(parent, typeof(BoatController), "BoatController");
            AddToggle(parent, typeof(TrainController), "TrainController");
            AddToggle(parent, typeof(LowriderController), "LowriderController");
            AddToggle(parent, typeof(WheelVisualSync), "WheelVisualSync");
            AddToggle(parent, typeof(VehicleSeatManager), "VehicleSeatManager");
            AddToggle(parent, typeof(UVSLocalMultiplayerCoordinator), "UVSLocalMultiplayerCoordinator");
            AddToggle(parent, typeof(VehicleInputHub), "VehicleInputHub");
            AddToggle(parent, typeof(VehicleAIController), "VehicleAIController");
            AddToggle(parent, typeof(VehicleSensorRig), "VehicleSensorRig");
        }

        private void AddToggle(VisualElement parent, Type type, string label)
        {
            var t = new Toggle(label) { value = false };
            parent.Add(t);
            _toggles[type] = t;
        }

        private void RefreshTogglesFromPrefab()
        {
            if (_context?.SelectedPrefab == null) return;
            var root = _context.SelectedPrefab;

            foreach (var kvp in _toggles)
            {
                kvp.Value.value = root.GetComponent(kvp.Key) != null;
            }
        }

        private void SetRecommended()
        {
            if (_context?.CurrentConfig == null) return;

            foreach (var kvp in _toggles)
                kvp.Value.value = GetRecommendedState(kvp.Key, _context.CurrentConfig);
        }

        private bool GetRecommendedState(Type type, VehicleConfig config)
        {
            if (type == typeof(VehiclePhysicsRouter)) return true;
            if (type == typeof(VehicleInputHub)) return true;
            if (type == typeof(WheelVisualSync)) return config.wheels != null && config.wheels.Count > 0;
            if (type == typeof(VehicleSeatManager)) return config.seats != null && config.seats.Count > 0;
            if (type == typeof(UVSLocalMultiplayerCoordinator))
                return (config.seats != null && config.seats.Count > 0) &&
                       (config.multiplayer.enableLocalSplitScreen || config.multiplayer.enableOnline);

            bool isLand = config.vehicleType == VehicleConfig.VehicleType.Land;
            bool isAir = config.vehicleType == VehicleConfig.VehicleType.Air;
            bool isWater = config.vehicleType == VehicleConfig.VehicleType.Water;
            bool isRail = config.vehicleType == VehicleConfig.VehicleType.Rail;
            bool isTank = isLand && config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Tank;
            bool isLowrider = isLand && config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Lowrider;
            bool isVTOL = isAir && config.IsSpecialized && config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL;
            bool isArticulated = isLand && (config.landCategory == VehicleConfig.LandVehicleCategory.Articulated_Truck ||
                                            config.landCategory == VehicleConfig.LandVehicleCategory.Semi_Truck ||
                                            config.landCategory == VehicleConfig.LandVehicleCategory.Tractor);

            bool hasTrackRoles = config.wheels != null && config.wheels.Any(w =>
                w.role == VehicleConfig.WheelRole.TrackLeft || w.role == VehicleConfig.WheelRole.TrackRight);

            if (type == typeof(LandVehicleController)) return isLand && !isTank && !hasTrackRoles;
            if (type == typeof(TrackDriveController)) return isLand && hasTrackRoles && !isTank;
            if (type == typeof(TankController)) return isTank;
            if (type == typeof(ArticulatedTruckController)) return isArticulated;
            if (type == typeof(AircraftController)) return isAir && !isVTOL;
            if (type == typeof(VTOLController)) return isVTOL;
            if (type == typeof(BoatController)) return isWater;
            if (type == typeof(TrainController)) return isRail;
            if (type == typeof(LowriderController)) return isLowrider;

            return false;
        }

        private void AutoAddControllers()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var kvp in _toggles)
                {
                    if (!kvp.Value.value) continue;
                    if (prefabRoot.GetComponent(kvp.Key) == null)
                        prefabRoot.AddComponent(kvp.Key);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                LogMessage("Controllers added.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void AutoWireReferences()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var config = _context.CurrentConfig;

                foreach (var c in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
                {
switch (c)
{
    case TankController tank:
        tank.config = config; break;

    case TrackDriveController track:
        track.config = config; break;

    case LowriderController low:
        low.config = config; break;

    case LandVehicleController land:
        land.config = config; break;

    case VTOLController vtol:
        vtol.config = config; break;

    case AircraftController air:
        air.config = config; break;

    case BoatController boat:
        boat.config = config; break;

    case TrainController train:
        train.config = config; break;

    case ArticulatedTruckController articulated:
        articulated.config = config; break;

    case VehiclePhysicsRouter router:
        router.config = config; break;

    case VehicleSeatManager seats:
        seats.config = config; break;

    case VehicleAIController ai:
        ai.config = config; break;
}

                }

                var wheelSync = prefabRoot.GetComponent<WheelVisualSync>();
                if (wheelSync != null)
                    AutoAssignWheelVisuals(prefabRoot, config, wheelSync);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                LogMessage("References auto-wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void AutoAssignWheelVisuals(GameObject root, VehicleConfig config, WheelVisualSync sync)
        {
            var colliders = root.GetComponentsInChildren<WheelCollider>(true);
            var pairs = new List<WheelVisualSync.WheelPair>();

            foreach (var col in colliders)
            {
                if (col == null) continue;
                Transform visual = null;

                string baseName = col.name.EndsWith("_collider") ? col.name[..^9] : col.name;
                var byName = root.transform.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.Equals(baseName, StringComparison.OrdinalIgnoreCase));

                if (byName != null)
                    visual = byName;
                else if (config.wheels != null && config.wheels.Count > 0)
                {
                    var local = col.transform.localPosition;
                    var match = config.wheels.OrderBy(w => (w.localPosition - local).sqrMagnitude).FirstOrDefault();
                    if (match != null)
                        visual = root.transform.Find(match.partPath);
                }

                if (visual != null)
                {
                    pairs.Add(new WheelVisualSync.WheelPair { collider = col, visualWheel = visual });
                }
            }

            sync.wheels = pairs.ToArray();
            EditorUtility.SetDirty(sync);
        }

        private void ValidateRuntimeSetup()
        {
            if (_context?.CurrentConfig == null || _context.SelectedPrefab == null)
            {
                LogError("No vehicle loaded.");
                return;
            }

            foreach (var kvp in _toggles)
            {
                bool recommended = GetRecommendedState(kvp.Key, _context.CurrentConfig);
                if (recommended && !_context.SelectedPrefab.GetComponent(kvp.Key))
                    LogWarning($"Missing {kvp.Key.Name}");
            }
        }
    }
}
