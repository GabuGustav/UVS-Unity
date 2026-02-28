using UnityEngine;
using System;
using System.Collections.Generic;
using UVS.Shared;

[CreateAssetMenu(menuName = "UVS/Vehicle Config")]
public class VehicleConfig : ScriptableObject
{
    public string prefabGuid;
    public GameObject prefabReference;
    public VehicleInputProfile inputProfileOverride;
    public string id;
    public List<string> legacyIds = new();
    public string vehicleName;
    public string authorName;
    public VehicleClassificationData classification = new();

    // ============ VEHICLE CLASSIFICATION ============
    [Header("Vehicle Classification")]
    public VehicleType vehicleType = VehicleType.Land;

    // Category enums - only one used based on vehicleType
    public LandVehicleCategory landCategory = LandVehicleCategory.Standard;
    public AirVehicleCategory airCategory = AirVehicleCategory.Standard;
    public WaterVehicleCategory waterCategory = WaterVehicleCategory.Standard;
    public SpaceVehicleCategory spaceCategory = SpaceVehicleCategory.Standard;
    public RailVehicleCategory railCategory = RailVehicleCategory.Train;

    // Specialized enums - only used if category is "Specialized"
    public SpecializedLandVehicleType specializedLand = SpecializedLandVehicleType.Construction;
    public SpecializedAirVehicleType specializedAir = SpecializedAirVehicleType.VTOL;

    [Serializable]
    public class VehicleClassificationData
    {
        public string typeId = "land";
        public string categoryId = "standard";
        public string subcategoryId = "";
        public List<string> tags = new();
    }

    // Helper properties
    public bool IsSpecialized
    {
        get
        {
            return vehicleType switch
            {
                VehicleType.Land => landCategory == LandVehicleCategory.Specialized,
                VehicleType.Air => airCategory == AirVehicleCategory.Specialized,
                _ => false
            };
        }
    }

    public string GetCurrentCategory()
    {
        return vehicleType switch
        {
            VehicleType.Land => landCategory.ToString(),
            VehicleType.Air => airCategory.ToString(),
            VehicleType.Water => waterCategory.ToString(),
            VehicleType.Space => spaceCategory.ToString(),
            VehicleType.Rail => railCategory.ToString(),
            _ => "Standard"
        };
    }

    public string GetCurrentSpecialized()
    {
        if (!IsSpecialized) return "";

        return vehicleType switch
        {
            VehicleType.Land => specializedLand.ToString(),
            VehicleType.Air => specializedAir.ToString(),
            _ => ""
        };
    }
    // ================================================

    // Dynamic properties storage for specialized modules
    private readonly Dictionary<string, object> _dynamicProperties = new();

    #region Helper Methods for Modules

    // Float helpers
    public void SetFloat(string key, float value)
    {
        _dynamicProperties[key] = value;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is float floatValue)
            return floatValue;
        return defaultValue;
    }

    // Int helpers
    public void SetInt(string key, int value)
    {
        _dynamicProperties[key] = value;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is int intValue)
            return intValue;
        return defaultValue;
    }

    // Bool helpers
    public void SetBool(string key, bool value)
    {
        _dynamicProperties[key] = value;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is bool boolValue)
            return boolValue;
        return defaultValue;
    }

    // String helpers
    public void SetString(string key, string value)
    {
        _dynamicProperties[key] = value;
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is string stringValue)
            return stringValue;
        return defaultValue;
    }

    // Enum helpers
    public void SetEnum<T>(string key, T value) where T : Enum
    {
        _dynamicProperties[key] = value;
    }

    public T GetEnum<T>(string key, T defaultValue) where T : Enum
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is T enumValue)
            return enumValue;
        return defaultValue;
    }

    // Vector3 helpers
    public void SetVector3(string key, Vector3 value)
    {
        _dynamicProperties[key] = value;
    }

    public Vector3 GetVector3(string key, Vector3 defaultValue)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is Vector3 vectorValue)
            return vectorValue;
        return defaultValue;
    }

    // Color helpers
    public void SetColor(string key, Color value)
    {
        _dynamicProperties[key] = value;
    }

    public Color GetColor(string key, Color defaultValue)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is Color colorValue)
            return colorValue;
        return defaultValue;
    }

    // Check if property exists
    public bool HasProperty(string key)
    {
        return _dynamicProperties.ContainsKey(key);
    }

    // Remove property
    public void RemoveProperty(string key)
    {
        _dynamicProperties.Remove(key);
    }

    // Clear all dynamic properties
    public void ClearDynamicProperties()
    {
        _dynamicProperties.Clear();
    }

    // Get all property keys
    public IEnumerable<string> GetAllPropertyKeys()
    {
        return _dynamicProperties.Keys;
    }

    #endregion

    // Part classification for scanning/tagging
    [Serializable]
    public class VehiclePartClassification
    {
        public string partPath;
        public VehiclePartType partType;
    }
    public List<VehiclePartClassification> partClassifications = new();

    public enum VehiclePartType
    {
        Body, Wheel, Suspension, Engine, Drivetrain,
        Brake, Light, Interior, Glass, Exhaust,
        Mirror, FuelSystem, Electrical,
        Miscellaneous, SteeringWheel,
        Transmission, Door, Turbo
    }

    public enum VehicleType { Land, Air, Water, Rail, Space, Fictional }
    public enum VehicleDriveModel { Realistic, Arcade }

    public enum SpecializedLandVehicleType
    {
        Construction, Tank, Lowrider
    }

    public enum SpecializedAirVehicleType
    {
        VTOL, Drone, Glider
    }

    public enum LandVehicleCategory
    {
        Sedan, SUV, Truck, Motorcycle, SportsCar,
        OffRoad, Bus, Van, Coupe, Convertible,
        Hatchback, Wagon, Electric,
        Articulated_Truck, Semi_Truck, Tractor,
        Standard, Classic, Specialized
    }

    public enum AirVehicleCategory
    {
        Airplane, Helicopter, Glider,
        Standard, Specialized
    }

    public enum WaterVehicleCategory
    {
        Boat, Ship, Submarine, JetSki, Sailboat,
        Standard,
    }

    public enum SpaceVehicleCategory
    {
        Shuttle, Rover, Satellite, SpaceStation, Fighter,
        Standard, Spaceship
    }
    
    public enum RailVehicleCategory
    {
        Train, Tram, Metro, Standard
    }

    // General vehicle measurements
    [Serializable]
    public class VehicleMeasurements
    {
        public float length;
        public float width;
        public float height;
        public float wheelbase;
        public float frontTrackWidth;
        public float rearTrackWidth;
        public float rideHeight;
        public float groundClearance;
        public Vector3 centerOfMassEstimate;
    }
    public VehicleMeasurements measurements = new();

    // Per-wheel settings
    [Serializable]
    public class WheelSettings
    {
        public string partPath;
        public float radius;
        public float SuspensionDistance;
        public float width;

        public Vector3 localPosition;
        public bool isPowered;
        public bool isSteering;
        public WheelRole role = WheelRole.Free;
    }
    public List<WheelSettings> wheels = new();

    public enum WheelRole
    {
        FrontSteer,
        RearDrive,
        TrackLeft,
        TrackRight,
        Free
    }

    // Engine
    [Serializable]
    public class EngineSettings
    {
        [Header("Performance")]
        public float horsepower = 150f;
        public float torque = 200f;
        public int cylinderCount = 4;
        public float displacement = 2.0f;
        public float redlineRPM = 7000f;
        public float idleRPM = 800f;
        public float engineRPM = 5000f;

        [Header("Drivetrain")]
        public Drivetrain drivetrain = Drivetrain.RWD;

        [Header("Audio Clips")]
        public AudioClip startClip;
        public AudioClip stopClip;
        public AudioClip idleClip;
        public AudioClip highRpmClip;
        public AudioClip lowRpmClip;
        public AudioClip shiftClip;
        public AudioClip lowOffClip;
        public AudioClip highOffClip;

        public enum Drivetrain
        {
            AWD, FWD, RWD
        }
    }
    public EngineSettings engine = new();

    [Serializable]
    public class Lowrider
    {
        public bool enableHydraulics = false;
        public float hopForce = 5000f;
        public float slamForce = 8000f;
        public float tiltSpeed = 2000f;
        public float maxLiftHeight = 0.6f;
        public float maxVerticalVelocity = 4f;
        public float liftSpring = 1200f;
        public float liftDamping = 200f;
        public float maxHopImpulsePerSecond = 12000f;
        public bool enableDanceMode = false;
        public float danceSpeed = 1.0f;
        public float danceHeight = 0.5f;
        public float bounceAmplitude = 0.2f;
        public float bounceFrequency = 2.0f;
        public int springCoilCount;
        public bool showCoiledSprings;
        public float maxTiltAngle = 30f;
        public float springThickness;
        public Color springColor;
    }
    public Lowrider lowrider = new();

    // Turbo System
    [Serializable]
    public class TurboSettings
    {
        [Header("Turbo Performance")]
        public bool hasTurbo = false;
        public float boostAmount = 0.5f;
        public float spoolTime = 2.0f;
        public float maxBoostPressure = 1.5f;
        public float boostThresholdRPM = 3000f;

        [Header("Turbo Audio")]
        public AudioClip whistleClip;
        public AudioClip blowoffClip;
    }
    public TurboSettings turbo = new();

    // Brakes
    [Serializable]
    public class BrakeSettings
    {
        public float frontDiscDiameter = 300f;
        public float rearDiscDiameter = 280f;
        public bool abs = true;
        public float brakeBias = 0.6f;
    }
    public BrakeSettings brakes = new();

    // Suspension
    [Serializable]
    public class SuspensionSettings
    {
        public float springStiffness = 35000f;
        public float damperStiffness = 4500f;
        public float antiRollBarStiffness = 5000f;
        public float suspensionTravel = 0.2f;
        public float suspensionDistance = 0.3f;
    }
    public SuspensionSettings suspension = new();

    [Serializable]
    public class DrivingAssistSettings
    {
        public float stopAssistSpeed = 1.2f;
        public float spinKillBrakeTorque = 1200f;
        public float rpmStopThreshold = 5f;

        public float reverseEngageSpeed = 1.2f;
        public float reverseExitSpeed = 2.0f;

        public bool autoFlipEnabled = true;
        public float flipMaxSpeed = 2.5f;
        public float flipAngleThreshold = 120f;
        public float flipTorque = 1800f;
        public float flipCooldown = 3f;
    }
    public DrivingAssistSettings drivingAssist = new();

    [Header("Drive Model")]
    public VehicleDriveModel driveModel = VehicleDriveModel.Realistic;

    [Serializable]
    public class ArticulationSettings
    {
        public Transform tractorHitch;
        public Transform trailerHitch;
        public float hitchYawLimit = 35f;
        public float hitchDamping = 8f;
        public float hitchSpring = 40f;
        public bool detachable = false;
    }
    public ArticulationSettings articulation = new();

    [Serializable]
    public class TrailerSettings
    {
        public bool hasTrailer = false;
        public bool poweredTrailer = false;
        public float trailerMass = 2500f;
        public float trailerBrakeStrength = 600f;
    }
    public TrailerSettings trailer = new();

    [Serializable]
    public class TrainSettings
    {
        public float maxSpeed = 30f;
        public float accel = 6f;
        public float brake = 10f;
        public float trackGauge = 1.435f;
        public bool useSignals = true;
    }
    public TrainSettings train = new();

    [Serializable]
    public class TrafficAISettings
    {
        public DriverProfile profile;
        public float targetSpeed = 14f;
        public float followDistance = 10f;
        public float laneChangeCooldown = 3f;
    }
    public TrafficAISettings trafficAI = new();

    [Serializable]
    public class TrackDriveSettings
    {
        public float trackTorqueMultiplier = 12f;
        public float trackBrakeStrength = 250f;
        [Range(0f, 1f)] public float steerBlend = 0.6f;
        public float trackDifferentialStrength = 0.8f;
        public float maxTrackSpeed = 25f;
        public float trackDrag = 0.15f;
    }
    public TrackDriveSettings trackDrive = new();

    [Serializable]
    public class SeatSettings
    {
        public bool allowMultipleOccupants = true;
        public bool allowPassengerInput = false;
        public int maxOccupants = 4;
        public float enterDistance = 2.5f;
        public float exitDistance = 2.5f;
        public float seatSwapCooldown = 0.5f;
    }
    public SeatSettings seatSettings = new();

    [Serializable]
    public class MultiplayerSettings
    {
        public bool enableLocalSplitScreen = true;
        public int localMaxPlayers = 4;
        public bool allowSharedVehicleOccupancy = true;
        public bool enableOnline = false;
        public string onlineProviderId = "ngo";
    }
    public MultiplayerSettings multiplayer = new();

    public enum WaterQualityTier
    {
        Low,
        Medium,
        High
    }

    [Serializable]
    public class WaterRenderSettings
    {
        public WaterQualityTier qualityTier = WaterQualityTier.Medium;
        public bool foamEnabled = true;
        public bool depthColorEnabled = true;
        public bool causticsEnabled = false;
    }
    public WaterRenderSettings waterRender = new();

    [Serializable]
    public class SeatAnchor
    {
        public string id;
        public SeatRole role = SeatRole.Driver;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Transform overrideTransform;
        public string entrySocketId;
        public string exitSocketId;
    }

    public enum SeatRole
    {
        Driver,
        Passenger
    }

    public List<SeatAnchor> seats = new();

    // Body
    [Serializable]
    public class BodySettings
    {
        public float mass = 1200f;
        public float dragCoefficient = 0.3f;
        public float frontalArea = 2.5f;
        public Vector3 centerOfMassOffset = Vector3.zero;
    }
    public BodySettings body = new();

    // Transmission
    [Serializable]
    public class TransmissionSettings
    {
        public TransmissionType type = TransmissionType.Manual;
        public int gearCount = 6;
        public float[] gearRatios = { 3.5f, 2.5f, 1.8f, 1.3f, 1.0f, 0.8f };
        public float finalDriveRatio = 3.2f;
        public float reverseGearRatio = -3.0f;
        public float shiftTime = 0.3f;

        public enum TransmissionType
        {
            Manual, Automatic, Sequential, CVT
        }
    }
    public TransmissionSettings transmission = new();

    // Fuel System
    [Serializable]
    public class FuelSystemSettings
    {
        [Header("Fuel Tank")]
        public float fuelCapacity = 60f;
        public float currentFuel = 60f;
        public float fuelConsumptionRate = 0.1f;

        [Header("Fuel Components")]
        public string fuelTankPath = "";
        public string fuelPumpPath = "";
        public string fuelFilterPath = "";
        public string fuelInjectorsPath = "";

        [Header("Fuel Properties")]
        public FuelType fuelType = FuelType.Gasoline;
        public float octaneRating = 95f;
        public float fuelDensity = 0.75f;

        public enum FuelType
        {
            Gasoline,
            Diesel,
            Electric,
            Hybrid,
            Hydrogen,
            Biofuel
        }
    }
    public FuelSystemSettings fuelSystem = new();

    // Steering
    [Serializable]
    public class SteeringSettings
    {
        public float maxSteeringAngle = 30f;
        public float steeringRatio = 16f;
        public bool powerSteering = true;
        public float steeringAssist = 0.5f;
    }
    public SteeringSettings steering = new();

    // Electronics
    [Serializable]
    public class ElectronicsSettings
    {
        public bool hasABS = true;
        public bool hasTCS = true;
        public bool hasESP = true;
        public bool hasLaunchControl = false;
        public bool hasCruiseControl = true;
    }
    public ElectronicsSettings electronics = new();

    // Performance Profiles
    [Serializable]
    public class PerformanceProfile
    {
        public string profileName;
        public float torqueMultiplier = 1f;
        public float suspensionStiffnessMultiplier = 1f;
        public float steeringResponsiveness = 1f;
        public bool enableAllAssists = true;
    }

    public List<PerformanceProfile> performanceProfiles = new()
    {
        new PerformanceProfile { profileName = "Comfort", torqueMultiplier = 0.8f, suspensionStiffnessMultiplier = 0.7f, steeringResponsiveness = 0.8f },
        new PerformanceProfile { profileName = "Sports", torqueMultiplier = 1.0f, suspensionStiffnessMultiplier = 1.0f, steeringResponsiveness = 1.0f },
        new PerformanceProfile { profileName = "Race", torqueMultiplier = 1.2f, suspensionStiffnessMultiplier = 1.3f, steeringResponsiveness = 1.2f },
        new PerformanceProfile { profileName = "Drift", torqueMultiplier = 1.1f, suspensionStiffnessMultiplier = 0.8f, steeringResponsiveness = 1.1f }
    };

    // Audio Mix
    [Serializable]
    public class AudioMixSettings
    {
        [Range(0f, 1f)]
        public float engineVolume = 0.8f;
        [Range(0f, 1f)]
        public float turboVolume = 0.5f;
        [Range(0f, 1f)]
        public float exhaustVolume = 0.6f;
        [Range(0f, 1f)]
        public float tireVolume = 0.4f;
        public AudioClip collisionClip;
        public AudioClip gearGrindClip;
    }
    public AudioMixSettings audioMix = new();

    [Serializable]
    public class AmmoTypeData
    {
        public string name;
        public float damage;
        public float speed;
    }
    public List<AmmoTypeData> ammoTypes = new();

    // Damage System
    [Serializable]
    public class DamageSettings
    {
        public bool enableDamage = true;
        public float maxHealth = 1000f;
        public float collisionThreshold = 5f;
        public float damageMultiplier = 1f;
        public bool visualDamage = true;
    }
    public DamageSettings damage = new();

    // Air physics
    [Serializable]
    public class AirSettings
    {
        [Header("Aerodynamics")]
        public float wingArea = 20f;
        public float liftCoefficient = 1.0f;
        public float dragCoefficient = 0.03f;
        public float airDensity = 1.225f;

        [Header("Thrust")]
        public float maxThrust = 20000f;

        [Header("Control Torques")]
        public float pitchTorque = 5000f;
        public float rollTorque = 6000f;
        public float yawTorque = 3000f;
    }
    public AirSettings air = new();

    [Serializable]
    public class VTOLSettings
    {
        [Header("Vectoring")]
        public float thrustVectoringRange = 45f;
        public float vectoringSpeed = 30f;
        public bool autoVectoring = true;
        public float vectoringResponseTime = 0.2f;

        [Header("Flight Modes")]
        public FlightMode defaultFlightMode = FlightMode.Hover;
        public bool autoTransition = true;
        public float transitionSpeed = 80f;
        public float transitionAltitude = 100f;

        [Header("Hover")]
        public float hoverHeight = 15f;
        public float hoverStability = 0.8f;
        public bool autoHover = true;
        public float hoverPowerRequirement = 75f;

        [Header("Vertical Flight")]
        public float verticalClimbRate = 8f;
        public float verticalDescentRate = 5f;
        public float maxVerticalSpeed = 15f;
        public bool verticalStabilization = true;

        [Header("Engines")]
        public int engineCount = 4;
        public float enginePower = 1500f;
        public float engineResponseTime = 0.5f;
        public bool engineSync = true;

        [Header("Safety")]
        public bool emergencyAutoLand = true;
        public float emergencyAltitude = 50f;
        public bool stallProtection = true;
        public float stallSpeed = 40f;

        [Header("Control")]
        public float controlSensitivity = 1.0f;
        public float controlDeadzone = 0.1f;
        public bool assistedFlying = true;
        public ControlScheme controlScheme = ControlScheme.Standard;
    }
    public VTOLSettings vtol = new();

    [Serializable]
    public class WaterSettings
    {
        [Header("Buoyancy")]
        public float waterDensity = 1000f;
        public float buoyancyForce = 1f;
        public List<BuoyancyPoint> buoyancyPoints = new();

        [Header("Drag")]
        public float linearDrag = 1.5f;
        public float angularDrag = 1f;

        [Header("Propulsion")]
        public float propulsionForce = 5000f;
        public float turnTorque = 2000f;
    }

    [Serializable]
    public class BuoyancyPoint
    {
        public Vector3 localPosition;
        public float volume = 1f;
        public float maxSubmersion = 1f;
    }
    public WaterSettings water = new();

    [Serializable]
    public class TankSettings
    {
        [Header("Turret")]
        public float turretRotationSpeed = 45f;
        public float turretElevationSpeed = 25f;
        public float turretMaxElevation = 20f;
        public float turretMinElevation = -10f;
        public bool turretStabilization = true;

        [Header("Armor")]
        public int frontArmorThickness = 120;
        public int sideArmorThickness = 80;
        public int rearArmorThickness = 40;
        public int turretArmorThickness = 150;
        public ArmorType armorType = ArmorType.RolledHomogeneousSteel;

        [Header("Weapon")]
        public int mainCaliber = 120;
        public float mainReloadTime = 8.5f;
        public int ammoCapacity = 40;
        public bool autoLoader = true;
        public AmmoType ammoType = AmmoType.APFSDS;

        [Header("Tracks")]
        public float trackWidth = 0.65f;
        public float trackLength = 4.2f;
        public int roadWheels = 7;
        public bool trackStabilization = true;

        [Header("Crew")]
        public int crewCount = 4;
        public string crewPositions = "Commander, Gunner, Loader, Driver";
        public bool crewInjurySimulation = true;
    }
    public TankSettings tank = new();

    [Serializable]
    public class ConstructionSettings
    {
        [Header("Equipment")]
        public EquipmentType equipmentType = EquipmentType.Excavator;
        public string equipmentModel = "CAT 320D";
        public float operatingWeight = 22000f;
        public float groundClearance = 450f;

        [Header("Hydraulics")]
        public float hydraulicPressure = 280f;
        public float hydraulicFlowRate = 205f;
        public float hydraulicResponseTime = 0.3f;
        public bool hydraulicOverloadProtection = true;
        public float hydraulicCoolingRate = 0.8f;

        [Header("Arm")]
        public float armLength = 6.2f;
        public float armMaxAngle = 160f;
        public float armMinAngle = -30f;
        public float armExtensionSpeed = 0.8f;
        public float armRotationSpeed = 12f;

        [Header("Attachment")]
        public float bucketCapacity = 1.2f;
        public float bucketWidth = 1.1f;
        public float bucketForce = 140f;
        public AttachmentType attachmentType = AttachmentType.StandardBucket;
        public float attachmentWeight = 850f;

        [Header("Stability")]
        public float stabilityBase = 2.8f;
        public float stabilityMargin = 0.3f;
        public bool autoStabilization = true;
        public float outriggerExtension = 3.5f;
        public bool outriggerAutoLevel = true;

        [Header("Safety")]
        public bool loadMonitoring = true;
        public float maxLoadCapacity = 3500f;
        public float safetyFactor = 1.5f;
        public bool operatorPresence = true;
        public bool emergencyStop = true;

        [Header("Environment")]
        public float operatingTemperature = 45f;
        public float windResistance = 50f;
        public float slopeCapability = 30f;
        public TerrainType terrainType = TerrainType.Mixed;
    }
    public ConstructionSettings construction = new();

    [Serializable]
    public class DeformationSettings
    {
        [Header("Deformation")]
        public bool enableDeformation = true;
        public float deformationStrength = 1.0f;
        public float deformationRadius = 0.5f;
        public DeformationType deformationType = DeformationType.Mesh;
        public float meshDetail = 0.8f;

        [Header("Damage")]
        public bool enableDamage = true;
        public float damageThreshold = 50f;
        public float damageMultiplier = 1.2f;
        public DamageModel damageModel = DamageModel.Realistic;
        public bool progressiveDamage = true;

        [Header("Material")]
        public float materialStrength = 400f;
        public float materialDuctility = 0.6f;
        public float materialHardness = 200f;
        public MaterialType materialType = MaterialType.Steel;
        public float corrosionResistance = 0.7f;

        [Header("Repair")]
        public bool enableRepair = true;
        public float repairSpeed = 1.0f;
        public float repairCost = 100f;
        public RepairMethod repairMethod = RepairMethod.Welding;
        public bool visualRepair = true;

        [Header("Visual")]
        public bool showDamage = true;
        public Color damageColor = Color.red;
        public float damageOpacity = 0.7f;
        public DamageTexture damageTexture = DamageTexture.Scratches;
        public bool particleEffects = true;

        [Header("Performance")]
        public int maxDeformations = 1000;
        public float updateFrequency = 30f;
        public bool lodDeformation = true;
        public int lodLevels = 4;
        public bool cullingOptimization = true;

        [Header("Safety")]
        public bool safetyOverride = false;
        public float maxDeformationLimit = 0.8f;
        public bool integrityMonitoring = true;
        public float failureThreshold = 0.9f;
        public bool emergencyStop = true;
    }
    public DeformationSettings deformation = new();

    // Public property for VehicleID compatibility
    public string VehicleID => id;

    // Helper methods
    public float GetTotalPower()
    {
        float basePower = engine.horsepower;
        if (turbo.hasTurbo)
        {
            basePower *= (1f + turbo.boostAmount);
        }
        return basePower;
    }

    public float GetPowerToWeightRatio()
    {
        return GetTotalPower() / body.mass;
    }

    public PerformanceProfile GetPerformanceProfile(string profileName)
    {
        foreach (var profile in performanceProfiles)
        {
            if (profile.profileName.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }
        return performanceProfiles[0];
    }

    public bool IsElectric()
    {
        return fuelSystem.fuelType == FuelSystemSettings.FuelType.Electric ||
               fuelSystem.fuelType == FuelSystemSettings.FuelType.Hybrid;
    }

    public void ResetToDefaults()
    {
        fuelSystem.currentFuel = fuelSystem.fuelCapacity;
        ClearDynamicProperties();
    }

    // ============ CLASSIFICATION HELPER METHODS ============
    /// <summary>
    /// Get the current category as a specific enum type
    /// </summary>
    public T GetCategoryEnum<T>() where T : Enum
    {
        return vehicleType switch
        {
            VehicleType.Land when typeof(T) == typeof(LandVehicleCategory) => (T)(object)landCategory,
            VehicleType.Air when typeof(T) == typeof(AirVehicleCategory) => (T)(object)airCategory,
            VehicleType.Water when typeof(T) == typeof(WaterVehicleCategory) => (T)(object)waterCategory,
            VehicleType.Space when typeof(T) == typeof(SpaceVehicleCategory) => (T)(object)spaceCategory,
            VehicleType.Rail when typeof(T) == typeof(RailVehicleCategory) => (T)(object)railCategory,
            _ => default
        };
    }

    /// <summary>
    /// Set category from any enum type
    /// </summary>
    public void SetCategoryEnum<T>(T categoryValue) where T : Enum
    {
        if (typeof(T) == typeof(LandVehicleCategory))
            landCategory = (LandVehicleCategory)(object)categoryValue;
        else if (typeof(T) == typeof(AirVehicleCategory))
            airCategory = (AirVehicleCategory)(object)categoryValue;
        else if (typeof(T) == typeof(WaterVehicleCategory))
            waterCategory = (WaterVehicleCategory)(object)categoryValue;
        else if (typeof(T) == typeof(SpaceVehicleCategory))
            spaceCategory = (SpaceVehicleCategory)(object)categoryValue;
        else if (typeof(T) == typeof(RailVehicleCategory))
            railCategory = (RailVehicleCategory)(object)categoryValue;
    }

    /// <summary>
    /// Get specialized type as enum
    /// </summary>
    public T GetSpecializedEnum<T>() where T : Enum
    {
        return vehicleType switch
        {
            VehicleType.Land when typeof(T) == typeof(SpecializedLandVehicleType) => (T)(object)specializedLand,
            VehicleType.Air when typeof(T) == typeof(SpecializedAirVehicleType) => (T)(object)specializedAir,
            _ => default
        };
    }

    /// <summary>
    /// Set specialized type from enum
    /// </summary>
    public void SetSpecializedEnum<T>(T specializedValue) where T : Enum
    {
        if (typeof(T) == typeof(SpecializedLandVehicleType))
            specializedLand = (SpecializedLandVehicleType)(object)specializedValue;
        else if (typeof(T) == typeof(SpecializedAirVehicleType))
            specializedAir = (SpecializedAirVehicleType)(object)specializedValue;
    }

    /// <summary>
    /// Check if this vehicle matches a specific type/category combination
    /// </summary>
    public bool MatchesClassification(VehicleType type, string category = null, string specialized = null)
    {
        if (vehicleType != type) return false;
        if (category != null && GetCurrentCategory() != category) return false;
        if (specialized != null && GetCurrentSpecialized() != specialized) return false;
        return true;
    }

    public void EnsureClassificationDefaults()
    {
        classification ??= new VehicleClassificationData();

        if (string.IsNullOrWhiteSpace(classification.typeId))
            classification.typeId = GetLegacyTypeId();

        if (string.IsNullOrWhiteSpace(classification.categoryId))
            classification.categoryId = GetLegacyCategoryId();

        if (classification.typeId == "land" && landCategory == LandVehicleCategory.Specialized)
            classification.subcategoryId = specializedLand.ToString().ToLowerInvariant();
        else if (classification.typeId == "air" && airCategory == AirVehicleCategory.Specialized)
            classification.subcategoryId = specializedAir.ToString().ToLowerInvariant();
        else if (classification.subcategoryId == null)
            classification.subcategoryId = string.Empty;
    }

    public void SyncLegacyClassificationFromIds()
    {
        EnsureClassificationDefaults();

        string type = classification.typeId?.Trim().ToLowerInvariant();
        switch (type)
        {
            case "land":
                vehicleType = VehicleType.Land;
                break;
            case "air":
                vehicleType = VehicleType.Air;
                break;
            case "water":
                vehicleType = VehicleType.Water;
                break;
            case "space":
                vehicleType = VehicleType.Space;
                break;
            case "rail":
                vehicleType = VehicleType.Rail;
                break;
            case "fictional":
                vehicleType = VehicleType.Fictional;
                break;
        }

        string category = classification.categoryId?.Trim();
        if (!string.IsNullOrEmpty(category))
        {
            if (vehicleType == VehicleType.Land &&
                Enum.TryParse<LandVehicleCategory>(category, true, out var land))
            {
                landCategory = land;
            }
            else if (vehicleType == VehicleType.Air &&
                Enum.TryParse<AirVehicleCategory>(category, true, out var air))
            {
                airCategory = air;
            }
            else if (vehicleType == VehicleType.Water &&
                Enum.TryParse<WaterVehicleCategory>(category, true, out var waterCat))
            {
                waterCategory = waterCat;
            }
            else if (vehicleType == VehicleType.Space &&
                Enum.TryParse<SpaceVehicleCategory>(category, true, out var space))
            {
                spaceCategory = space;
            }
            else if (vehicleType == VehicleType.Rail &&
                Enum.TryParse<RailVehicleCategory>(category, true, out var rail))
            {
                railCategory = rail;
            }
        }

        string subcategory = classification.subcategoryId?.Trim();
        if (!string.IsNullOrEmpty(subcategory))
        {
            if (vehicleType == VehicleType.Land &&
                Enum.TryParse<SpecializedLandVehicleType>(subcategory, true, out var landSpec))
            {
                specializedLand = landSpec;
                landCategory = LandVehicleCategory.Specialized;
            }
            else if (vehicleType == VehicleType.Air &&
                Enum.TryParse<SpecializedAirVehicleType>(subcategory, true, out var airSpec))
            {
                specializedAir = airSpec;
                airCategory = AirVehicleCategory.Specialized;
            }
        }
    }

    public string GetLegacyTypeId()
    {
        return vehicleType switch
        {
            VehicleType.Land => "land",
            VehicleType.Air => "air",
            VehicleType.Water => "water",
            VehicleType.Space => "space",
            VehicleType.Rail => "rail",
            VehicleType.Fictional => "fictional",
            _ => "land"
        };
    }

    public string GetLegacyCategoryId()
    {
        return vehicleType switch
        {
            VehicleType.Land => landCategory.ToString().ToLowerInvariant(),
            VehicleType.Air => airCategory.ToString().ToLowerInvariant(),
            VehicleType.Water => waterCategory.ToString().ToLowerInvariant(),
            VehicleType.Space => spaceCategory.ToString().ToLowerInvariant(),
            VehicleType.Rail => railCategory.ToString().ToLowerInvariant(),
            VehicleType.Fictional => "standard",
            _ => "standard"
        };
    }

    public static string ComputeDeterministicIdFromPrefabGuid(string sourcePrefabGuid)
    {
        if (string.IsNullOrWhiteSpace(sourcePrefabGuid))
            return string.Empty;

        string compact = sourcePrefabGuid.Trim().Replace("-", string.Empty).ToUpperInvariant();
        return $"UVS-{compact}";
    }
    // ======================================================
}
