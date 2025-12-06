using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(fileName = "VehicleIDRegistry", menuName = "Vehicles/Vehicle ID Registry", order = 0)]
public class VehicleIDRegistry : ScriptableObject
{
    [Header("Registry Storage")]
    [SerializeField] private List<string> registeredIDs = new List<string>();
    
    [Header("Current Vehicle Info")]
    public string vehicleID;
    public string vehicleType;
    public int seatingCapacity;
    public VehicleConfig config;

    // Dual mapping: ID -> Config AND PrefabGUID -> Config
    private Dictionary<string, VehicleConfig> idToConfigMap = new Dictionary<string, VehicleConfig>();
    private Dictionary<string, VehicleConfig> prefabGuidToConfigMap = new Dictionary<string, VehicleConfig>();

    // In VehicleIDRegistry.cs, replace the RegisterID method with this:

    public void RegisterID(string vehicleID, GameObject vehiclePrefab)
    {
        if (string.IsNullOrEmpty(vehicleID))
        {
            Debug.LogWarning("VehicleID is empty, cannot register.");
            return;
        }

        // CRITICAL: Initialize registry if maps are empty
        if (idToConfigMap.Count == 0 && prefabGuidToConfigMap.Count == 0)
        {
            InitializeRegistry();
        }

        string prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(vehiclePrefab));

        // Check if this prefab already has a config
        if (prefabGuidToConfigMap.TryGetValue(prefabGuid, out VehicleConfig existingConfig))
        {
            Debug.LogWarning($"Vehicle prefab '{vehiclePrefab.name}' is already registered with ID: {existingConfig.id}");
            return;
        }

        // Check if this ID is already used
        if (idToConfigMap.ContainsKey(vehicleID))
        {
            Debug.LogWarning($"VehicleID '{vehicleID}' is already in use!");
            return;
        }

        // Find or create the config for this vehicle using prefab GUID
        VehicleConfig vehicleConfig = FindOrCreateConfigForPrefab(vehiclePrefab, prefabGuid);

        // Set the ID and update both mappings
        vehicleConfig.id = vehicleID;
        idToConfigMap.Add(vehicleID, vehicleConfig);
        prefabGuidToConfigMap[prefabGuid] = vehicleConfig;

        if (!registeredIDs.Contains(vehicleID))
        {
            registeredIDs.Add(vehicleID);
        }

        Debug.Log($"Registered VehicleID: {vehicleID} for {vehiclePrefab.name}");

        // Mark both config and registry as dirty to save changes
        EditorUtility.SetDirty(vehicleConfig);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public bool ContainsID(string id)
    {
        return idToConfigMap.ContainsKey(id);
    }

    public VehicleConfig GetConfigForID(string id)
    {
        if (idToConfigMap.TryGetValue(id, out VehicleConfig config))
        {
            return config;
        }
        return null;
    }

    public VehicleConfig GetConfigForPrefabGuid(string prefabGuid)
    {
        if (prefabGuidToConfigMap.TryGetValue(prefabGuid, out VehicleConfig config))
        {
            return config;
        }
        return null;
    }

    public VehicleConfig GetConfigForPrefab(GameObject prefab)
    {
        string prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        return GetConfigForPrefabGuid(prefabGuid);
    }

    public bool PrefabHasConfig(GameObject prefab)
    {
        return GetConfigForPrefab(prefab) != null;
    }

    public string GetIDForPrefab(GameObject prefab)
    {
        VehicleConfig config = GetConfigForPrefab(prefab);
        return config?.id;
    }

    public List<string> GetAllRegisteredIDs()
    {
        return new List<string>(registeredIDs);
    }

    public void ClearRegistry()
    {
        idToConfigMap.Clear();
        prefabGuidToConfigMap.Clear();
        registeredIDs.Clear();
        EditorUtility.SetDirty(this);
    }

    // Initialize both mappings from existing VehicleConfig assets
    public void InitializeRegistry()
    {
        idToConfigMap.Clear();
        prefabGuidToConfigMap.Clear();
        registeredIDs.Clear();

        // Rebuild both dictionaries by finding all VehicleConfig assets
        string[] configGUIDs = AssetDatabase.FindAssets("t:VehicleConfig");
        foreach (string guid in configGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VehicleConfig config = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
            
            if (config != null)
            {
                // Register by ID
                if (!string.IsNullOrEmpty(config.id))
                {
                    idToConfigMap[config.id] = config;
                    if (!registeredIDs.Contains(config.id))
                    {
                        registeredIDs.Add(config.id);
                    }
                }

                // Register by prefab GUID (this is the key part!)
                if (!string.IsNullOrEmpty(config.prefabGuid))
                {
                    prefabGuidToConfigMap[config.prefabGuid] = config;
                }
            }
        }
    }

    private VehicleConfig FindOrCreateConfigForPrefab(GameObject prefab, string prefabGuid)
    {
        // First, try to find existing config by prefab GUID
        if (prefabGuidToConfigMap.TryGetValue(prefabGuid, out VehicleConfig existingConfig))
        {
            return existingConfig;
        }

        // If not found in memory, search all VehicleConfig assets
        string[] configGUIDs = AssetDatabase.FindAssets("t:VehicleConfig");
        foreach (string guid in configGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VehicleConfig config = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
            
            if (config != null && config.prefabGuid == prefabGuid)
            {
                // Found it! Add to our mapping and return
                prefabGuidToConfigMap[prefabGuid] = config;
                if (!string.IsNullOrEmpty(config.id))
                {
                    idToConfigMap[config.id] = config;
                }
                return config;
            }
        }
        
        // Create new config if not found
        var newConfig = ScriptableObject.CreateInstance<VehicleConfig>();
        newConfig.prefabReference = prefab;
        newConfig.prefabGuid = prefabGuid;
        
        const string folder = "Assets/VehicleConfigs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "VehicleConfigs");
        
        string cfgPath = $"{folder}/{prefab.name}_{prefabGuid.Substring(0, 8)}Config.asset";
        AssetDatabase.CreateAsset(newConfig, cfgPath);
        AssetDatabase.SaveAssets();
        
        // Add to our mapping
        prefabGuidToConfigMap[prefabGuid] = newConfig;
        
        return newConfig;
    }

    // Called when the ScriptableObject is loaded or enabled
    private void OnEnable()
    {
        InitializeRegistry();
    }

    // Called when the asset is loaded in the editor
    private void OnValidate()
    {
        // Ensure registry is initialized when the asset is inspected
        if (idToConfigMap.Count == 0 && AssetDatabase.FindAssets("t:VehicleConfig").Length > 0)
        {
            InitializeRegistry();
        }
    }

    public void RegisterVehicle(string vehicleID, VehicleConfig config)
    {
        if (string.IsNullOrEmpty(vehicleID))
        {
            Debug.LogWarning("VehicleID is empty, cannot register.");
            return;
        }

        if (idToConfigMap.ContainsKey(vehicleID))
        {
            Debug.LogWarning($"VehicleID '{vehicleID}' already exists in registry.");
            return;
        }

        // Set the ID in the config
        config.id = vehicleID;

        // Add to mappings
        idToConfigMap.Add(vehicleID, config);

        // Also add by prefab GUID if available
        if (!string.IsNullOrEmpty(config.prefabGuid))
        {
            prefabGuidToConfigMap[config.prefabGuid] = config;
        }

        if (!registeredIDs.Contains(vehicleID))
        {
            registeredIDs.Add(vehicleID);
        }

        Debug.Log($"Registered Vehicle: {vehicleID}");

        // Mark as dirty to save changes
        EditorUtility.SetDirty(config);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}