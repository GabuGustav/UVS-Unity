using System;
using System.Linq;
using UnityEditor;

namespace UVS.Editor.Core
{
    public static class ModuleCompatibilityService
    {
        private static ModuleCompatibilityProfile _cachedProfile;
        private static double _nextRefreshTime;

        public static bool IsModuleAllowed(IVehicleEditorModule module, VehicleConfig config)
        {
            if (module == null) return false;
            if (config == null) return true;

            config.EnsureClassificationDefaults();
            string typeId = VehicleClassificationResolver.GetTypeId(config);
            string categoryId = VehicleClassificationResolver.GetCategoryId(config);
            string subcategoryId = VehicleClassificationResolver.GetSubcategoryId(config);

            if (TryEvaluateOverride(module.ModuleId, typeId, categoryId, subcategoryId, out bool overrideResult))
                return overrideResult;

            var attrs = module.GetType()
                .GetCustomAttributes(typeof(VehicleModuleSupportAttribute), true)
                .Cast<VehicleModuleSupportAttribute>()
                .ToArray();

            if (attrs.Length == 0)
                return true;

            foreach (var attr in attrs)
            {
                if (Matches(attr.typeId, typeId) &&
                    Matches(attr.categoryId, categoryId) &&
                    Matches(attr.subcategoryId, subcategoryId))
                {
                    return true;
                }
            }

            return false;
        }

        public static ModuleCompatibilityProfile GetProfile()
        {
            if (_cachedProfile != null && EditorApplication.timeSinceStartup < _nextRefreshTime)
                return _cachedProfile;

            _cachedProfile = ModuleCompatibilityProfile.GetDefault();
            _nextRefreshTime = EditorApplication.timeSinceStartup + 1d;
            return _cachedProfile;
        }

        private static bool TryEvaluateOverride(string moduleId, string typeId, string categoryId, string subcategoryId, out bool result)
        {
            result = false;
            var profile = GetProfile();
            if (profile == null || profile.overrides == null || profile.overrides.Count == 0)
                return false;

            ModuleCompatibilityProfile.ModuleRule best = null;
            int bestScore = -1;

            foreach (var rule in profile.overrides)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.moduleId))
                    continue;

                if (!string.Equals(rule.moduleId, moduleId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!Matches(rule.typeId, typeId) ||
                    !Matches(rule.categoryId, categoryId) ||
                    !Matches(rule.subcategoryId, subcategoryId))
                {
                    continue;
                }

                int score = Specificity(rule.typeId) + Specificity(rule.categoryId) + Specificity(rule.subcategoryId);
                if (score > bestScore)
                {
                    best = rule;
                    bestScore = score;
                }
            }

            if (best == null)
                return false;

            result = best.enabled;
            return true;
        }

        private static bool Matches(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected))
                return true;

            return string.Equals(expected.Trim(), actual, StringComparison.OrdinalIgnoreCase);
        }

        private static int Specificity(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? 0 : 1;
        }
    }
}
