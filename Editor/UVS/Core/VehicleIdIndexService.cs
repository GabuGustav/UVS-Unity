using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Core
{
    public static class VehicleIdIndexService
    {
        public const string DataFolder = "Assets/Editor/UVSVehicleSystem/Data";
        public const string GuidIndexPath = DataFolder + "/VehicleGuidIndex.json";
        public const string MigrationMapPath = DataFolder + "/VehicleIdMigrationMap.json";

        public static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");

            if (!AssetDatabase.IsValidFolder("Assets/Editor/UVSVehicleSystem"))
                AssetDatabase.CreateFolder("Assets/Editor", "UVSVehicleSystem");

            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/Editor/UVSVehicleSystem", "Data");
        }

        public static VehicleGuidIndexFile LoadGuidIndex()
        {
            try
            {
                string diskPath = ToDiskPath(GuidIndexPath);
                if (!File.Exists(diskPath))
                    return new VehicleGuidIndexFile();

                string json = File.ReadAllText(diskPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new VehicleGuidIndexFile();

                var parsed = JsonUtility.FromJson<VehicleGuidIndexFile>(json);
                return parsed ?? new VehicleGuidIndexFile();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UVS] Failed to load GUID index JSON: {ex.Message}");
                return new VehicleGuidIndexFile();
            }
        }

        public static void SaveGuidIndex(VehicleGuidIndexFile index)
        {
            EnsureDataFolder();
            if (index == null)
                index = new VehicleGuidIndexFile();

            string json = JsonUtility.ToJson(index, true);
            AtomicWriteText(GuidIndexPath, json);
            AssetDatabase.ImportAsset(GuidIndexPath, ImportAssetOptions.ForceUpdate);
        }

        public static void SaveMigrationMap(VehicleIdMigrationMapFile map)
        {
            EnsureDataFolder();
            if (map == null)
                map = new VehicleIdMigrationMapFile();

            string json = JsonUtility.ToJson(map, true);
            AtomicWriteText(MigrationMapPath, json);
            AssetDatabase.ImportAsset(MigrationMapPath, ImportAssetOptions.ForceUpdate);
        }

        private static void AtomicWriteText(string path, string content)
        {
            string diskPath = ToDiskPath(path);
            string tempPath = diskPath + ".tmp";
            string dir = Path.GetDirectoryName(diskPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(tempPath, content);

            try
            {
                if (File.Exists(diskPath))
                {
                    File.Replace(tempPath, diskPath, null);
                }
                else
                {
                    File.Move(tempPath, diskPath);
                }
            }
            catch
            {
                if (File.Exists(diskPath))
                    File.Delete(diskPath);
                File.Move(tempPath, diskPath);
            }
        }

        private static string ToDiskPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string relative = assetPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, relative);
        }
    }
}
