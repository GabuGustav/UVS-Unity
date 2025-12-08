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
    public string authorname;

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

    public enum VehicleType { Land, Air, Water, Space, Fictional }

    public enum SpecializedLandVehicleType
    {
        Construction,Tank
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
        Standard,Classic, Specialized
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
    public DamageSettings damage = new ();

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
}