using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class InfoModule : VehicleEditorModuleBase
    {
        private VisualElement _dropZone;
        private Label _dropZoneLabel;
        private TextField _vehicleNameField;
        private TextField _manufacturerField;
        private TextField _seedField;
        private Label _idLabel;
        private EnumField _typeField;
        private EnumField _categoryField;
        private EnumField _specializedField;
        private Button _generateIDButton;
            private readonly Dictionary<VehicleType, Enum> _vehicleCategoryMap = new()
{
    { VehicleType.Land, global::VehicleConfig.LandVehicleCategory.Standard },
    { VehicleType.Air, global::VehicleConfig.AirVehicleCategory.Airplane },
    { VehicleType.Water, global::VehicleConfig.WaterVehicleCategory.Boat },
    { VehicleType.Space, global::VehicleConfig.SpaceVehicleCategory.Spaceship }
};

        private bool TypeSupportsSpecialization(VehicleType t)
        {
            return t == VehicleType.Land || t == VehicleType.Air;
        }


        private class VehicleSpecializationRule
        {
            public bool hasSpecializedCategory;
            public bool hasSpecializedEnum;

            public Type categoryEnumType;
            public object specializedCategoryValue;

            public Type specializedEnumType;
            public object specializedEnumDefaultValue;
        }


        private readonly Dictionary<VehicleConfig.VehicleType, VehicleSpecializationRule> _rules =
     new()
     {
        {
            VehicleConfig.VehicleType.Land,
            new VehicleSpecializationRule
            {
                hasSpecializedCategory = true,
                hasSpecializedEnum      = true,

                categoryEnumType          = typeof(VehicleConfig.LandVehicleCategory),
                specializedCategoryValue  = VehicleConfig.LandVehicleCategory.Specialized,

                specializedEnumType       = typeof(VehicleConfig.SpecializedLandVehicleType),
                specializedEnumDefaultValue = VehicleConfig.SpecializedLandVehicleType.Construction
            }
        },
        {
            VehicleConfig.VehicleType.Air,
            new VehicleSpecializationRule
            {
                hasSpecializedCategory = true,
                hasSpecializedEnum     = true,

                categoryEnumType         = typeof(VehicleConfig.AirVehicleCategory),
                specializedCategoryValue = VehicleConfig.AirVehicleCategory.Specialized,

                specializedEnumType      = typeof(VehicleConfig.SpecializedAirVehicleType),
                specializedEnumDefaultValue = VehicleConfig.SpecializedAirVehicleType.VTOL
            }
        },
        {
            VehicleConfig.VehicleType.Water,
            new VehicleSpecializationRule
            {
                hasSpecializedCategory = false,
                hasSpecializedEnum     = false,

                categoryEnumType = typeof(VehicleConfig.WaterVehicleCategory),
                specializedCategoryValue = VehicleConfig.WaterVehicleCategory.Standard
            }
        },
        {
            VehicleConfig.VehicleType.Space,
            new VehicleSpecializationRule
            {
                hasSpecializedCategory = false,
                hasSpecializedEnum     = false,

                categoryEnumType = typeof(VehicleConfig.SpaceVehicleCategory),
                specializedCategoryValue = VehicleConfig.SpaceVehicleCategory.Standard
            }
        },
        {
            VehicleConfig.VehicleType.Fictional,
            new VehicleSpecializationRule
            {
                hasSpecializedCategory = false,
                hasSpecializedEnum     = false,

                // choose whatever default category you want:
                categoryEnumType = typeof(VehicleConfig.LandVehicleCategory),
                specializedCategoryValue = VehicleConfig.LandVehicleCategory.Standard
            }
        }
     };


        public override string ModuleId => "info";
        public override string DisplayName => "Info";
        public override int Priority => 10;
        public override bool RequiresVehicle => false;

        protected override VisualElement CreateModuleUI()
        {
            var container = new VisualElement
            {
                style =
                {
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 20,
                    paddingBottom = 20
                }
            };

            CreateDropZone(container);
            CreateVehicleInfoSection(container);
            CreateIDGenerationSection(container);

            return container;
        }

        private void CreateDropZone(VisualElement parent)
        {
            var dropZoneContainer = new VisualElement { style = { marginBottom = 20 } };

            var dropZoneLabel = new Label("Drop Vehicle Prefab Here")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            dropZoneContainer.Add(dropZoneLabel);

            _dropZone = new VisualElement
            {
                style =
                {
                    height = 100,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                    borderLeftWidth = 2,
                    borderRightWidth = 2,
                    borderTopWidth = 2,
                    borderBottomWidth = 2,
                    borderLeftColor = Color.gray,
                    borderRightColor = Color.gray,
                    borderTopColor = Color.gray,
                    borderBottomColor = Color.gray,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };

            _dropZoneLabel = new Label("Drag & drop a vehicle prefab here") { style = { color = Color.white } };
            _dropZone.Add(_dropZoneLabel);

            _dropZone.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _dropZone.RegisterCallback<DragPerformEvent>(OnDragPerform);

            dropZoneContainer.Add(_dropZone);
            parent.Add(dropZoneContainer);
        }

        private void CreateVehicleInfoSection(VisualElement parent)
        {
            var infoContainer = new VisualElement { style = { marginBottom = 20 } };

            var infoLabel = new Label("Vehicle Information")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            infoContainer.Add(infoLabel);

            _vehicleNameField = new TextField("Vehicle Name") { style = { marginBottom = 5 } };
            infoContainer.Add(_vehicleNameField);

            _manufacturerField = new TextField("Manufacturer") { style = { marginBottom = 5 } };
            infoContainer.Add(_manufacturerField);

            _typeField = new EnumField("Vehicle Type", VehicleType.Land) { style = { marginBottom = 5 } };
            _typeField.RegisterValueChangedCallback(evt =>
            {
                UpdateGenerateButton();
                UpdateCategoryField();
                UpdateSpecializedField();
            });

            infoContainer.Add(_typeField);

            UpdateCategoryField();     // will create _categoryField
            UpdateSpecializedField();  // will create or remove _specializedField as needed


            parent.Add(infoContainer);
        }

        private void CreateIDGenerationSection(VisualElement parent)
        {
            var idContainer = new VisualElement();

            var idLabel = new Label("Vehicle ID Generation")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            idContainer.Add(idLabel);

            var seedContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var seedLabel = new Label("Seed (3 chars):") { style = { width = 120 } };
            seedContainer.Add(seedLabel);

            _seedField = new TextField { style = { width = 60 } };
            _seedField.maxLength = 3;
            _seedField.RegisterValueChangedCallback(_ => UpdateGenerateButton());
            seedContainer.Add(_seedField);

            idContainer.Add(seedContainer);

            _generateIDButton = new Button(OnGenerateID)
            {
                text = "Generate ID",
                style = { marginTop = 10 }
            };
            _generateIDButton.SetEnabled(false);
            idContainer.Add(_generateIDButton);

            _idLabel = new Label("No ID generated yet")
            {
                style =
                {
                    marginTop = 10,
                    color = Color.yellow
                }
            };
            idContainer.Add(_idLabel);

            parent.Add(idContainer);
        }

        private void UpdateCategoryField()
        {
            // Remove old category field if it exists
            _categoryField?.RemoveFromHierarchy();
            _categoryField = null;

            // Get the selected module type (InfoModule.VehicleType)
            var type = (VehicleType)_typeField.value;

            // Determine the enum type we must present and a default value
            Type categoryEnumType;
            Enum defaultCategoryValue;

            switch (type)
            {
                case VehicleType.Land:
                    categoryEnumType = typeof(VehicleConfig.LandVehicleCategory);
                    defaultCategoryValue = VehicleConfig.LandVehicleCategory.Standard;
                    break;

                case VehicleType.Air:
                    categoryEnumType = typeof(VehicleConfig.AirVehicleCategory);
                    defaultCategoryValue = VehicleConfig.AirVehicleCategory.Airplane;
                    break;

                case VehicleType.Water:
                    categoryEnumType = typeof(VehicleConfig.WaterVehicleCategory);
                    defaultCategoryValue = VehicleConfig.WaterVehicleCategory.Standard;
                    break;

                case VehicleType.Space:
                    categoryEnumType = typeof(VehicleConfig.SpaceVehicleCategory);
                    defaultCategoryValue = VehicleConfig.SpaceVehicleCategory.Standard;
                    break;

                case VehicleType.Fictional:
                default:
                    // choose a safe enum type for Fictional — you used LandVehicleCategory earlier
                    categoryEnumType = typeof(VehicleConfig.LandVehicleCategory);
                    defaultCategoryValue = VehicleConfig.LandVehicleCategory.Standard;
                    break;
            }

            // Create EnumField using the proper typed Enum value
            _categoryField = new EnumField("Vehicle Category", defaultCategoryValue)
            {
                style = { marginBottom = 5 }
            };

            // When user changes category, specialized field must update
            _categoryField.RegisterValueChangedCallback(_ => UpdateSpecializedField());

            // Add to UI
            _typeField.parent.Add(_categoryField);
        }

        private void UpdateSpecializedField()
        {
            // Remove old specialized field cleanly
            _specializedField?.RemoveFromHierarchy();
            _specializedField = null;

            // Read current selected type and category
            var type = (VehicleType)_typeField.value;

            // If category field doesn't exist yet, nothing to do
            if (_categoryField == null)
                return;

            // Convert the category field to string to check if user picked "Specialized"
            // (we compare names because category enum types differ per vehicle type)
            string categoryName = _categoryField.value?.ToString() ?? "Standard";

            bool supportsSpecial = TypeSupportsSpecialization(type);

            // If the type does not support specialization -> force category to Standard and bail out
            if (!supportsSpecial)
            {
                // force Standard on that category enum type
                // We can't directly assign a different enum type; use Enum.Parse on the correct enum type.
                // Determine the enum type used by _categoryField:
                var enumType = _categoryField.value.GetType();

                if (enumType != null)
                {
                    // assign Standard (safely)
                    try
                    {
                        var standardValue = Enum.Parse(enumType, "Standard");
                        _categoryField.value = (Enum)standardValue;
                    }
                    catch
                    {
                        // fallback: do nothing if the enum doesn't have Standard
                    }
                }

                return;
            }

            // Only show specialized field if category is "Specialized"
            if (!string.Equals(categoryName, "Specialized", StringComparison.OrdinalIgnoreCase))
                return;

            // Create correct specialized enum field depending on type (Land or Air)
            if (type == VehicleType.Land)
            {
                _specializedField = new EnumField(
                    "Specialized Type",
                    VehicleConfig.SpecializedLandVehicleType.Construction)
                {
                    style = { marginBottom = 5 }
                };
            }
            else if (type == VehicleType.Air)
            {
                _specializedField = new EnumField(
                    "Specialized Type",
                    VehicleConfig.SpecializedAirVehicleType.VTOL)
                {
                    style = { marginBottom = 5 }
                };
            }

            if (_specializedField != null)
            {
                _typeField.parent.Add(_specializedField);
            }
        }


        private void OnDragUpdated(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length == 1 &&
                DragAndDrop.objectReferences[0] is GameObject go &&
                PrefabUtility.IsPartOfPrefabAsset(go) &&
                go.CompareTag("vehicle"))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            e.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent e)
        {
            e.StopPropagation();

            if (DragAndDrop.objectReferences.Length != 1 ||
                DragAndDrop.objectReferences[0] is not GameObject go)
            {
                LogError("Please drop exactly one prefab here.");
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(go))
            {
                LogError($"\"{go.name}\" is not a Prefab asset.");
                return;
            }

            HandlePrefabDropped(go);
        }

        protected override void OnModuleActivated()
        {
            // Load or refresh module state
            if (_context?.CurrentConfig != null)
            {
                OnConfigChanged(_context.CurrentConfig);
            }
        }

        private void HandlePrefabDropped(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string prefabGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // Check if this vehicle already has a config
            if (_context.GuidToConfigMap.TryGetValue(prefabGuid, out var existingConfig))
            {
                // Vehicle already registered - load it!
                _context.NotifyConfigChanged(existingConfig);
                _context.NotifyPrefabChanged(prefab);
                _context.IsFinalized = true;

                _idLabel.text = existingConfig.id;
                _idLabel.style.color = Color.green;
                _vehicleNameField.value = prefab.name;

                _dropZoneLabel.text = $"Loaded: {prefab.name}";
                _dropZone.AddToClassList("dropzone-selected");

                LogMessage($"Loaded existing vehicle: {existingConfig.id}");

                // CRITICAL: Request validation to update all tabs
                _context.RequestValidation();

                return;
            }

            // New vehicle - not registered yet
            _context.NotifyPrefabChanged(prefab);
            _context.IsFinalized = false;

            _dropZoneLabel.text = $"New Vehicle: {prefab.name}";
            _dropZone.AddToClassList("dropzone-selected");
            _vehicleNameField.value = prefab.name;

            UpdateGenerateButton();
            LogMessage($"New vehicle prefab selected: {prefab.name}. Generate ID to register.");
        }

        private void UpdateGenerateButton()
        {
            bool valid = _context.SelectedPrefab != null &&
                        _typeField.value != null &&
                        _seedField.value.Trim().Length == 3;
            _generateIDButton.SetEnabled(valid);
        }



        private void OnGenerateID()
        {
            if (_context.SelectedPrefab == null)
            {
                LogError("No prefab selected.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
            string prefabGuid = AssetDatabase.AssetPathToGUID(assetPath);

            var type = (VehicleType)_typeField.value;
            string prefix = type.ToString()[0].ToString();
            string seed = _seedField.value.Trim().ToUpper();

            if (seed.Length != 3)
            {
                LogError("Seed must be 3 characters.");
                return;
            }

            string guid = System.Guid.NewGuid().ToString("N").ToUpper();
            string id = $"{prefix}{seed}-{guid[..4]}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}";

            int tries = 0;
            while (_context.Registry.ContainsID(id) && tries++ < 5)
            {
                guid = System.Guid.NewGuid().ToString("N").ToUpper();
                id = $"{prefix}{seed}-{guid[..4]}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}";
            }

            if (_context.Registry.ContainsID(id))
            {
                LogError("ID collision detected. Please try again.");
                return;
            }

            _context.Registry.RegisterID(id, _context.SelectedPrefab);
            EditorUtility.SetDirty(_context.Registry);
            AssetDatabase.SaveAssets();

            var newConfig = ScriptableObject.CreateInstance<VehicleConfig>();
            newConfig.prefabReference = _context.SelectedPrefab;
            newConfig.prefabGuid = prefabGuid;
            newConfig.id = id;

            const string folder = "Assets/VehicleConfigs";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "VehicleConfigs");

            string configPath = $"{folder}/{_context.SelectedPrefab.name}_{prefabGuid[..8]}Config.asset";
            AssetDatabase.CreateAsset(newConfig, configPath);
            AssetDatabase.SaveAssets();

            _context.GuidToConfigMap[prefabGuid] = newConfig;
            _context.NotifyConfigChanged(newConfig);
            _context.IsFinalized = true;

            _idLabel.text = id;
            _idLabel.style.color = Color.green;

            LogMessage($"Vehicle ID \"{id}\" generated successfully.");
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context.SelectedPrefab == null)
                return ValidationResult.Warning("No vehicle prefab selected");

            if (!_context.IsFinalized)
                return ValidationResult.Warning("Vehicle ID not generated yet");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config != null)
            {
                _vehicleNameField.value = config.prefabReference != null ? config.prefabReference.name : null ?? "";
                _idLabel.text = config.id;
                _idLabel.style.color = Color.green;
            }
        }

        protected override void OnPrefabChanged(GameObject prefab)
        {
            if (prefab != null)
            {
                _vehicleNameField.value = prefab.name;
            }
        }

        public override void OnModuleGUI() { }

        public enum VehicleType { Land, Air, Water, Space, Fictional }
    }
}