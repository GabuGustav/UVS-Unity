using System;

public static class VehicleClassificationResolver
{
    public static string GetTypeId(VehicleConfig config)
    {
        if (config == null) return "land";
        config.EnsureClassificationDefaults();
        return string.IsNullOrWhiteSpace(config.classification.typeId)
            ? config.GetLegacyTypeId()
            : config.classification.typeId.ToLowerInvariant();
    }

    public static string GetCategoryId(VehicleConfig config)
    {
        if (config == null) return "standard";
        config.EnsureClassificationDefaults();
        return string.IsNullOrWhiteSpace(config.classification.categoryId)
            ? config.GetLegacyCategoryId()
            : config.classification.categoryId.ToLowerInvariant();
    }

    public static string GetSubcategoryId(VehicleConfig config)
    {
        if (config == null) return string.Empty;
        config.EnsureClassificationDefaults();
        return (config.classification.subcategoryId ?? string.Empty).ToLowerInvariant();
    }

    public static bool Matches(VehicleConfig config, string typeId, string categoryId = null, string subcategoryId = null)
    {
        if (config == null) return false;

        if (!string.IsNullOrWhiteSpace(typeId) &&
            !string.Equals(GetTypeId(config), typeId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(categoryId) &&
            !string.Equals(GetCategoryId(config), categoryId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(subcategoryId) &&
            !string.Equals(GetSubcategoryId(config), subcategoryId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
