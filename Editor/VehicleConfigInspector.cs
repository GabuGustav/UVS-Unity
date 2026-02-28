using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleConfig))]
public class VehicleConfigInspector : Editor
{
    public override void OnInspectorGUI()
    {
        VehicleConfig config = (VehicleConfig)target;

        // Draw default inspector for everything except classification
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip the category/specialized fields - we'll draw them manually
            if (prop.name == "landCategory" ||
                prop.name == "airCategory" ||
                prop.name == "waterCategory" ||
                prop.name == "spaceCategory" ||
                prop.name == "railCategory" ||
                prop.name == "specializedLand" ||
                prop.name == "specializedAir")
            {
                continue;
            }

            // Draw vehicle type
            if (prop.name == "vehicleType")
            {
                EditorGUILayout.PropertyField(prop, true);

                // Now draw the appropriate category based on vehicle type
                DrawCategoryField(config);

                // Draw specialized field if needed
                if (config.IsSpecialized)
                {
                    DrawSpecializedField(config);
                }

                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategoryField(VehicleConfig config)
    {
        EditorGUI.BeginChangeCheck();

        switch (config.vehicleType)
        {
            case VehicleConfig.VehicleType.Land:
                var newLandCat = (VehicleConfig.LandVehicleCategory)EditorGUILayout.EnumPopup(
                    "Vehicle Category", config.landCategory);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Land Category");
                    config.landCategory = newLandCat;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Air:
                var newAirCat = (VehicleConfig.AirVehicleCategory)EditorGUILayout.EnumPopup(
                    "Vehicle Category", config.airCategory);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Air Category");
                    config.airCategory = newAirCat;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Water:
                var newWaterCat = (VehicleConfig.WaterVehicleCategory)EditorGUILayout.EnumPopup(
                    "Vehicle Category", config.waterCategory);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Water Category");
                    config.waterCategory = newWaterCat;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Space:
                var newSpaceCat = (VehicleConfig.SpaceVehicleCategory)EditorGUILayout.EnumPopup(
                    "Vehicle Category", config.spaceCategory);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Space Category");
                    config.spaceCategory = newSpaceCat;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Rail:
                var newRailCat = (VehicleConfig.RailVehicleCategory)EditorGUILayout.EnumPopup(
                    "Vehicle Category", config.railCategory);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Rail Category");
                    config.railCategory = newRailCat;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Fictional:
                EditorGUILayout.LabelField("Vehicle Category", "N/A (Fictional)");
                break;
        }
    }

    private void DrawSpecializedField(VehicleConfig config)
    {
        EditorGUI.BeginChangeCheck();

        switch (config.vehicleType)
        {
            case VehicleConfig.VehicleType.Land:
                var newLandSpec = (VehicleConfig.SpecializedLandVehicleType)EditorGUILayout.EnumPopup(
                    "Specialized Type", config.specializedLand);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Land Specialized Type");
                    config.specializedLand = newLandSpec;
                    EditorUtility.SetDirty(config);
                }
                break;

            case VehicleConfig.VehicleType.Air:
                var newAirSpec = (VehicleConfig.SpecializedAirVehicleType)EditorGUILayout.EnumPopup(
                    "Specialized Type", config.specializedAir);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Air Specialized Type");
                    config.specializedAir = newAirSpec;
                    EditorUtility.SetDirty(config);
                }
                break;
        }
    }
}
