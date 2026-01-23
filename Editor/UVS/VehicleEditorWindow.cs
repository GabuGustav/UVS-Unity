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
        [MenuItem("Tools/Vehicle Editor")]
        public static void ShowWindow() => GetWindow<VehicleEditorWindow>("Vehicle Editor");

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
            _moduleRegistry = new VehicleEditorModuleRegistry();
            _moduleRegistry.Initialize();

            _context = new VehicleEditorContext();
            LoadRegistryAndConfigs();

            // preview manager
            _previewManager = new VehiclePreviewManager();

            // console
            _console = new EnhancedEditorConsole(_consoleArea);
            _context.Console = _console;

            // subscribe
            _context.OnLogMessage += _console.LogInfo;
            _context.OnLogError += _console.LogError;
            _context.OnValidationRequired += ValidateAllModules;
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
                    Directory.CreateDirectory("Assets/Editor/UVSVehicleSystem/Data");
                    AssetDatabase.CreateAsset(_context.Registry, "Assets/Editor/UVSVehicleSystem/Data/VehicleIDRegistry.asset");
                    AssetDatabase.SaveAssets();
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

            // optional USS
            var ussPath = "Assets/Editor/UVS/VehicleEditorWindow.uss";
            if (File.Exists(ussPath))
            {
                var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (ss != null) root.styleSheets.Add(ss);
            }

            var main = new VisualElement { style = { flexDirection = FlexDirection.Column, height = new StyleLength(Length.Percent(100)) } };
            root.Add(main);

            CreateHeader(main);

            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            main.Add(content);

            var left = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1, minWidth = 400 } };
            content.Add(left);

            _tabStrip = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            left.Add(_tabStrip);

            _contentArea = new VisualElement { style = { flexGrow = 1 } };
            left.Add(_contentArea);

            var right = new VisualElement { style = { width = 350, flexDirection = FlexDirection.Column } };
            content.Add(right);

            _previewContainer = new IMGUIContainer { style = { height = 220 } };
            right.Add(_previewContainer);

            _consoleArea = new ScrollView { style = { flexGrow = 1 } };
            right.Add(_consoleArea);

            var consoleControls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
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

            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var title = new Label("Vehicle Editor") { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, color = Color.white, marginRight = 12 } };
            left.Add(title);

            var newBtn = new Button(CreateNewVehicle) { text = "New Vehicle", style = { marginRight = 6 } };
            var loadBtn = new Button(LoadVehicle) { text = "Load Vehicle", style = { marginRight = 6 } };
            var helpBtn = new Button(ShowHelp) { text = "Help" };

            left.Add(newBtn);
            left.Add(loadBtn);
            left.Add(helpBtn);

            header.Add(left);

            var status = new Label("Ready") { name = "statusLabel", style = { color = Color.green } };
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
            Rect controlsRect = new Rect(_previewContainer.contentRect.x + 8, _previewContainer.contentRect.y + 8, 220, 120);
            GUILayout.BeginArea(controlsRect);
            GUILayout.BeginVertical("Box");
            GUILayout.Label("3D Preview Controls", EditorStyles.boldLabel);

            bool newW = GUILayout.Toggle(_showWheelGizmos, "Show Wheel Gizmos");
            bool newC = GUILayout.Toggle(_showColliderGizmos, "Show Collider Gizmos");
            bool newS = GUILayout.Toggle(_showSuspensionGizmos, "Show Suspension Gizmos");

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

            GUILayout.Space(6);
            GUILayout.Label("Pipeline", EditorStyles.label);

            VehiclePreviewManager.Mode mode = _previewManager.mode;
            var newMode = (VehiclePreviewManager.Mode)GUILayout.Toolbar((int)mode, new[] { "Auto", "Built-in", "URP" });

            if (newMode != mode)
                _previewManager.SetMode(newMode);

            if (GUILayout.Button("Reset Camera"))
                _previewManager.SetVehicle(_context?.SelectedPrefab); // This reframes the camera

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

                    // Base check: needs vehicle loaded
                    bool canActivate = !module.RequiresVehicle || HasValidVehicle();

                    btn.SetEnabled(canActivate);

                    if (!canActivate && module.RequiresVehicle && !HasValidVehicle())
                    {
                        btn.tooltip = "Requires a vehicle to be loaded";
                    }

                    _tabStrip.Add(btn);
                    _tabButtons[module.ModuleId] = btn;
                }

                if (_moduleRegistry.Modules.Count > 0)
                    ActivateModule(_moduleRegistry.Modules[0].ModuleId);
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
            if (module.RequiresVehicle && !HasValidVehicle()) { _console?.LogWarning($"Cannot activate {module.DisplayName} - no vehicle"); return; }

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
                bool active = _activeModule != null && _activeModule.ModuleId == kv.Key;
                kv.Value.EnableInClassList("tab-active", active);

                var module = _moduleRegistry?.GetModule<IVehicleEditorModule>(kv.Key);
                if (module != null)
                {
                    bool can = !module.RequiresVehicle || HasValidVehicle();
                    kv.Value.SetEnabled(can);
                }
            }
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
                newConfig.id = $"Vehicle_{System.Guid.NewGuid().ToString()[..8]}";
                newConfig.vehicleName = "New Vehicle";
                newConfig.prefabGuid = string.Empty;

                _context.CurrentConfig = newConfig;
                _context.SelectedPrefab = null;

                string path = EditorUtility.SaveFilePanelInProject("Save Vehicle Config", $"{newConfig.id}.asset", "asset", "Save the vehicle configuration");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(newConfig, path);
                    AssetDatabase.SaveAssets();
                    if (_context.Registry != null) _context.Registry.RegisterVehicle(newConfig.id, newConfig);
                    _context.NotifyConfigChanged(newConfig);
                    _console?.LogSuccess($"Created new vehicle: {newConfig.id}");
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
                        _context.CurrentConfig = cfg;
                        if (!string.IsNullOrEmpty(cfg.prefabGuid))
                        {
                            var prefabPath = AssetDatabase.GUIDToAssetPath(cfg.prefabGuid);
                            _context.SelectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            SetPreviewVehicle(_context.SelectedPrefab);
                        }
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
            _previewContainer?.MarkDirtyRepaint();
        }

        public void RefreshPreview() => _previewContainer?.MarkDirtyRepaint();
    }
}
