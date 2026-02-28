using UnityEngine;

public class VehiclePhysicsRouter : MonoBehaviour
{
    public VehicleConfig config;
    public bool autoConfigureOnAwake = true;

    private void Awake()
    {
        if (autoConfigureOnAwake)
        {
            if (config == null)
            {
                config = GetComponent<LandVehicleController>()?.config
                    ?? GetComponent<TankController>()?.config
                    ?? GetComponent<AircraftController>()?.config
                    ?? GetComponent<VTOLController>()?.config
                    ?? GetComponent<BoatController>()?.config
                    ?? GetComponent<LowriderController>()?.config
                    ?? GetComponent<ArticulatedTruckController>()?.config
                    ?? GetComponent<TrainController>()?.config;
            }
            ApplyMode();
        }
    }

    public void ApplyMode()
    {
        if (config == null) return;

        SyncConfig();

        bool isLand = config.vehicleType == VehicleConfig.VehicleType.Land;
        bool isAir = config.vehicleType == VehicleConfig.VehicleType.Air;
        bool isWater = config.vehicleType == VehicleConfig.VehicleType.Water;
        bool isRail = config.vehicleType == VehicleConfig.VehicleType.Rail;

        bool isTank = isLand && config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Tank;
        bool isLowrider = isLand && config.IsSpecialized && config.specializedLand == VehicleConfig.SpecializedLandVehicleType.Lowrider;
        bool isVTOL = isAir && config.IsSpecialized && config.specializedAir == VehicleConfig.SpecializedAirVehicleType.VTOL;
        bool hasTrackRoles = config.wheels != null && config.wheels.Exists(w =>
            w.role == VehicleConfig.WheelRole.TrackLeft || w.role == VehicleConfig.WheelRole.TrackRight);
        bool useTrackDrive = isLand && hasTrackRoles && !isTank;
        bool isArticulated = isLand && (config.landCategory == VehicleConfig.LandVehicleCategory.Articulated_Truck ||
                                        config.landCategory == VehicleConfig.LandVehicleCategory.Semi_Truck ||
                                        config.landCategory == VehicleConfig.LandVehicleCategory.Tractor);

        SetEnabled<LandVehicleController>(isLand && !isTank && !useTrackDrive);
        SetEnabled<TankController>(isTank);
        SetEnabled<TrackDriveController>(useTrackDrive);
        SetEnabled<AircraftController>(isAir && !isVTOL);
        SetEnabled<VTOLController>(isVTOL);
        SetEnabled<BoatController>(isWater);
        SetEnabled<LowriderController>(isLowrider);
        SetEnabled<ArticulatedTruckController>(isArticulated);
        SetEnabled<TrainController>(isRail);
    }

    private void SyncConfig()
    {
        var land = GetComponent<LandVehicleController>();
        if (land != null && land.config == null) land.config = config;

        var tank = GetComponent<TankController>();
        if (tank != null && tank.config == null) tank.config = config;

        var track = GetComponent<TrackDriveController>();
        if (track != null && track.config == null) track.config = config;

        var air = GetComponent<AircraftController>();
        if (air != null && air.config == null) air.config = config;

        var vtol = GetComponent<VTOLController>();
        if (vtol != null && vtol.config == null) vtol.config = config;

        var boat = GetComponent<BoatController>();
        if (boat != null && boat.config == null) boat.config = config;

        var lowrider = GetComponent<LowriderController>();
        if (lowrider != null && lowrider.config == null) lowrider.config = config;

        var articulated = GetComponent<ArticulatedTruckController>();
        if (articulated != null && articulated.config == null) articulated.config = config;

        var train = GetComponent<TrainController>();
        if (train != null && train.config == null) train.config = config;
    }

    private void SetEnabled<T>(bool enabled) where T : Behaviour
    {
        var comp = GetComponent<T>();
        if (comp != null) comp.enabled = enabled;
    }
}

