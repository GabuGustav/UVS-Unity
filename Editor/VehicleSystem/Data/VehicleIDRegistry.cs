using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UVS.Editor.Core;

[CreateAssetMenu(fileName = "VehicleIDRegistry", menuName = "Vehicles/Vehicle ID Registry", order = 0)]
public class VehicleIDRegistry : ScriptableObject
{
    [Header("Registry Storage")]
    [SerializeField] private List<string> registeredIDs = new();

    [Header("Current Vehicle Info")]
    public string vehicleID;
    public string vehicleName;
    public string vehicleType;
    public string manufacturer;
    public int seatingCapacity;
    public VehicleConfig config;

    private readonly Dictionary<string, VehicleConfig> idToConfigMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VehicleConfig> prefabGuidToConfigMap = new(StringComparer.OrdinalIgnoreCase);

    private const string ConfigRoot = "Assets/VehicleConfigs";
    private const string QuarantineRoot = "Assets/VehicleConfigs/_DuplicateGuidQuarantine";

    [Serializable]
    private sealed class ConfigRecord
    {
        public VehicleConfig config;
        public string path;
        public string prefabGuid;
        public DateTime createdUtc;
    }

    public void RegisterID(string ignoredVehicleID, GameObject vehiclePrefab)
    {
        if (vehiclePrefab == null)
        {
            Debug.LogWarning("[UVS] RegisterID called with null prefab.");
            return;
        }

        var cfg = GetOrCreateConfigForPrefab(vehiclePrefab);
        if (cfg == null) return;

        EnsureDeterministicId(cfg);
        UpsertConfig(cfg);
        EditorUtility.SetDirty(cfg);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UVS] Deterministic ID ensured for '{vehiclePrefab.name}': {cfg.id}");
    }

    public void RegisterVehicle(string ignoredVehicleID, VehicleConfig vehicleConfig)
    {
        if (vehicleConfig == null)
        {
            Debug.LogWarning("[UVS] RegisterVehicle called with null config.");
            return;
        }

        EnsureDeterministicId(vehicleConfig);
        UpsertConfig(vehicleConfig);

        EditorUtility.SetDirty(vehicleConfig);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public bool ContainsID(string id)
    {
        if (idToConfigMap.Count == 0)
            InitializeRegistry();

        return !string.IsNullOrWhiteSpace(id) && idToConfigMap.ContainsKey(id);
    }

    public bool TryGetByPrefabGuid(string guid, out VehicleConfig vehicleConfig)
    {
        if (prefabGuidToConfigMap.Count == 0)
            InitializeRegistry();

        return prefabGuidToConfigMap.TryGetValue(guid ?? string.Empty, out vehicleConfig);
    }

    public VehicleConfig GetConfigForID(string id)
    {
        if (ContainsID(id))
            return idToConfigMap[id];
        return null;
    }

    public VehicleConfig GetConfigForPrefabGuid(string prefabGuid)
    {
        TryGetByPrefabGuid(prefabGuid, out var cfg);
        return cfg;
    }

    public VehicleConfig GetConfigForPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        string path = AssetDatabase.GetAssetPath(prefab);
        string guid = AssetDatabase.AssetPathToGUID(path);
        return GetConfigForPrefabGuid(guid);
    }

    public bool PrefabHasConfig(GameObject prefab)
    {
        return GetConfigForPrefab(prefab) != null;
    }

    public string GetIDForPrefab(GameObject prefab)
    {
        var cfg = GetConfigForPrefab(prefab);
        return cfg != null ? cfg.id : null;
    }

    public List<string> GetAllRegisteredIDs()
    {
        if (idToConfigMap.Count == 0)
            InitializeRegistry();
        return new List<string>(registeredIDs);
    }

    public string EnsureDeterministicId(VehicleConfig vehicleConfig)
    {
        if (vehicleConfig == null || string.IsNullOrWhiteSpace(vehicleConfig.prefabGuid))
            return vehicleConfig != null ? vehicleConfig.id : string.Empty;

        string deterministic = VehicleConfig.ComputeDeterministicIdFromPrefabGuid(vehicleConfig.prefabGuid);
        if (string.IsNullOrEmpty(deterministic))
            return vehicleConfig.id;

        if (!string.Equals(vehicleConfig.id, deterministic, StringComparison.OrdinalIgnoreCase))
        {
            string old = vehicleConfig.id;
            if (!string.IsNullOrWhiteSpace(old) &&
                !string.Equals(old, deterministic, StringComparison.OrdinalIgnoreCase) &&
                !vehicleConfig.legacyIds.Contains(old, StringComparer.OrdinalIgnoreCase))
            {
                vehicleConfig.legacyIds.Add(old);
            }

            vehicleConfig.id = deterministic;
            EditorUtility.SetDirty(vehicleConfig);
        }

        return vehicleConfig.id;
    }

    public VehicleConfig GetOrCreateConfigForPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
        if (string.IsNullOrWhiteSpace(prefabGuid)) return null;

        if (TryGetByPrefabGuid(prefabGuid, out var existing) && existing != null)
            return existing;

        string[] configGuids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { ConfigRoot });
        foreach (var cfgGuid in configGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(cfgGuid);
            var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
            if (cfg == null) continue;

            if (string.Equals(cfg.prefabGuid, prefabGuid, StringComparison.OrdinalIgnoreCase))
            {
                cfg.prefabReference = prefab;
                EnsureDeterministicId(cfg);
                UpsertConfig(cfg);
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
                return cfg;
            }
        }

        if (!AssetDatabase.IsValidFolder(ConfigRoot))
            AssetDatabase.CreateFolder("Assets", "VehicleConfigs");

        var created = ScriptableObject.CreateInstance<VehicleConfig>();
        created.prefabReference = prefab;
        created.prefabGuid = prefabGuid;
        created.vehicleName = prefab.name;
        created.EnsureClassificationDefaults();
        EnsureDeterministicId(created);

        string safePrefix = SanitizeFileName(prefab.name);
        string cfgPath = $"{ConfigRoot}/{safePrefix}_{prefabGuid[..8]}Config.asset";
        cfgPath = AssetDatabase.GenerateUniqueAssetPath(cfgPath);

        AssetDatabase.CreateAsset(created, cfgPath);
        EditorUtility.SetDirty(created);
        UpsertConfig(created);
        AssetDatabase.SaveAssets();

        return created;
    }

    public VehicleIdRepairReport RebuildGuidIndexAndRepair(bool rekeyAll, bool exportMigrationMap, bool quarantineDuplicates)
    {
        var report = new VehicleIdRepairReport();
        var migrationMap = new VehicleIdMigrationMapFile
        {
            generatedUtc = DateTime.UtcNow.ToString("o")
        };

        idToConfigMap.Clear();
        prefabGuidToConfigMap.Clear();
        registeredIDs.Clear();

        VehicleIdIndexService.EnsureDataFolder();
        var jsonIndex = VehicleIdIndexService.LoadGuidIndex();
        var jsonByPath = (jsonIndex.entries ?? new List<VehicleGuidIndexEntry>())
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.configAssetPath))
            .GroupBy(e => e.configAssetPath)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        string[] configGuids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { ConfigRoot });
        var scanned = new List<ConfigRecord>();

        foreach (var cfgGuid in configGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(cfgGuid);
            var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
            if (cfg == null) continue;

            report.scannedConfigs++;

            if (string.IsNullOrWhiteSpace(cfg.prefabGuid) && jsonByPath.TryGetValue(path, out var jsonEntry))
            {
                if (!string.IsNullOrWhiteSpace(jsonEntry.prefabGuid))
                {
                    cfg.prefabGuid = jsonEntry.prefabGuid;
                    report.recoveredGuidsFromJson++;
                    EditorUtility.SetDirty(cfg);
                }
            }

            if (string.IsNullOrWhiteSpace(cfg.prefabGuid))
            {
                report.missingGuidConfigs++;
                continue;
            }

            string oldId = cfg.id;
            string newId = EnsureDeterministicId(cfg);
            if (!string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase))
            {
                report.updatedIds++;
                migrationMap.mappings.Add(new VehicleIdMigrationMapEntry
                {
                    prefabGuid = cfg.prefabGuid,
                    oldId = oldId,
                    newId = newId,
                    configAssetPath = path
                });
            }

            DateTime created = DateTime.MaxValue;
            try
            {
                created = File.GetCreationTimeUtc(Path.GetFullPath(path));
            }
            catch { }

            scanned.Add(new ConfigRecord
            {
                config = cfg,
                path = path,
                prefabGuid = cfg.prefabGuid,
                createdUtc = created
            });
        }

        var finalRecords = new List<ConfigRecord>();
        foreach (var group in scanned.GroupBy(r => r.prefabGuid, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group
                .OrderBy(r => r.createdUtc)
                .ThenBy(r => r.path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var keeper = ordered[0];
            finalRecords.Add(keeper);

            if (ordered.Count <= 1) continue;

            report.duplicateGroups++;

            for (int i = 1; i < ordered.Count; i++)
            {
                var dup = ordered[i];
                if (!quarantineDuplicates)
                    continue;

                if (TryQuarantineConfig(dup.path, out var movedPath))
                {
                    report.quarantinedDuplicates++;
                    Debug.LogWarning($"[UVS] Quarantined duplicate GUID config: '{dup.path}' -> '{movedPath}'");
                }
            }
        }

        foreach (var record in finalRecords)
        {
            var cfg = record.config;
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.prefabGuid) || string.IsNullOrWhiteSpace(cfg.id))
                continue;

            prefabGuidToConfigMap[cfg.prefabGuid] = cfg;
            idToConfigMap[cfg.id] = cfg;
        }

        registeredIDs = idToConfigMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        var outIndex = new VehicleGuidIndexFile();
        outIndex.entries = finalRecords
            .Where(r => r.config != null && !string.IsNullOrWhiteSpace(r.config.id) && !string.IsNullOrWhiteSpace(r.prefabGuid))
            .OrderBy(r => r.prefabGuid, StringComparer.OrdinalIgnoreCase)
            .Select(r => new VehicleGuidIndexEntry
            {
                prefabGuid = r.prefabGuid,
                vehicleId = r.config.id,
                configAssetPath = r.path,
                lastSyncedUtc = DateTime.UtcNow.ToString("o")
            })
            .ToList();

        report.jsonEntriesWritten = outIndex.entries.Count;
        VehicleIdIndexService.SaveGuidIndex(outIndex);

        if (exportMigrationMap || rekeyAll)
            VehicleIdIndexService.SaveMigrationMap(migrationMap);

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        return report;
    }

    public void ExportMigrationMapFromLegacyIds()
    {
        var file = new VehicleIdMigrationMapFile
        {
            generatedUtc = DateTime.UtcNow.ToString("o")
        };

        string[] configGuids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { ConfigRoot });
        foreach (var cfgGuid in configGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(cfgGuid);
            var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.id) || cfg.legacyIds == null || cfg.legacyIds.Count == 0)
                continue;

            foreach (string legacy in cfg.legacyIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(legacy, cfg.id, StringComparison.OrdinalIgnoreCase))
                    continue;

                file.mappings.Add(new VehicleIdMigrationMapEntry
                {
                    prefabGuid = cfg.prefabGuid,
                    oldId = legacy,
                    newId = cfg.id,
                    configAssetPath = path
                });
            }
        }

        VehicleIdIndexService.SaveMigrationMap(file);
    }

    public void InitializeRegistry()
    {
        RebuildGuidIndexAndRepair(rekeyAll: false, exportMigrationMap: false, quarantineDuplicates: false);
    }

    public void ClearRegistry()
    {
        idToConfigMap.Clear();
        prefabGuidToConfigMap.Clear();
        registeredIDs.Clear();
        EditorUtility.SetDirty(this);
    }

    public void UpsertConfig(VehicleConfig vehicleConfig)
    {
        if (vehicleConfig == null) return;
        if (string.IsNullOrWhiteSpace(vehicleConfig.prefabGuid)) return;

        EnsureDeterministicId(vehicleConfig);

        if (!string.IsNullOrWhiteSpace(vehicleConfig.id))
            idToConfigMap[vehicleConfig.id] = vehicleConfig;

        prefabGuidToConfigMap[vehicleConfig.prefabGuid] = vehicleConfig;

        registeredIDs = idToConfigMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Vehicle";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static bool TryQuarantineConfig(string sourcePath, out string targetPath)
    {
        targetPath = null;

        if (string.IsNullOrWhiteSpace(sourcePath))
            return false;

        if (!AssetDatabase.IsValidFolder(ConfigRoot))
            return false;

        if (!AssetDatabase.IsValidFolder(QuarantineRoot))
        {
            if (!AssetDatabase.IsValidFolder("Assets/VehicleConfigs"))
                AssetDatabase.CreateFolder("Assets", "VehicleConfigs");
            AssetDatabase.CreateFolder("Assets/VehicleConfigs", "_DuplicateGuidQuarantine");
        }

        string fileName = Path.GetFileName(sourcePath);
        string destination = AssetDatabase.GenerateUniqueAssetPath($"{QuarantineRoot}/{fileName}");

        string err = AssetDatabase.MoveAsset(sourcePath, destination);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogWarning($"[UVS] Failed to quarantine duplicate config '{sourcePath}': {err}");
            return false;
        }

        targetPath = destination;
        return true;
    }

    private void OnEnable()
    {
        InitializeRegistry();
    }

    private void OnValidate()
    {
        if (idToConfigMap.Count == 0 && AssetDatabase.FindAssets("t:VehicleConfig", new[] { ConfigRoot }).Length > 0)
            InitializeRegistry();
    }
}
