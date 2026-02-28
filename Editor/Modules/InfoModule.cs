using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UVS.Editor.Core;

namespace UVS.Editor.Modules
{
    public class InfoModule : VehicleEditorModuleBase
    {
        private VisualElement _dropZone;
        private Label _dropZoneLabel;

        private TextField _vehicleNameField;
        private TextField _manufacturerField;

        private VisualElement _classificationContainer;
        private PopupField<string> _typePopup;
        private PopupField<string> _categoryPopup;
        private PopupField<string> _subcategoryPopup;

        private readonly List<string> _typeIds = new();
        private readonly List<string> _categoryIds = new();
        private readonly List<string> _subcategoryIds = new();

        private TextField _vehicleIdField;
        private TextField _prefabGuidField;
        private Label _idStatusLabel;

        private bool _isUpdatingUI;
        private VehicleTaxonomyAsset _taxonomy;

        public override string ModuleId => "info";
        public override string DisplayName => "Info";
        public override int Priority => 10;
        public override bool RequiresVehicle => false;

        protected override VisualElement CreateModuleUI()
        {
            _taxonomy = VehicleTaxonomyService.GetOrCreateDefault(createIfMissing: true);

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
            CreateVehicleIdentitySection(container);

            return container;
        }

        private void CreateDropZone(VisualElement parent)
        {
            var dropZoneContainer = new VisualElement { style = { marginBottom = 20 } };
            dropZoneContainer.Add(new Label("Drop Vehicle Prefab Here")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            });

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
            infoContainer.Add(new Label("Vehicle Information")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            });

            _vehicleNameField = new TextField("Vehicle Name") { style = { marginBottom = 5 } };
            _vehicleNameField.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingUI || _context?.CurrentConfig == null) return;
                _context.CurrentConfig.vehicleName = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
                _context.NotifyConfigChanged(_context.CurrentConfig);
            });
            infoContainer.Add(_vehicleNameField);

            _manufacturerField = new TextField("Manufacturer") { style = { marginBottom = 5 } };
            _manufacturerField.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingUI || _context?.CurrentConfig == null) return;
                _context.CurrentConfig.authorName = evt.newValue;
                EditorUtility.SetDirty(_context.CurrentConfig);
                _context.NotifyConfigChanged(_context.CurrentConfig);
            });
            infoContainer.Add(_manufacturerField);

            _classificationContainer = new VisualElement { style = { marginBottom = 5 } };
            infoContainer.Add(_classificationContainer);

            parent.Add(infoContainer);

            RebuildClassificationUI();
        }

        private void CreateVehicleIdentitySection(VisualElement parent)
        {
            var idContainer = new VisualElement { style = { marginTop = 8 } };

            idContainer.Add(new Label("Vehicle Identity")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            });

            var idRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            _vehicleIdField = new TextField("Vehicle ID") { style = { flexGrow = 1 } };
            _vehicleIdField.SetEnabled(false);
            idRow.Add(_vehicleIdField);
            idRow.Add(new Button(() => EditorGUIUtility.systemCopyBuffer = _vehicleIdField.value ?? string.Empty)
            {
                text = "Copy ID",
                style = { marginLeft = 6 }
            });
            idContainer.Add(idRow);

            var guidRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            _prefabGuidField = new TextField("Prefab GUID") { style = { flexGrow = 1 } };
            _prefabGuidField.SetEnabled(false);
            guidRow.Add(_prefabGuidField);
            guidRow.Add(new Button(() => EditorGUIUtility.systemCopyBuffer = _prefabGuidField.value ?? string.Empty)
            {
                text = "Copy GUID",
                style = { marginLeft = 6 }
            });
            idContainer.Add(guidRow);

            _idStatusLabel = new Label("Status: Waiting for prefab")
            {
                style =
                {
                    marginTop = 4,
                    color = Color.yellow,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            idContainer.Add(_idStatusLabel);

            parent.Add(idContainer);
        }

        private void RebuildClassificationUI()
        {
            _classificationContainer?.Clear();
            _typeIds.Clear();
            _categoryIds.Clear();
            _subcategoryIds.Clear();

            if (_taxonomy == null)
            {
                _classificationContainer?.Add(new Label("Taxonomy asset missing."));
                return;
            }

            string typeId = _context?.CurrentConfig != null
                ? VehicleClassificationResolver.GetTypeId(_context.CurrentConfig)
                : "land";
            string categoryId = _context?.CurrentConfig != null
                ? VehicleClassificationResolver.GetCategoryId(_context.CurrentConfig)
                : "standard";
            string subcategoryId = _context?.CurrentConfig != null
                ? VehicleClassificationResolver.GetSubcategoryId(_context.CurrentConfig)
                : string.Empty;

            var typeOptions = new List<string>();
            foreach (var type in _taxonomy.types)
            {
                _typeIds.Add(type.id);
                typeOptions.Add(FormatLabel(type.displayName, type.id));
            }

            int selectedType = Mathf.Max(0, _typeIds.FindIndex(t => string.Equals(t, typeId, StringComparison.OrdinalIgnoreCase)));
            if (selectedType >= typeOptions.Count) selectedType = 0;

            _typePopup = new PopupField<string>("Vehicle Type", typeOptions, selectedType);
            _typePopup.RegisterValueChangedCallback(_ =>
            {
                if (_isUpdatingUI) return;
                ApplyClassificationFromUI(rebuild: true);
            });
            _classificationContainer.Add(_typePopup);

            var selectedTypeEntry = _taxonomy.types.Count > selectedType ? _taxonomy.types[selectedType] : null;
            var categoryOptions = new List<string>();
            if (selectedTypeEntry != null)
            {
                foreach (var cat in selectedTypeEntry.categories)
                {
                    _categoryIds.Add(cat.id);
                    categoryOptions.Add(FormatLabel(cat.displayName, cat.id));
                }
            }

            if (categoryOptions.Count == 0)
            {
                _categoryIds.Add("standard");
                categoryOptions.Add("Standard");
            }

            int selectedCategory = Mathf.Max(0, _categoryIds.FindIndex(c => string.Equals(c, categoryId, StringComparison.OrdinalIgnoreCase)));
            if (selectedCategory >= categoryOptions.Count) selectedCategory = 0;

            _categoryPopup = new PopupField<string>("Vehicle Category", categoryOptions, selectedCategory);
            _categoryPopup.RegisterValueChangedCallback(_ =>
            {
                if (_isUpdatingUI) return;
                ApplyClassificationFromUI(rebuild: true);
            });
            _classificationContainer.Add(_categoryPopup);

            var selectedCategoryEntry = selectedTypeEntry != null && selectedCategory < selectedTypeEntry.categories.Count
                ? selectedTypeEntry.categories[selectedCategory]
                : null;

            if (selectedCategoryEntry != null && selectedCategoryEntry.subcategories != null && selectedCategoryEntry.subcategories.Count > 0)
            {
                var subOptions = new List<string>();
                foreach (var sub in selectedCategoryEntry.subcategories)
                {
                    _subcategoryIds.Add(sub.id);
                    subOptions.Add(FormatLabel(sub.displayName, sub.id));
                }

                int selectedSub = Mathf.Max(0, _subcategoryIds.FindIndex(s => string.Equals(s, subcategoryId, StringComparison.OrdinalIgnoreCase)));
                if (selectedSub >= subOptions.Count) selectedSub = 0;

                _subcategoryPopup = new PopupField<string>("Subcategory", subOptions, selectedSub);
                _subcategoryPopup.RegisterValueChangedCallback(_ =>
                {
                    if (_isUpdatingUI) return;
                    ApplyClassificationFromUI(rebuild: false);
                });
                _classificationContainer.Add(_subcategoryPopup);
            }
            else
            {
                _subcategoryPopup = null;
            }
        }

        private static string FormatLabel(string displayName, string id)
        {
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
            return ObjectNames.NicifyVariableName(id ?? "Unknown");
        }

        private static string GetSelectedId(PopupField<string> popup, List<string> ids, string fallback)
        {
            if (popup == null || ids.Count == 0) return fallback;
            int idx = popup.index;
            if (idx < 0 || idx >= ids.Count) return fallback;
            return ids[idx] ?? fallback;
        }

        private void ApplyClassificationFromUI(bool rebuild)
        {
            if (_context?.CurrentConfig == null) return;

            _context.CurrentConfig.classification ??= new VehicleConfig.VehicleClassificationData();
            _context.CurrentConfig.classification.typeId = GetSelectedId(_typePopup, _typeIds, "land");
            _context.CurrentConfig.classification.categoryId = GetSelectedId(_categoryPopup, _categoryIds, "standard");
            _context.CurrentConfig.classification.subcategoryId = _subcategoryPopup != null
                ? GetSelectedId(_subcategoryPopup, _subcategoryIds, string.Empty)
                : string.Empty;

            _context.CurrentConfig.SyncLegacyClassificationFromIds();
            EditorUtility.SetDirty(_context.CurrentConfig);

            if (rebuild)
            {
                _isUpdatingUI = true;
                RebuildClassificationUI();
                _isUpdatingUI = false;
            }

            _context.NotifyConfigChanged(_context.CurrentConfig);
        }

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length == 1 &&
                DragAndDrop.objectReferences[0] is GameObject go &&
                PrefabUtility.IsPartOfPrefabAsset(go))
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

            if (DragAndDrop.objectReferences.Length != 1 || DragAndDrop.objectReferences[0] is not GameObject go)
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

        private void HandlePrefabDropped(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string prefabGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(prefabGuid))
            {
                UpdateIdentityStatus("Missing prefab GUID. Cannot assign ID.", Color.red);
                return;
            }

            VehicleConfig cfg = null;
            bool created = false;
            bool existedInRegistry = false;

            if (_context.GuidToConfigMap.TryGetValue(prefabGuid, out var existing))
            {
                cfg = existing;
                existedInRegistry = true;
            }
            else if (_context.Registry != null && _context.Registry.TryGetByPrefabGuid(prefabGuid, out var fromRegistry))
            {
                cfg = fromRegistry;
                existedInRegistry = true;
            }
            else if (_context.Registry != null)
            {
                cfg = _context.Registry.GetOrCreateConfigForPrefab(prefab);
                created = cfg != null && !existedInRegistry;
            }

            if (cfg == null)
            {
                UpdateIdentityStatus("Failed to resolve/create config for prefab.", Color.red);
                return;
            }

            cfg.prefabReference = prefab;
            cfg.prefabGuid = prefabGuid;
            cfg.vehicleName = string.IsNullOrWhiteSpace(cfg.vehicleName) ? prefab.name : cfg.vehicleName;

            if (created || cfg.classification == null || string.IsNullOrWhiteSpace(cfg.classification.typeId))
            {
                cfg.classification ??= new VehicleConfig.VehicleClassificationData();
                cfg.classification.typeId = GetSelectedId(_typePopup, _typeIds, "land");
                cfg.classification.categoryId = GetSelectedId(_categoryPopup, _categoryIds, "standard");
                cfg.classification.subcategoryId = _subcategoryPopup != null
                    ? GetSelectedId(_subcategoryPopup, _subcategoryIds, string.Empty)
                    : string.Empty;
                cfg.SyncLegacyClassificationFromIds();
            }

            if (_context.Registry != null)
            {
                _context.Registry.EnsureDeterministicId(cfg);
                _context.Registry.UpsertConfig(cfg);
                EditorUtility.SetDirty(_context.Registry);
            }

            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();

            _context.GuidToConfigMap[prefabGuid] = cfg;
            _context.NotifyPrefabChanged(prefab);
            _context.NotifyConfigChanged(cfg);
            _context.IsFinalized = true;

            _dropZoneLabel.text = $"Loaded: {prefab.name}";
            _dropZone.AddToClassList("dropzone-selected");

            UpdateIdentityStatus(created ? "Synced (new config created)" : "Synced", Color.green);
            LogMessage($"Vehicle synced: {cfg.id}");
            _context.RequestValidation();
        }

        private void UpdateIdentityStatus(string text, Color color)
        {
            if (_idStatusLabel == null) return;
            _idStatusLabel.text = $"Status: {text}";
            _idStatusLabel.style.color = color;
        }

        protected override ValidationResult ValidateModule()
        {
            if (_context.SelectedPrefab == null)
                return ValidationResult.Warning("No vehicle prefab selected");

            if (_context.CurrentConfig == null)
                return ValidationResult.Error("No vehicle config loaded");

            if (string.IsNullOrWhiteSpace(_context.CurrentConfig.prefabGuid))
                return ValidationResult.Error("Prefab GUID is missing");

            string expected = VehicleConfig.ComputeDeterministicIdFromPrefabGuid(_context.CurrentConfig.prefabGuid);
            if (!string.Equals(_context.CurrentConfig.id, expected, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Warning("Vehicle ID is out of sync. Drop prefab again or run ID repair.");

            return ValidationResult.Success();
        }

        protected override void OnConfigChanged(VehicleConfig config)
        {
            if (config == null) return;

            config.EnsureClassificationDefaults();

            _isUpdatingUI = true;
            _vehicleNameField.value = string.IsNullOrEmpty(config.vehicleName)
                ? (config.prefabReference != null ? config.prefabReference.name : "")
                : config.vehicleName;
            _manufacturerField.value = config.authorName ?? "";
            RebuildClassificationUI();
            _isUpdatingUI = false;

            _vehicleIdField.value = config.id ?? string.Empty;
            _prefabGuidField.value = config.prefabGuid ?? string.Empty;

            if (string.IsNullOrWhiteSpace(config.prefabGuid))
                UpdateIdentityStatus("Missing prefab GUID", Color.red);
            else if (!string.Equals(
                config.id,
                VehicleConfig.ComputeDeterministicIdFromPrefabGuid(config.prefabGuid),
                StringComparison.OrdinalIgnoreCase))
            {
                UpdateIdentityStatus("Out of sync (run ID repair)", new Color(1f, 0.8f, 0f));
            }
            else
                UpdateIdentityStatus("Synced", Color.green);
        }

        protected override void OnPrefabChanged(GameObject prefab)
        {
            if (prefab != null)
                _vehicleNameField.value = prefab.name;
        }

        protected override void OnModuleActivated()
        {
            if (_context?.CurrentConfig != null)
                OnConfigChanged(_context.CurrentConfig);
            else
                UpdateIdentityStatus("Waiting for prefab", Color.yellow);
        }

        public override void OnModuleGUI() { }
    }
}
