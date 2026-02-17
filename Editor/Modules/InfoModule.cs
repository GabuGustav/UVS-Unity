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

        public override string ModuleId => "info";
        public override string DisplayName => "Info";
        public override int Priority => 10;
        public override bool RequiresVehicle => false;
        public override bool RequiresSpecializedCategory => false;
        public override bool IsConstructionModule => false;
        public override bool IsTankModule => false;
        public override bool IsVTOLModule => false;

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
            _vehicleNameField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.vehicleName = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            infoContainer.Add(_vehicleNameField);

            _manufacturerField = new TextField("Manufacturer") { style = { marginBottom = 5 } };
            _manufacturerField.RegisterValueChangedCallback(evt =>
            {
                if (_context?.CurrentConfig != null)
                {
                    _context.CurrentConfig.authorName = evt.newValue;
                    EditorUtility.SetDirty(_context.CurrentConfig);
                }
            });
            infoContainer.Add(_manufacturerField);

            // Vehicle Type
            _typeField = new EnumField("Vehicle Type", VehicleConfig.VehicleType.Land) { style = { marginBottom = 5 } };
            _typeField.RegisterValueChangedCallback(OnVehicleTypeChanged);
            infoContainer.Add(_typeField);

            // Category field will be created dynamically
            // Specialized field will be created dynamically

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

        private void OnVehicleTypeChanged(ChangeEvent<Enum> evt)
        {
            if (_context?.CurrentConfig == null) return;

            var newType = (VehicleConfig.VehicleType)evt.newValue;

            // Save the type
            _context.CurrentConfig.vehicleType = newType;

            // Reset category to Standard when type changes
            ResetCategoryToStandard(newType);

            // Mark dirty
            EditorUtility.SetDirty(_context.CurrentConfig);

            // Rebuild category UI
            RebuildCategoryField();

            // Update generate button
            UpdateGenerateButton();

            // Notify context that config changed (triggers module visibility updates)
            _context.NotifyConfigChanged(_context.CurrentConfig);
            VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
        }

        private void ResetCategoryToStandard(VehicleConfig.VehicleType type)
        {
            if (_context?.CurrentConfig == null) return;

            switch (type)
            {
                case VehicleConfig.VehicleType.Land:
                    _context.CurrentConfig.landCategory = VehicleConfig.LandVehicleCategory.Standard;
                    break;
                case VehicleConfig.VehicleType.Air:
                    _context.CurrentConfig.airCategory = VehicleConfig.AirVehicleCategory.Standard;
                    break;
                case VehicleConfig.VehicleType.Water:
                    _context.CurrentConfig.waterCategory = VehicleConfig.WaterVehicleCategory.Standard;
                    break;
                case VehicleConfig.VehicleType.Space:
                    _context.CurrentConfig.spaceCategory = VehicleConfig.SpaceVehicleCategory.Standard;
                    break;
            }
        }

        private void RebuildCategoryField()
        {
            if (_context?.CurrentConfig == null) return;

            // Remove old category field
            _categoryField?.RemoveFromHierarchy();
            _categoryField = null;

            // Remove old specialized field
            _specializedField?.RemoveFromHierarchy();
            _specializedField = null;

            var type = _context.CurrentConfig.vehicleType;

            // Create appropriate category field based on type
            switch (type)
            {
                case VehicleConfig.VehicleType.Land:
                    _categoryField = new EnumField("Vehicle Category", _context.CurrentConfig.landCategory);
                    _categoryField.RegisterValueChangedCallback(evt =>
                    {
                        var newCategory = (VehicleConfig.LandVehicleCategory)evt.newValue;
                        _context.CurrentConfig.landCategory = newCategory;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        // Rebuild specialized field if needed
                        RebuildSpecializedField();

                        // Notify modules to update visibility
                        _context.NotifyConfigChanged(_context.CurrentConfig);
                        VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
                    });
                    break;

                case VehicleConfig.VehicleType.Air:
                    _categoryField = new EnumField("Vehicle Category", _context.CurrentConfig.airCategory);
                    _categoryField.RegisterValueChangedCallback(evt =>
                    {
                        var newCategory = (VehicleConfig.AirVehicleCategory)evt.newValue;
                        _context.CurrentConfig.airCategory = newCategory;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        RebuildSpecializedField();
                        _context.NotifyConfigChanged(_context.CurrentConfig);
                        VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
                    });
                    break;

                case VehicleConfig.VehicleType.Water:
                    _categoryField = new EnumField("Vehicle Category", _context.CurrentConfig.waterCategory);
                    _categoryField.RegisterValueChangedCallback(evt =>
                    {
                        var newCategory = (VehicleConfig.WaterVehicleCategory)evt.newValue;
                        _context.CurrentConfig.waterCategory = newCategory;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        _context.NotifyConfigChanged(_context.CurrentConfig);
                        VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
                    });
                    break;

                case VehicleConfig.VehicleType.Space:
                    _categoryField = new EnumField("Vehicle Category", _context.CurrentConfig.spaceCategory);
                    _categoryField.RegisterValueChangedCallback(evt =>
                    {
                        var newCategory = (VehicleConfig.SpaceVehicleCategory)evt.newValue;
                        _context.CurrentConfig.spaceCategory = newCategory;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        _context.NotifyConfigChanged(_context.CurrentConfig);
                        VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
                    });
                    break;

                case VehicleConfig.VehicleType.Fictional:
                    // No category for fictional
                    break;
            }

            // Add category field after type field
            if (_categoryField != null)
            {
                _categoryField.style.marginBottom = 5;
                _typeField.parent.Add(_categoryField);
            }

            // Check if we need specialized field
            RebuildSpecializedField();
        }

        private void RebuildSpecializedField()
        {
            if (_context?.CurrentConfig == null) return;

            // Remove old specialized field
            _specializedField?.RemoveFromHierarchy();
            _specializedField = null;

            // Only show specialized field if category is "Specialized"
            if (!_context.CurrentConfig.IsSpecialized) return;

            var type = _context.CurrentConfig.vehicleType;

            switch (type)
            {
                case VehicleConfig.VehicleType.Land:
                    _specializedField = new EnumField("Specialized Type", _context.CurrentConfig.specializedLand);
                    _specializedField.RegisterValueChangedCallback(evt =>
                    {
                        var newSpec = (VehicleConfig.SpecializedLandVehicleType)evt.newValue;
                        _context.CurrentConfig.specializedLand = newSpec;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        // Notify modules to update visibility
                        _context.NotifyConfigChanged(_context.CurrentConfig);
                        VehicleRuntimeAutoBinder.QueueSync(_context.CurrentConfig);
                    });
                    break;

                case VehicleConfig.VehicleType.Air:
                    _specializedField = new EnumField("Specialized Type", _context.CurrentConfig.specializedAir);
                    _specializedField.RegisterValueChangedCallback(evt =>
                    {
                        var newSpec = (VehicleConfig.SpecializedAirVehicleType)evt.newValue;
                        _context.CurrentConfig.specializedAir = newSpec;
                        EditorUtility.SetDirty(_context.CurrentConfig);

                        _context.NotifyConfigChanged(_context.CurrentConfig);
                    });
                    break;
            }

            // Add specialized field after category field
            if (_specializedField != null)
            {
                _specializedField.style.marginBottom = 5;

                // Insert after category field
                if (_categoryField != null)
                {
                    _categoryField.parent.Add(_specializedField);
                }
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
                // Vehicle already registered - load it
                _context.NotifyConfigChanged(existingConfig);
                _context.NotifyPrefabChanged(prefab);
                _context.IsFinalized = true;

                LoadConfigValues(existingConfig);

                _dropZoneLabel.text = $"Loaded: {prefab.name}";
                _dropZone.AddToClassList("dropzone-selected");

                LogMessage($"Loaded existing vehicle: {existingConfig.id}");
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

        private void LoadConfigValues(VehicleConfig config)
        {
            _vehicleNameField.value = config.vehicleName;
            _manufacturerField.value = config.authorName;
            _idLabel.text = config.id;
            _idLabel.style.color = Color.green;

            // Load type
            _typeField.value = config.vehicleType;

            // Rebuild category/specialized fields with loaded values
            RebuildCategoryField();
        }

        private void UpdateGenerateButton()
        {
            // If this prefab already has a permanent ID, button stays off. No exceptions.
            bool alreadyRegistered = false;
            if (_context.SelectedPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(_context.SelectedPrefab);
                string guid = AssetDatabase.AssetPathToGUID(path);
                alreadyRegistered = _context.GuidToConfigMap.ContainsKey(guid) ||
                                    _context.Registry.PrefabHasConfig(_context.SelectedPrefab);
            }

            bool valid = _context.SelectedPrefab != null &&
                        _typeField.value != null &&
                        _seedField.value.Trim().Length == 3 &&
                        !alreadyRegistered;

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

            // ── PERMANENT ID GUARD ──────────────────────────────────
            // Check in-memory map first (fast)
            if (_context.GuidToConfigMap.TryGetValue(prefabGuid, out var existingFromMap))
            {
                LogError($"This vehicle already has a permanent ID: {existingFromMap.id}. IDs cannot be regenerated.");
                _idLabel.text = existingFromMap.id;
                _idLabel.style.color = Color.green;
                _generateIDButton.SetEnabled(false);
                return;
            }
            // Check registry on disk (catches cases after editor reload)
            if (_context.Registry.PrefabHasConfig(_context.SelectedPrefab))
            {
                var existingFromRegistry = _context.Registry.GetConfigForPrefab(_context.SelectedPrefab);
                string existingId = existingFromRegistry != null ? existingFromRegistry.id : "unknown";
                LogError($"This vehicle already has a permanent ID: {existingId}. IDs cannot be regenerated.");
                _idLabel.text = existingId;
                _idLabel.style.color = Color.green;
                _generateIDButton.SetEnabled(false);
                return;
            }
            // ────────────────────────────────────────────────────────

            var type = (VehicleConfig.VehicleType)_typeField.value;
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
            newConfig.vehicleName = _vehicleNameField.value;
            newConfig.authorName = _manufacturerField.value;
            newConfig.vehicleType = type;

            // Set initial category to Standard
            ResetCategoryToStandard(type);

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

            // Rebuild UI with new config
            RebuildCategoryField();

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
                LoadConfigValues(config);
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
    }
}
