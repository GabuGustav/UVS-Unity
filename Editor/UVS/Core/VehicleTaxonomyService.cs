using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Core
{
    public static class VehicleTaxonomyService
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string DefaultAssetPath = "Assets/Resources/VehicleTaxonomy_Default.asset";

        public static VehicleTaxonomyAsset GetOrCreateDefault(bool createIfMissing)
        {
            var taxonomy = VehicleTaxonomyAsset.GetDefault();
            if (taxonomy != null || !createIfMissing)
            {
                if (taxonomy != null)
                {
                    EnsureTaxonomyCoverage(taxonomy);
                }
                return taxonomy;
            }

            EnsureResourcesFolder();
            taxonomy = ScriptableObject.CreateInstance<VehicleTaxonomyAsset>();
            SeedFromLegacyEnums(taxonomy);
            AssetDatabase.CreateAsset(taxonomy, DefaultAssetPath);
            EditorUtility.SetDirty(taxonomy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return taxonomy;
        }

        public static void EnsureCompanionProfiles()
        {
            EnsureResourcesFolder();

            if (ModuleCompatibilityProfile.GetDefault() == null)
            {
                var profile = ScriptableObject.CreateInstance<ModuleCompatibilityProfile>();
                AssetDatabase.CreateAsset(profile, "Assets/Resources/ModuleCompatibilityProfile_Default.asset");
                EditorUtility.SetDirty(profile);
            }

            if (PipelineShaderFallbackProfile.GetDefault() == null)
            {
                var fallback = ScriptableObject.CreateInstance<PipelineShaderFallbackProfile>();
                fallback.builtInDefaultShader = "Standard";
                fallback.urpDefaultShader = "Universal Render Pipeline/Lit";
                fallback.hdrpDefaultShader = "HDRP/Lit";
                AssetDatabase.CreateAsset(fallback, "Assets/Resources/PipelineShaderFallbackProfile_Default.asset");
                EditorUtility.SetDirty(fallback);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
        }

        public static void SeedFromLegacyEnums(VehicleTaxonomyAsset taxonomy)
        {
            taxonomy.types = new List<VehicleTaxonomyAsset.VehicleTypeEntry>
            {
                BuildType("land", "Land", Enum.GetNames(typeof(VehicleConfig.LandVehicleCategory))),
                BuildType("air", "Air", Enum.GetNames(typeof(VehicleConfig.AirVehicleCategory))),
                BuildType("water", "Water", Enum.GetNames(typeof(VehicleConfig.WaterVehicleCategory))),
                BuildType("rail", "Rail", Enum.GetNames(typeof(VehicleConfig.RailVehicleCategory))),
                BuildType("space", "Space", Enum.GetNames(typeof(VehicleConfig.SpaceVehicleCategory))),
                BuildType("fictional", "Fictional", new[] { "Standard" })
            };

            AddSubcategories(taxonomy, "land", "specialized", Enum.GetNames(typeof(VehicleConfig.SpecializedLandVehicleType)));
            AddSubcategories(taxonomy, "air", "specialized", Enum.GetNames(typeof(VehicleConfig.SpecializedAirVehicleType)));
        }

        private static void EnsureTaxonomyCoverage(VehicleTaxonomyAsset taxonomy)
        {
            if (taxonomy == null) return;

            EnsureType(taxonomy, "land", "Land", Enum.GetNames(typeof(VehicleConfig.LandVehicleCategory)));
            EnsureType(taxonomy, "air", "Air", Enum.GetNames(typeof(VehicleConfig.AirVehicleCategory)));
            EnsureType(taxonomy, "water", "Water", Enum.GetNames(typeof(VehicleConfig.WaterVehicleCategory)));
            EnsureType(taxonomy, "rail", "Rail", Enum.GetNames(typeof(VehicleConfig.RailVehicleCategory)));
            EnsureType(taxonomy, "space", "Space", Enum.GetNames(typeof(VehicleConfig.SpaceVehicleCategory)));
            EnsureType(taxonomy, "fictional", "Fictional", new[] { "Standard" });

            AddSubcategories(taxonomy, "land", "specialized", Enum.GetNames(typeof(VehicleConfig.SpecializedLandVehicleType)));
            AddSubcategories(taxonomy, "air", "specialized", Enum.GetNames(typeof(VehicleConfig.SpecializedAirVehicleType)));

            EditorUtility.SetDirty(taxonomy);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureType(VehicleTaxonomyAsset taxonomy, string id, string display, IEnumerable<string> categories)
        {
            var type = taxonomy.FindType(id);
            if (type == null)
            {
                taxonomy.types.Add(BuildType(id, display, categories));
                return;
            }

            if (type.categories == null)
                type.categories = new List<VehicleTaxonomyAsset.VehicleCategoryEntry>();

            foreach (var c in categories)
            {
                string cid = c.ToLowerInvariant();
                if (!type.categories.Any(x => string.Equals(x.id, cid, StringComparison.OrdinalIgnoreCase)))
                {
                    type.categories.Add(new VehicleTaxonomyAsset.VehicleCategoryEntry
                    {
                        id = cid,
                        displayName = Nicify(c)
                    });
                }
            }
        }

        private static VehicleTaxonomyAsset.VehicleTypeEntry BuildType(string id, string display, IEnumerable<string> categories)
        {
            var entry = new VehicleTaxonomyAsset.VehicleTypeEntry
            {
                id = id,
                displayName = display
            };

            entry.categories = categories
                .Select(c => new VehicleTaxonomyAsset.VehicleCategoryEntry
                {
                    id = c.ToLowerInvariant(),
                    displayName = Nicify(c)
                })
                .ToList();

            return entry;
        }

        private static void AddSubcategories(VehicleTaxonomyAsset taxonomy, string typeId, string categoryId, IEnumerable<string> subcategories)
        {
            var type = taxonomy.FindType(typeId);
            var category = type?.categories?.FirstOrDefault(c => string.Equals(c.id, categoryId, StringComparison.OrdinalIgnoreCase));
            if (category == null) return;

            category.subcategories = subcategories
                .Select(s => new VehicleTaxonomyAsset.VehicleSubcategoryEntry
                {
                    id = s.ToLowerInvariant(),
                    displayName = Nicify(s)
                })
                .ToList();
        }

        private static string Nicify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return ObjectNames.NicifyVariableName(value);
        }
    }
}
