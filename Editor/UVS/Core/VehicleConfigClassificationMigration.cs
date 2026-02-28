using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Core
{
    public static class VehicleConfigClassificationMigration
    {
        [MenuItem("Tools/Vehicle Editor/Migrate/Sync Classification IDs")]
        public static void SyncClassificationIds()
        {
            VehicleTaxonomyService.GetOrCreateDefault(createIfMissing: true);
            VehicleTaxonomyService.EnsureCompanionProfiles();

            var guids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { "Assets/VehicleConfigs" });
            int updated = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                if (config == null) continue;

                string prevType = config.classification?.typeId;
                string prevCat = config.classification?.categoryId;
                string prevSub = config.classification?.subcategoryId;

                config.EnsureClassificationDefaults();

                if (prevType != config.classification.typeId ||
                    prevCat != config.classification.categoryId ||
                    prevSub != config.classification.subcategoryId)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UVS] Classification migration complete. Updated {updated} VehicleConfig assets.");
        }
    }
}
