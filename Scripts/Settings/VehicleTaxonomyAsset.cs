using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "UVS/Taxonomy/Vehicle Taxonomy", fileName = "VehicleTaxonomy")]
public class VehicleTaxonomyAsset : ScriptableObject
{
    [Serializable]
    public class VehicleSubcategoryEntry
    {
        public string id;
        public string displayName;
    }

    [Serializable]
    public class VehicleCategoryEntry
    {
        public string id;
        public string displayName;
        public List<VehicleSubcategoryEntry> subcategories = new();
    }

    [Serializable]
    public class VehicleTypeEntry
    {
        public string id;
        public string displayName;
        public List<string> tags = new();
        public List<VehicleCategoryEntry> categories = new();
    }

    public const string DefaultResourcePath = "VehicleTaxonomy_Default";
    public List<VehicleTypeEntry> types = new();

    public VehicleTypeEntry FindType(string typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId)) return null;
        return types.FirstOrDefault(t =>
            string.Equals(t.id, typeId, StringComparison.OrdinalIgnoreCase));
    }

    public VehicleCategoryEntry FindCategory(string typeId, string categoryId)
    {
        var type = FindType(typeId);
        if (type == null || string.IsNullOrWhiteSpace(categoryId)) return null;
        return type.categories.FirstOrDefault(c =>
            string.Equals(c.id, categoryId, StringComparison.OrdinalIgnoreCase));
    }

    public VehicleSubcategoryEntry FindSubcategory(string typeId, string categoryId, string subcategoryId)
    {
        var category = FindCategory(typeId, categoryId);
        if (category == null || string.IsNullOrWhiteSpace(subcategoryId)) return null;
        return category.subcategories.FirstOrDefault(s =>
            string.Equals(s.id, subcategoryId, StringComparison.OrdinalIgnoreCase));
    }

    public static VehicleTaxonomyAsset GetDefault()
    {
        return Resources.Load<VehicleTaxonomyAsset>(DefaultResourcePath);
    }
}
