using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "UVS/Vehicle Config")]
public class VehicleConfig : ScriptableObject
{
    public string prefabGuid;
    public GameObject prefabReference;
    public string id;
    public string vehicleName;
    public string authorName;

    // ============ VEHICLE CLASSIFICATION ============
    [Header("Vehicle Classification")]
    public VehicleType vehicleType = VehicleType.Land;

    // Category enums - only one used based on vehicleType
    public LandVehicleCategory landCategory = LandVehicleCategory.Standard;
    public AirVehicleCategory airCategory = AirVehicleCategory.Standard;
    public WaterVehicleCategory waterCategory = WaterVehicleCategory.Standard;
    public SpaceVehicleCategory spaceCategory = SpaceVehicleCategory.Standard;

    // Specialized enums - only used if category is "Specialized"
    public SpecializedLandVehicleType specializedLand = SpecializedLandVehicleType.Construction;
    public SpecializedAirVehicleType specializedAir = SpecializedAirVehicleType.VTOL;

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

    // Dynamic properties storage for specialized modules (serialized + cached)
    [SerializeField] private List<DynamicProperty> _dynamicPropertyEntries = new();
    private readonly Dictionary<string, DynamicProperty> _dynamicPropertyMap = new();
    private readonly Dictionary<string, object> _dynamicProperties = new();

#if UNITY_EDITOR
    public static event Action<VehicleConfig> OnConfigValidated;
#endif

    private void OnEnable()
    {
        RebuildDynamicPropertyMap();
    }

    private void OnValidate()
    {
        RebuildDynamicPropertyMap();
#if UNITY_EDITOR
        OnConfigValidated?.Invoke(this);
#endif
    }

    [Serializable]
    public class DynamicProperty
    {
        public string key;
        public DynamicPropertyType type;

        public float floatValue;
        public int intValue;
        public bool boolValue;
        public string stringValue;
        public Vector3 vector3Value;
        public Color colorValue;

        public string enumType;
        public string enumValue;
    }

    public enum DynamicPropertyType
    {
        Float,
        Int,
        Bool,
        String,
        Enum,
        Vector3,
        Color
    }

    private void RebuildDynamicPropertyMap()
    {
        _dynamicPropertyMap.Clear();
        if (_dynamicPropertyEntries == null)
            _dynamicPropertyEntries = new List<DynamicProperty>();

        foreach (var entry in _dynamicPropertyEntries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            _dynamicPropertyMap[entry.key] = entry;

            switch (entry.type)
            {
                case DynamicPropertyType.Float: _dynamicProperties[entry.key] = entry.floatValue; break;
                case DynamicPropertyType.Int: _dynamicProperties[entry.key] = entry.intValue; break;
                case DynamicPropertyType.Bool: _dynamicProperties[entry.key] = entry.boolValue; break;
                case DynamicPropertyType.String: _dynamicProperties[entry.key] = entry.stringValue; break;
                case DynamicPropertyType.Vector3: _dynamicProperties[entry.key] = entry.vector3Value; break;
                case DynamicPropertyType.Color: _dynamicProperties[entry.key] = entry.colorValue; break;
                case DynamicPropertyType.Enum: _dynamicProperties[entry.key] = entry.enumValue; break;
            }
        }
    }

    private DynamicProperty GetOrCreate(string key, DynamicPropertyType type)
    {
        if (_dynamicPropertyMap.TryGetValue(key, out var entry))
        {
            entry.type = type;
            return entry;
        }

        entry = new DynamicProperty { key = key, type = type };
        _dynamicPropertyEntries.Add(entry);
        _dynamicPropertyMap[key] = entry;
        return entry;
    }

    private bool TryGetEntry(string key, DynamicPropertyType type, out DynamicProperty entry)
    {
        if (_dynamicPropertyMap.TryGetValue(key, out entry))
        {
            // Allow read even if type mismatches (will just ignore if incompatible)
            return true;
        }
        return false;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    #region Helper Methods for Modules

    // Float helpers
    public void SetFloat(string key, float value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Float);
        entry.floatValue = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is float floatValue)
            return floatValue;
        if (TryGetEntry(key, DynamicPropertyType.Float, out var entry))
        {
            _dynamicProperties[key] = entry.floatValue;
            return entry.floatValue;
        }
        return defaultValue;
    }

    // Int helpers
    public void SetInt(string key, int value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Int);
        entry.intValue = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is int intValue)
            return intValue;
        if (TryGetEntry(key, DynamicPropertyType.Int, out var entry))
        {
            _dynamicProperties[key] = entry.intValue;
            return entry.intValue;
        }
        return defaultValue;
    }

    // Bool helpers
    public void SetBool(string key, bool value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Bool);
        entry.boolValue = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is bool boolValue)
            return boolValue;
        if (TryGetEntry(key, DynamicPropertyType.Bool, out var entry))
        {
            _dynamicProperties[key] = entry.boolValue;
            return entry.boolValue;
        }
        return defaultValue;
    }

    // String helpers
    public void SetString(string key, string value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.String);
        entry.stringValue = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is string stringValue)
            return stringValue;
        if (TryGetEntry(key, DynamicPropertyType.String, out var entry))
        {
            _dynamicProperties[key] = entry.stringValue;
            return entry.stringValue;
        }
        return defaultValue;
    }

    // Enum helpers
    public void SetEnum<T>(string key, T value) where T : Enum
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Enum);
        entry.enumType = typeof(T).AssemblyQualifiedName;
        entry.enumValue = value.ToString();
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public T GetEnum<T>(string key, T defaultValue) where T : Enum
    {
        if (_dynamicProperties.TryGetValue(key, out object value))
        {
            if (value is T enumValue)
                return enumValue;
            if (value is string enumString && Enum.TryParse(typeof(T), enumString, true, out object parsedFromString))
            {
                var parsed = (T)parsedFromString;
                _dynamicProperties[key] = parsed;
                return parsed;
            }
        }
        if (TryGetEntry(key, DynamicPropertyType.Enum, out var entry) &&
            !string.IsNullOrEmpty(entry.enumValue) &&
            Enum.TryParse(typeof(T), entry.enumValue, true, out object parsedFromEntry))
        {
            var parsed = (T)parsedFromEntry;
            _dynamicProperties[key] = parsed;
            return parsed;
        }
        return defaultValue;
    }

    // Vector3 helpers
    public void SetVector3(string key, Vector3 value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Vector3);
        entry.vector3Value = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public Vector3 GetVector3(string key, Vector3 defaultValue)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is Vector3 vectorValue)
            return vectorValue;
        if (TryGetEntry(key, DynamicPropertyType.Vector3, out var entry))
        {
            _dynamicProperties[key] = entry.vector3Value;
            return entry.vector3Value;
        }
        return defaultValue;
    }

    // Color helpers
    public void SetColor(string key, Color value)
    {
        var entry = GetOrCreate(key, DynamicPropertyType.Color);
        entry.colorValue = value;
        _dynamicProperties[key] = value;
        MarkDirty();
    }

    public Color GetColor(string key, Color defaultValue)
    {
        if (_dynamicProperties.TryGetValue(key, out object value) && value is Color colorValue)
            return colorValue;
        if (TryGetEntry(key, DynamicPropertyType.Color, out var entry))
        {
            _dynamicProperties[key] = entry.colorValue;
            return entry.colorValue;
        }
        return defaultValue;
    }

    // Check if property exists
    public bool HasProperty(string key)
    {
        return _dynamicPropertyMap.ContainsKey(key) || _dynamicProperties.ContainsKey(key);
    }

    // Remove property
    public void RemoveProperty(string key)
    {
        _dynamicProperties.Remove(key);
        if (_dynamicPropertyMap.TryGetValue(key, out var entry))
        {
            _dynamicPropertyEntries.Remove(entry);
            _dynamicPropertyMap.Remove(key);
        }
        MarkDirty();
    }

    // Clear all dynamic properties
    public void ClearDynamicProperties()
    {
        _dynamicProperties.Clear();
        _dynamicPropertyEntries.Clear();
        _dynamicPropertyMap.Clear();
        MarkDirty();
    }

    // Get all property keys
    public IEnumerable<string> GetAllPropertyKeys()
    {
        return _dynamicPropertyMap.Keys;
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

    public enum VehicleType { Land, Air, Water, Space, Fictional }

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
    }
    public List<WheelSettings> wheels = new();

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

    // Physics (aircraft and construction-focused)
    [Serializable]
    public class AirplanePhysicsSettings
    {
        public float wingArea = 20f;
        public float liftCoefficient = 1.2f;
        public float dragCoefficient = 0.03f;
        public float maxThrust = 50000f;
        public float stallSpeed = 25f;
        public float maxBankAngle = 60f;
        public float pitchStability = 0.6f;
        public float rollStability = 0.6f;
        public float yawStability = 0.6f;
        public float controlSurfaceEffectiveness = 1.0f;
    }
    public AirplanePhysicsSettings airplanePhysics = new();

    [Serializable]
    public class HelicopterPhysicsSettings
    {
        public int rotorCount = 1;
        public float mainRotorDiameter = 10f;
        public float tailRotorDiameter = 1.6f;
        public float collectivePitchRange = 15f;
        public float cyclicResponse = 1.0f;
        public float torqueCompensation = 1.0f;
        public float hoverEfficiency = 0.8f;
        public float maxClimbRate = 8f;
        public float maxDescentRate = 6f;
        public float maxForwardSpeed = 60f;
        public float maxYawRate = 90f;
    }
    public HelicopterPhysicsSettings helicopterPhysics = new();

    [Serializable]
    public class ConstructionPhysicsSettings
    {
        public float tractionCoefficient = 1.2f;
        public float stabilityAssist = 0.5f;
        public float maxSlopeAngle = 25f;
        public float weightDistributionFront = 0.55f;
        public float hydraulicForceMultiplier = 1.0f;
        public float groundPressureLimit = 300f;
        public float suspensionCompliance = 0.5f;
        public bool enableOutriggerPhysics = true;
    }
    public ConstructionPhysicsSettings constructionPhysics = new();

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
    // ======================================================
}
