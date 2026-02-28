using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UVS.Editor.Core;
using UVS.Editor.Modules;

namespace UVS.Editor
{
    public class VehicleEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Vehicle Editor/Open Vehicle Editor")]
        public static void ShowWindow() => GetWindow<VehicleEditorWindow>("Vehicle Editor");

        [MenuItem("Tools/Open UVS Vehicle Editor")]
        public static void ShowWindowShortcut() => ShowWindow();

        // Systems
        private VehicleEditorModuleRegistry _moduleRegistry;
        private VehicleEditorContext _context;
        private EnhancedEditorConsole _console;
        private VehiclePreviewManager _previewManager;

        // UI
        private VisualElement _tabStrip;
        private VisualElement _contentArea;
        private ScrollView _consoleArea;
        private IMGUIContainer _previewContainer;

        // State
        private IVehicleEditorModule _activeModule;
        private readonly Dictionary<string, VisualElement> _moduleUI = new();
        private readonly Dictionary<string, Button> _tabButtons = new();

        // Preview toggles
        private bool _showWheelGizmos = true;
        private bool _showColliderGizmos = true;
        private bool _showSuspensionGizmos = true;
        private bool _showSeatGizmos = true;
        private bool _topDownPreview = false;

        private void OnEnable()
        {
            try
            {
                CreateUI();
                InitializeSystems();
                LoadModules();
                SetupPreviewCallbacks();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize Vehicle Editor Window: {ex}");
            }
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void InitializeSystems()
        {
            VehicleTaxonomyService.GetOrCreateDefault(createIfMissing: true);
            VehicleTaxonomyService.EnsureCompanionProfiles();

            _moduleRegistry = new VehicleEditorModuleRegistry();
            _moduleRegistry.Initialize();

            _context = new VehicleEditorContext();
            _context.RequestNewVehicle = CreateNewVehicle;
            _context.RequestLoadVehicle = LoadVehicle;
            _context.RequestHelp = ShowHelp;
            LoadRegistryAndConfigs();

            _previewManager = new VehiclePreviewManager();

            _console = new EnhancedEditorConsole(_consoleArea);
            _context.Console = _console;

            _context.OnLogMessage += _console.LogInfo;
            _context.OnLogError += _console.LogError;
            _context.OnValidationRequired += ValidateAllModules;

            _context.OnConfigChanged += OnContextConfigChanged;
            _context.OnPrefabChanged += OnPrefabChanged;
            _context.OnConfigChanged += OnConfigChangedForPreview;
        }

        private void OnPrefabChanged(GameObject prefab)
        {
            if (prefab != null)
            {
                SetPreviewVehicle(prefab);
                UpdateSeatPreview();
            }
            else
            {
                _previewManager?.SetVehicle(null);
                _previewContainer?.MarkDirtyRepaint();
            }
        }

        private void OnConfigChangedForPreview(VehicleConfig config)
        {
            if (config != null && config.prefabReference != null)
            {
                SetPreviewVehicle(config.prefabReference);
                UpdateSeatPreview();
            }
        }

        private void LoadRegistryAndConfigs()
        {
            try
            {
                var guids = AssetDatabase.FindAssets("t:VehicleIDRegistry");
                if (guids.Length > 0)
                    _context.Registry = AssetDatabase.LoadAssetAtPath<VehicleIDRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
                else
                {
                    _context.Registry = ScriptableObject.CreateInstance<VehicleIDRegistry>();
                    VehicleIdIndexService.EnsureDataFolder();
                    AssetDatabase.CreateAsset(_context.Registry, VehicleIdIndexService.DataFolder + "/VehicleIDRegistry.asset");
                    AssetDatabase.SaveAssets();
                }

                if (_context.Registry != null)
                {
                    _context.Registry.RebuildGuidIndexAndRepair(
                        rekeyAll: false,
                        exportMigrationMap: false,
                        quarantineDuplicates: true);
                }

                _context.GuidToConfigMap.Clear();
                var configGuids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { "Assets/VehicleConfigs" });
                foreach (var cfgGuid in configGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(cfgGuid);
                    var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.prefabGuid))
                        _context.GuidToConfigMap[cfg.prefabGuid] = cfg;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load registry and configs: {ex}");
            }
        }

        private void CreateUI()
        {
            var root = rootVisualElement;
            if (root == null) return;
            root.Clear();
            root.AddToClassList("uvs-root");

            var ussPath = "Assets/Editor/UVS/VehicleEditorWindow.uss";
            if (File.Exists(ussPath))
            {
                var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (ss != null) root.styleSheets.Add(ss);
            }

            var main = new VisualElement { style = { flexDirection = FlexDirection.Column, height = new StyleLength(Length.Percent(100)) } };
            main.AddToClassList("uvs-main");
            root.Add(main);

            CreateHeader(main);

            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            content.AddToClassList("uvs-content");
            main.Add(content);

            var left = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1, minWidth = 400 } };
            left.AddToClassList("uvs-left");
            content.Add(left);

            _tabStrip = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            _tabStrip.AddToClassList("uvs-tabs");
            left.Add(_tabStrip);

            _contentArea = new VisualElement { style = { flexGrow = 1 } };
            _contentArea.AddToClassList("uvs-panel");
            left.Add(_contentArea);

            var right = new VisualElement { style = { width = 350, flexDirection = FlexDirection.Column } };
            right.AddToClassList("uvs-right");
            content.Add(right);

            _previewContainer = new IMGUIContainer { style = { height = 220 } };
            _previewContainer.AddToClassList("uvs-preview");
            right.Add(_previewContainer);

            _consoleArea = new ScrollView { style = { flexGrow = 1 } };
            _consoleArea.AddToClassList("uvs-console");
            right.Add(_consoleArea);

            var consoleControls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            consoleControls.AddToClassList("uvs-console-controls");
            var clearBtn = new Button(() => _console?.Clear()) { text = "Clear" };
            var exportBtn = new Button(ExportLogs) { text = "Export" };
            consoleControls.Add(clearBtn);
            consoleControls.Add(exportBtn);
            right.Add(consoleControls);
        }

        private void CreateHeader(VisualElement parent)
        {
            var header = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8,
                    flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center
                }
            };
            header.AddToClassList("uvs-header");
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var title = new Label("Vehicle Editor") { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, color = Color.white, marginRight = 12 } };
            title.AddToClassList("uvs-title");
            left.Add(title);

            var newBtn = new Button(CreateNewVehicle) { text = "New Vehicle", style = { marginRight = 6 } };
            var loadBtn = new Button(LoadVehicle) { text = "Load Vehicle", style = { marginRight = 6 } };
            var helpBtn = new Button(ShowHelp) { text = "Help" };
            newBtn.AddToClassList("uvs-header-button");
            loadBtn.AddToClassList("uvs-header-button");
            helpBtn.AddToClassList("uvs-header-button");
            left.Add(newBtn);
            left.Add(loadBtn);
            left.Add(helpBtn);

            header.Add(left);

            var status = new Label("Ready") { name = "statusLabel", style = { color = Color.green } };
            status.AddToClassList("uvs-status");
            header.Add(status);

            parent.Add(header);
        }

        private void SetupPreviewCallbacks()
        {
            if (_previewContainer == null) return;

            _previewContainer.onGUIHandler = () =>
            {
                _previewManager.Update();
                var preview = _previewManager.Current;

                if (preview != null)
                {
                    preview.RenderPreview(_previewContainer.contentRect);
                    DrawPreviewControls();
                }
                else
                {
                    EditorGUI.DrawRect(_previewContainer.contentRect, new Color(0.1f, 0.1f, 0.1f));
                    EditorGUI.LabelField(new Rect(10, 10, 300, 20), "3D Preview (No renderer active)");
                }
            };
        }

        private void DrawPreviewControls()
        {
            if (_previewContainer == null) return;
            Rect controlsRect = new(_previewContainer.contentRect.x + 8, _previewContainer.contentRect.y + 8, 220, 150);
            GUILayout.BeginArea(controlsRect);
            GUILayout.BeginVertical("Box");
            GUILayout.Label("3D Preview Controls", EditorStyles.boldLabel);

            bool newW = GUILayout.Toggle(_showWheelGizmos, "Show Wheel Gizmos");
            bool newC = GUILayout.Toggle(_showColliderGizmos, "Show Collider Gizmos");
            bool newS = GUILayout.Toggle(_showSuspensionGizmos, "Show Suspension Gizmos");
            bool newSeats = GUILayout.Toggle(_showSeatGizmos, "Show Seat Gizmos");
            bool newTopDown = GUILayout.Toggle(_topDownPreview, "Top-Down View");

            if (newW != _showWheelGizmos)
            {
                _showWheelGizmos = newW;
                _previewManager.ToggleGizmo("wheels", newW);
            }
            if (newC != _showColliderGizmos)
            {
                _showColliderGizmos = newC;
                _previewManager.ToggleGizmo("colliders", newC);
            }
            if (newS != _showSuspensionGizmos)
            {
                _showSuspensionGizmos = newS;
                _previewManager.ToggleGizmo("suspension", newS);
            }
            if (newSeats != _showSeatGizmos)
            {
                _showSeatGizmos = newSeats;
                _previewManager.ToggleGizmo("seats", newSeats);
            }
            if (newTopDown != _topDownPreview)
            {
                _topDownPreview = newTopDown;
                if (_previewManager.Current is ISeatPreview seatPreview)
                    seatPreview.SetTopDown(_topDownPreview);
            }

            GUILayout.Space(6);
            GUILayout.Label("Pipeline", EditorStyles.label);

            VehiclePreviewManager.Mode mode = _previewManager.mode;
            var newMode = (VehiclePreviewManager.Mode)GUILayout.Toolbar((int)mode, new[] { "Auto", "Built-in", "URP", "HDRP" });

            if (newMode != mode)
            {
                _previewManager.SetMode(newMode);
                UpdateSeatPreview();
            }

            if (!_previewManager.IsModeSupported(newMode) &&
                (newMode == VehiclePreviewManager.Mode.URP || newMode == VehiclePreviewManager.Mode.HDRP))
            {
                GUILayout.Label($"{newMode} preview package not installed. Falling back automatically.", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Reset Camera"))
                _previewManager.SetVehicle(_context?.SelectedPrefab);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void LoadModules()
        {
            if (_moduleRegistry == null) return;

            try
            {
                _moduleRegistry.InitializeModules(_context);

                foreach (var module in _moduleRegistry.Modules)
                {
                    var ui = module.CreateUI();
                    _moduleUI[module.ModuleId] = ui;

                    var btn = new Button(() => ActivateModule(module.ModuleId))
                    {
                        text = module.DisplayName,
                        name = $"{module.ModuleId}Tab"
                    };
                    btn.AddToClassList("uvs-tab");

                    bool canShow = CanShowModule(module, _context.CurrentConfig);
                    btn.style.display = canShow ? DisplayStyle.Flex : DisplayStyle.None;

                    _tabStrip.Add(btn);
                    _tabButtons[module.ModuleId] = btn;
                }

                var firstVisibleModule = _moduleRegistry.Modules
                    .FirstOrDefault(m => CanShowModule(m, _context.CurrentConfig));

                if (firstVisibleModule != null)
                    ActivateModule(firstVisibleModule.ModuleId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LoadModules failed: {ex}");
            }
        }

        private void ActivateModule(string id)
        {
            _activeModule?.OnDeactivate();
            var module = _moduleRegistry?.GetModule<IVehicleEditorModule>(id);
            if (module == null) return;
            if (module.RequiresVehicle && !HasValidVehicle())
            {
                _console?.LogWarning($"Cannot activate {module.DisplayName} - no vehicle");
                return;
            }

            _contentArea?.Clear();
            if (_moduleUI.TryGetValue(id, out var ui))
                _contentArea.Add(ui);

            module.OnActivate();
            _activeModule = module;
            UpdateTabButtons();
            _console?.LogInfo($"Activated module: {module.DisplayName}");
        }

        private void UpdateTabButtons()
        {
            foreach (var kv in _tabButtons)
            {
                var module = _moduleRegistry?.GetModule<IVehicleEditorModule>(kv.Key);
                if (module == null) continue;

                bool isActive = _activeModule != null && _activeModule.ModuleId == kv.Key;
                kv.Value.EnableInClassList("tab-active", isActive);

                bool canShow = CanShowModule(module, _context?.CurrentConfig);
                kv.Value.style.display = canShow ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private bool CanShowModule(IVehicleEditorModule module, VehicleConfig config)
        {
            if (module == null) return false;
            if (!module.CanActivateWithConfig(config)) return false;
            if (module.RequiresVehicle && !HasValidVehicle()) return false;
            return ModuleCompatibilityService.IsModuleAllowed(module, config);
        }

        private bool HasValidVehicle() => _context?.CurrentConfig != null && _context.SelectedPrefab != null;

        private void ValidateAllModules()
        {
            if (_moduleRegistry == null) return;
            var results = new List<ValidationResult>();
            foreach (var module in _moduleRegistry.Modules)
            {
                try
                {
                    if (!CanShowModule(module, _context?.CurrentConfig))
                        continue;
                    var r = module.Validate();
                    results.Add(r);
                    if (!r.IsValid) _console?.LogError($"Module {module.DisplayName}: {r.ErrorMessage}");
                    else if (!string.IsNullOrEmpty(r.ErrorMessage))
                    {
                        if (r.Severity == ValidationSeverity.Warning) _console?.LogWarning($"Module {module.DisplayName}: {r.ErrorMessage}");
                        else _console?.LogInfo($"Module {module.DisplayName}: {r.ErrorMessage}");
                    }
                }
                catch (System.Exception ex)
                {
                    _console?.LogError($"Validation error {module.DisplayName}: {ex.Message}");
                }
            }

            var statusLabel = rootVisualElement?.Q<Label>("statusLabel");
            if (statusLabel != null)
            {
                bool hasErrors = results.Any(r => !r.IsValid);
                bool hasWarnings = results.Any(r => r.Severity == ValidationSeverity.Warning);
                if (hasErrors) { statusLabel.text = "Errors Found"; statusLabel.style.color = Color.red; }
                else if (hasWarnings) { statusLabel.text = "Warnings"; statusLabel.style.color = Color.yellow; }
                else { statusLabel.text = "Ready"; statusLabel.style.color = Color.green; }
            }
        }

        private void ExportLogs()
        {
            string path = EditorUtility.SaveFilePanel("Export Logs", Application.dataPath, "vehicle_editor_logs", "txt");
            if (!string.IsNullOrEmpty(path)) _console?.ExportLogs(path);
        }

        private void Cleanup()
        {
            try
            {
                _previewManager?.Cleanup();

                if (_context != null)
                {
                    _context.OnConfigChanged -= OnConfigChangedForPreview;
                    _context.OnPrefabChanged -= OnPrefabChanged;
                    _context.OnConfigChanged -= OnContextConfigChanged;
                    _context.RequestNewVehicle = null;
                    _context.RequestLoadVehicle = null;
                    _context.RequestHelp = null;
                }

                if (_moduleRegistry != null)
                {
                    foreach (var m in _moduleRegistry.Modules)
                    {
                        try { m.OnSave(); } catch (System.Exception ex) { Debug.LogError($"Save failed for {m.ModuleId}: {ex.Message}"); }
                    }
                    _moduleRegistry.CleanupModules();
                }
            }
            catch (System.Exception ex) { Debug.LogError($"Cleanup error: {ex}"); }
        }

        private void CreateNewVehicle()
        {
            try
            {
                _console?.LogInfo("Creating new vehicle...");
                var newConfig = ScriptableObject.CreateInstance<VehicleConfig>();
                newConfig.id = string.Empty;
                newConfig.vehicleName = "New Vehicle";
                newConfig.prefabGuid = string.Empty;
                newConfig.EnsureClassificationDefaults();

                _context.CurrentConfig = newConfig;
                _context.SelectedPrefab = null;

                string path = EditorUtility.SaveFilePanelInProject("Save Vehicle Config", "VehicleConfig.asset", "asset", "Save the vehicle configuration");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(newConfig, path);
                    AssetDatabase.SaveAssets();
                    _context.NotifyConfigChanged(newConfig);
                    _console?.LogSuccess("Created new vehicle config. Drop a prefab in Info to assign deterministic ID.");
                    UpdateTabButtons();
                    RefreshPreview();
                }
                else _console?.LogWarning("New vehicle creation cancelled.");
            }
            catch (System.Exception ex) { _console?.LogError($"Failed to create new vehicle: {ex.Message}"); }
        }

        private void LoadVehicle()
        {
            try
            {
                _console?.LogInfo("Loading vehicle configuration...");
                string path = EditorUtility.OpenFilePanel("Load Vehicle Config", "Assets", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath)) path = "Assets" + path[Application.dataPath.Length..];
                    var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                    if (cfg != null)
                    {
                        if (_context.Registry != null)
                        {
                            string oldId = cfg.id;
                            _context.Registry.EnsureDeterministicId(cfg);
                            _context.Registry.UpsertConfig(cfg);
                            if (!string.Equals(oldId, cfg.id, System.StringComparison.OrdinalIgnoreCase))
                                EditorUtility.SetDirty(cfg);
                        }

                        _context.CurrentConfig = cfg;
                        if (!string.IsNullOrEmpty(cfg.prefabGuid))
                        {
                            var prefabPath = AssetDatabase.GUIDToAssetPath(cfg.prefabGuid);
                            _context.SelectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            SetPreviewVehicle(_context.SelectedPrefab);
                            _context.GuidToConfigMap[cfg.prefabGuid] = cfg;
                        }
                        else
                        {
                            _context.SelectedPrefab = null;
                            SetPreviewVehicle(null);
                            _console?.LogWarning("Loaded config has no prefab GUID. Drop prefab in Info to sync deterministic ID.");
                        }

                        AssetDatabase.SaveAssets();
                        _context.NotifyConfigChanged(cfg);
                        _console?.LogSuccess($"Loaded vehicle: {cfg.id}");
                        UpdateTabButtons();
                        ValidateAllModules();
                    }
                    else _console?.LogError("Failed to load vehicle configuration - invalid asset type");
                }
                else _console?.LogInfo("Vehicle load cancelled.");
            }
            catch (System.Exception ex) { _console?.LogError($"Failed to load vehicle: {ex.Message}"); }
        }

        private void ShowHelp()
        {
            EditorUtility.DisplayDialog("Vehicle Editor Help",
                "Vehicle Editor Help\n\n• New Vehicle\n• Load Vehicle\n• Tabs for modules\n• Preview: 3D view with gizmos\n• Console: logs and validation",
                "OK");
        }

        public void SetPreviewVehicle(GameObject vehicle)
        {
            _previewManager.SetVehicle(vehicle);
            UpdateSeatPreview();
            _previewContainer?.MarkDirtyRepaint();
        }

        public void RefreshPreview() => _previewContainer?.MarkDirtyRepaint();

        private void OnContextConfigChanged(VehicleConfig config)
        {
            UpdateTabButtons();
            UpdateSeatPreview();

            if (_activeModule != null && !_activeModule.CanActivateWithConfig(config))
            {
                var firstVisible = _moduleRegistry?.Modules
                    .FirstOrDefault(m => CanShowModule(m, config));

                if (firstVisible != null)
                {
                    ActivateModule(firstVisible.ModuleId);
                }
            }
        }

        private void UpdateSeatPreview()
        {
            if (_previewManager?.Current is not ISeatPreview seatPreview)
                return;

            seatPreview.SetSeatData(_context?.CurrentConfig, OnSeatChangedFromPreview);
            seatPreview.SetTopDown(_topDownPreview);
        }

        private void OnSeatChangedFromPreview(int index, Vector3 localPosition, Vector3 localEuler)
        {
            if (_context?.CurrentConfig == null) return;
            if (_context.CurrentConfig.seats == null) return;
            if (index < 0 || index >= _context.CurrentConfig.seats.Count) return;

            var seat = _context.CurrentConfig.seats[index];
            seat.localPosition = localPosition;
            seat.localEuler = localEuler;
            EditorUtility.SetDirty(_context.CurrentConfig);
            _previewContainer?.MarkDirtyRepaint();
        }
    }
}






