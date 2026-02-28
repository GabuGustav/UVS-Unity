using System;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    [Serializable]
    public class VehicleGuidIndexFile
    {
        public int version = 1;
        public List<VehicleGuidIndexEntry> entries = new();
    }

    [Serializable]
    public class VehicleGuidIndexEntry
    {
        public string prefabGuid;
        public string vehicleId;
        public string configAssetPath;
        public string lastSyncedUtc;
    }

    [Serializable]
    public class VehicleIdMigrationMapFile
    {
        public string generatedUtc;
        public List<VehicleIdMigrationMapEntry> mappings = new();
    }

    [Serializable]
    public class VehicleIdMigrationMapEntry
    {
        public string prefabGuid;
        public string oldId;
        public string newId;
        public string configAssetPath;
    }

    public sealed class VehicleIdRepairReport
    {
        public int scannedConfigs;
        public int updatedIds;
        public int recoveredGuidsFromJson;
        public int missingGuidConfigs;
        public int duplicateGroups;
        public int quarantinedDuplicates;
        public int jsonEntriesWritten;
    }
}
