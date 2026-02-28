using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Core
{
    public static class VehicleIdMigrationService
    {
        [MenuItem("Tools/Vehicle Editor/IDs/Rebuild & Rekey All")]
        public static void RebuildAndRekeyAll()
        {
            var registry = GetOrCreateRegistry();
            if (registry == null) return;

            var report = registry.RebuildGuidIndexAndRepair(rekeyAll: true, exportMigrationMap: true, quarantineDuplicates: true);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UVS] Rekey complete. Scanned={report.scannedConfigs}, UpdatedIDs={report.updatedIds}, " +
                      $"RecoveredGuids={report.recoveredGuidsFromJson}, Duplicates={report.duplicateGroups}, " +
                      $"Quarantined={report.quarantinedDuplicates}, MissingGuid={report.missingGuidConfigs}");
        }

        [MenuItem("Tools/Vehicle Editor/IDs/Validate & Repair Index")]
        public static void ValidateAndRepairIndex()
        {
            var registry = GetOrCreateRegistry();
            if (registry == null) return;

            var report = registry.RebuildGuidIndexAndRepair(rekeyAll: false, exportMigrationMap: false, quarantineDuplicates: true);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UVS] ID index repaired. Scanned={report.scannedConfigs}, UpdatedIDs={report.updatedIds}, " +
                      $"RecoveredGuids={report.recoveredGuidsFromJson}, Duplicates={report.duplicateGroups}, " +
                      $"Quarantined={report.quarantinedDuplicates}, MissingGuid={report.missingGuidConfigs}");
        }

        [MenuItem("Tools/Vehicle Editor/IDs/Export Migration Map")]
        public static void ExportMigrationMap()
        {
            var registry = GetOrCreateRegistry();
            if (registry == null) return;

            registry.ExportMigrationMapFromLegacyIds();
            AssetDatabase.Refresh();
            Debug.Log($"[UVS] Migration map exported to {VehicleIdIndexService.MigrationMapPath}");
        }

        private static VehicleIDRegistry GetOrCreateRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleIDRegistry");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<VehicleIDRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));

            VehicleIdIndexService.EnsureDataFolder();
            var created = ScriptableObject.CreateInstance<VehicleIDRegistry>();
            AssetDatabase.CreateAsset(created, VehicleIdIndexService.DataFolder + "/VehicleIDRegistry.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return created;
        }
    }
}
