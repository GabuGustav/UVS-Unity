using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UVS.Editor.Core;
using UVS.Modules;

namespace UVS.Editor
{
    public class VehicleEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Vehicle Editor")]
        public static void ShowWindow() => GetWindow<VehicleEditorWindow>("Vehicle Editor");

        // Core modular systems
        private VehicleEditorModuleRegistry _moduleRegistry;
        private VehicleEditorContext _context;
        private EnhancedEditorConsole _console;
        private VehiclePreview3D _preview3D;

        // UI elements
        private VisualElement _tabStrip;
        private VisualElement _contentArea;
        private ScrollView _consoleArea;
        private IMGUIContainer _previewContainer;

        // State
        private IVehicleEditorModule _activeModule;
        private Dictionary<string, VisualElement> _moduleUI = new Dictionary<string, VisualElement>();
        private Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>();

        // Preview controls state
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
            try
            {
                _moduleRegistry = new VehicleEditorModuleRegistry();
                _moduleRegistry.Initialize();

                _context = new VehicleEditorContext();
                LoadRegistryAndConfigs();

                // Initialize 3D preview
                _preview3D = new VehiclePreview3D();

                // Initialize console
                _console = new EnhancedEditorConsole(_consoleArea);
                _context.Console = _console;

                // Subscribe to context events
                _context.OnLogMessage += _console.LogInfo;
                _context.OnLogError += _console.LogError;
                _context.OnValidationRequired += ValidateAllModules;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize systems: {ex}");
            }
        }

        private void LoadRegistryAndConfigs()
        {
            try
            {
                var guids = AssetDatabase.FindAssets("t:VehicleIDRegistry");
                if (guids.Length > 0)
                {
                    _context.Registry = AssetDatabase.LoadAssetAtPath<VehicleIDRegistry>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                else
                {
                    _context.Registry = ScriptableObject.CreateInstance<VehicleIDRegistry>();
                    Directory.CreateDirectory("Assets/VehicleSystem/Data");
                    AssetDatabase.CreateAsset(_context.Registry, "Assets/VehicleSystem/Data/VehicleIDRegistry.asset");
                    AssetDatabase.SaveAssets();
                }

                _context.GuidToConfigMap.Clear();
                var configGuids = AssetDatabase.FindAssets("t:VehicleConfig", new[] { "Assets/VehicleConfigs" });
                foreach (var cfgGuid in configGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(cfgGuid);
                    var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.prefabGuid))
                    {
                        _context.GuidToConfigMap[cfg.prefabGuid] = cfg;
                    }
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
            if (root == null)
            {
                Debug.LogError("Root visual element is null");
                return;
            }

            root.Clear();

            // Load stylesheet safely
            var ussPath = "Assets/Editor/UVS/VehicleEditorWindow.uss";
            if (File.Exists(ussPath))
            {
                var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (uss != null)
                {
                    root.styleSheets.Add(uss);
                }
            }

            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            root.Add(mainContainer);

            CreateHeader(mainContainer);

            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            mainContainer.Add(contentContainer);

            var leftPanel = new VisualElement();
            leftPanel.style.flexDirection = FlexDirection.Column;
            leftPanel.style.flexGrow = 1;
            leftPanel.style.minWidth = new StyleLength(400);
            contentContainer.Add(leftPanel);

            // Initialize _tabStrip before using it
            _tabStrip = new VisualElement();
            _tabStrip.style.flexDirection = FlexDirection.Row;
            _tabStrip.style.flexWrap = Wrap.Wrap;
            _tabStrip.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _tabStrip.style.paddingLeft = 5;
            _tabStrip.style.paddingRight = 5;
            _tabStrip.style.paddingTop = 5;
            _tabStrip.style.paddingBottom = 5;
            leftPanel.Add(_tabStrip);

            // Initialize _contentArea before using it
            _contentArea = new VisualElement();
            _contentArea.style.flexGrow = 1;
            _contentArea.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            leftPanel.Add(_contentArea);

            var rightPanel = new VisualElement();
            rightPanel.style.width = new StyleLength(350);
            rightPanel.style.flexDirection = FlexDirection.Column;
            contentContainer.Add(rightPanel);

            // Initialize _previewContainer before using it
            _previewContainer = new IMGUIContainer();
            _previewContainer.style.height = new StyleLength(200);
            _previewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            rightPanel.Add(_previewContainer);

            // Initialize _consoleArea before using it
            _consoleArea = new ScrollView();
            _consoleArea.style.flexGrow = 1;
            _consoleArea.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            rightPanel.Add(_consoleArea);
            var clearButton = new Button(() => _console?.Clear()) { text = "Clear" };
            var exportButton = new Button(ExportLogs) { text = "Export" };

            var consoleControls = new VisualElement();
            consoleControls.style.flexDirection = FlexDirection.Row;
            consoleControls.style.paddingLeft = 5;
            consoleControls.style.paddingRight = 5;
            consoleControls.style.paddingTop = 5;
            consoleControls.style.paddingBottom = 5;


            consoleControls.Add(clearButton);
            consoleControls.Add(exportButton);
            rightPanel.Add(consoleControls);
        }

        private void SetupPreviewCallbacks()
        {
            if (_previewContainer != null)
            {
                _previewContainer.onGUIHandler = () =>
                {
                    if (_preview3D != null && _previewContainer != null)
                    {
                        // Render the 3D preview
                        _preview3D.RenderPreview(_previewContainer.contentRect);

                        // Draw preview controls overlay
                        DrawPreviewControls();
                    }
                    else
                    {
                        // Fallback: Draw placeholder
                        EditorGUI.DrawRect(_previewContainer.contentRect, new Color(0.1f, 0.1f, 0.1f, 1f));
                        EditorGUI.LabelField(new Rect(10, 10, 330, 20), "3D Preview");
                        EditorGUI.LabelField(new Rect(10, 30, 330, 20), "Preview functionality");
                        EditorGUI.LabelField(new Rect(10, 50, 330, 20), "Drag a vehicle to preview");
                    }
                };
            }
        }

        private void DrawPreviewControls()
        {
            if (_previewContainer == null) return;

            Rect controlsRect = new Rect(_previewContainer.contentRect.x + 10,
                                       _previewContainer.contentRect.y + 10,
                                       200, 120);

            GUILayout.BeginArea(controlsRect);
            GUILayout.BeginVertical("Box", GUILayout.Width(200));

            GUILayout.Label("3D Preview Controls", EditorStyles.boldLabel);

            // Update gizmo states and get new values
            bool newWheelGizmos = GUILayout.Toggle(_showWheelGizmos, "Show Wheel Gizmos");
            bool newColliderGizmos = GUILayout.Toggle(_showColliderGizmos, "Show Collider Gizmos");
            bool newSuspensionGizmos = GUILayout.Toggle(_showSuspensionGizmos, "Show Suspension Gizmos");

            // Update preview if any toggle changed
            if (newWheelGizmos != _showWheelGizmos ||
                newColliderGizmos != _showColliderGizmos ||
                newSuspensionGizmos != _showSuspensionGizmos)
            {
                _showWheelGizmos = newWheelGizmos;
                _showColliderGizmos = newColliderGizmos;
                _showSuspensionGizmos = newSuspensionGizmos;

                if (_preview3D != null)
                {
                    _preview3D.ToggleGizmo("wheels", _showWheelGizmos);
                    _preview3D.ToggleGizmo("colliders", _showColliderGizmos);
                    _preview3D.ToggleGizmo("suspension", _showSuspensionGizmos);
                }
            }

            if (GUILayout.Button("Reset Camera"))
            {
                // Reset camera to default position
                // You can implement camera reset logic in VehiclePreview3D
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void CreateHeader(VisualElement parent)
        {
            if (parent == null) return;

            var header = new VisualElement();
            header.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 10;
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;

            // Left section with title and buttons
            var leftSection = new VisualElement();
            leftSection.style.flexDirection = FlexDirection.Row;
            leftSection.style.alignItems = Align.Center;

            var titleLabel = new Label("Vehicle Editor");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = Color.white;
            titleLabel.style.marginRight = 20;
            leftSection.Add(titleLabel);

            // Add the missing action buttons
            var newVehicleButton = new Button(CreateNewVehicle) { text = "New Vehicle" };
            newVehicleButton.style.marginRight = 10;
            leftSection.Add(newVehicleButton);

            var loadVehicleButton = new Button(LoadVehicle) { text = "Load Vehicle" };
            loadVehicleButton.style.marginRight = 10;
            leftSection.Add(loadVehicleButton);

            var helpButton = new Button(ShowHelp) { text = "Help" };
            leftSection.Add(helpButton);

            header.Add(leftSection);

            var statusLabel = new Label("Ready");
            statusLabel.name = "statusLabel";
            statusLabel.style.color = Color.green;
            header.Add(statusLabel);

            parent.Add(header);
        }

        private void LoadModules()
        {
            if (_moduleRegistry == null)
            {
                Debug.LogError("Module registry is not initialized");
                return;
            }

            try
            {
                _moduleRegistry.InitializeModules(_context);

                foreach (var module in _moduleRegistry.Modules)
                {
                    try
                    {
                        var moduleUI = module.CreateUI();
                        _moduleUI[module.ModuleId] = moduleUI;

                        var tabButton = new Button(() => ActivateModule(module.ModuleId))
                        {
                            text = module.DisplayName
                        };
                        tabButton.name = $"{module.ModuleId}Tab";
                        tabButton.AddToClassList("tab-button");

                        bool canActivate = !module.RequiresVehicle || HasValidVehicle();
                        tabButton.SetEnabled(canActivate);

                        if (!canActivate)
                        {
                            tabButton.tooltip = "Requires a vehicle to be loaded";
                        }

                        _tabStrip.Add(tabButton);
                        _tabButtons[module.ModuleId] = tabButton;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to create UI for module '{module.ModuleId}': {ex.Message}");
                    }
                }

                var firstModule = _moduleRegistry.Modules.Count > 0 ? _moduleRegistry.Modules[0] : null;
                if (firstModule != null)
                {
                    ActivateModule(firstModule.ModuleId);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load modules: {ex}");
            }
        }

        private void ActivateModule(string moduleId)
        {
            if (_activeModule != null)
            {
                _activeModule.OnDeactivate();
            }

            var module = _moduleRegistry?.GetModule<IVehicleEditorModule>(moduleId);
            if (module == null)
            {
                Debug.LogError($"Module '{moduleId}' not found");
                return;
            }

            if (module.RequiresVehicle && !HasValidVehicle())
            {
                _console?.LogWarning($"Cannot activate module '{module.DisplayName}' - no vehicle loaded");
                return;
            }

            _contentArea?.Clear();

            if (_moduleUI.TryGetValue(moduleId, out var moduleUI))
            {
                _contentArea?.Add(moduleUI);
            }

            module.OnActivate();
            _activeModule = module;

            UpdateTabButtons();

            _console?.LogInfo($"Activated module: {module.DisplayName}");
        }

        private void UpdateTabButtons()
        {
            foreach (var kvp in _tabButtons)
            {
                bool isActive = _activeModule != null && _activeModule.ModuleId == kvp.Key;
                kvp.Value.EnableInClassList("tab-active", isActive);

                var module = _moduleRegistry?.GetModule<IVehicleEditorModule>(kvp.Key);
                if (module != null)
                {
                    bool canActivate = !module.RequiresVehicle || HasValidVehicle();
                    kvp.Value.SetEnabled(canActivate);
                }
            }
        }

        private bool HasValidVehicle()
        {
            return _context?.CurrentConfig != null && _context.SelectedPrefab != null;
        }

        private void ValidateAllModules()
        {
            if (_moduleRegistry == null) return;

            var validationResults = new List<ValidationResult>();

            foreach (var module in _moduleRegistry.Modules)
            {
                try
                {
                    var result = module.Validate();
                    validationResults.Add(result);

                    if (!result.IsValid)
                    {
                        _console?.LogError($"Module '{module.DisplayName}': {result.ErrorMessage}");
                    }
                    else if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (result.Severity == ValidationSeverity.Warning)
                            _console?.LogWarning($"Module '{module.DisplayName}': {result.ErrorMessage}");
                        else
                            _console?.LogInfo($"Module '{module.DisplayName}': {result.ErrorMessage}");
                    }
                }
                catch (System.Exception ex)
                {
                    _console?.LogError($"Validation error in module '{module.DisplayName}': {ex.Message}");
                }
            }

            var statusLabel = rootVisualElement?.Q<Label>("statusLabel");
            if (statusLabel != null)
            {
                bool hasErrors = validationResults.Any(r => !r.IsValid);
                bool hasWarnings = validationResults.Any(r => r.Severity == ValidationSeverity.Warning);

                if (hasErrors)
                {
                    statusLabel.text = "Errors Found";
                    statusLabel.style.color = Color.red;
                }
                else if (hasWarnings)
                {
                    statusLabel.text = "Warnings";
                    statusLabel.style.color = Color.yellow;
                }
                else
                {
                    statusLabel.text = "Ready";
                    statusLabel.style.color = Color.green;
                }
            }
        }

        private void ExportLogs()
        {
            string path = EditorUtility.SaveFilePanel("Export Logs", Application.dataPath, "vehicle_editor_logs", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                _console?.ExportLogs(path);
            }
        }

        private void Cleanup()
        {
            try
            {
                // Cleanup 3D preview
                _preview3D?.Cleanup();

                if (_moduleRegistry != null)
                {
                    foreach (var module in _moduleRegistry.Modules)
                    {
                        try
                        {
                            module.OnSave();
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Failed to save module '{module.ModuleId}': {ex.Message}");
                        }
                    }

                    _moduleRegistry.CleanupModules();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during cleanup: {ex}");
            }
        }

        // Public method to set the vehicle in the preview - call this from modules
        public void SetPreviewVehicle(GameObject vehicle)
        {
            _preview3D?.SetVehicle(vehicle);
            _previewContainer?.MarkDirtyRepaint(); // Force refresh
        }

        // Public method to refresh the preview
        public void RefreshPreview()
        {
            _previewContainer?.MarkDirtyRepaint();
        }

        // Remove the OnGUI method since we're using IMGUIContainer callbacks instead

        private void CreateNewVehicle()
        {
            try
            {
                _console?.LogInfo("Creating new vehicle...");

                // Create new vehicle configuration
                var newConfig = ScriptableObject.CreateInstance<VehicleConfig>();
                newConfig.id = $"Vehicle_{System.Guid.NewGuid().ToString().Substring(0, 8)}"; // Now this will work
                newConfig.vehicleName = "New Vehicle";
                newConfig.prefabGuid = string.Empty;

                // Set as current config
                _context.CurrentConfig = newConfig;
                _context.SelectedPrefab = null;

                // Create asset file
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Vehicle Config",
                    $"{newConfig.VehicleID}.asset",
                    "asset",
                    "Save the vehicle configuration");

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(newConfig, path);
                    AssetDatabase.SaveAssets();

                    // Add to registry - Now this will work
                    if (_context.Registry != null)
                    {
                        _context.Registry.RegisterVehicle(newConfig.VehicleID, newConfig);
                    }

                    _context.NotifyConfigChanged(newConfig);
                    _console?.LogSuccess($"Created new vehicle: {newConfig.VehicleID}");

                    // Update tabs since we now have a vehicle
                    UpdateTabButtons();

                    // Refresh preview if we have one
                    RefreshPreview();
                }
                else
                {
                    _console?.LogWarning("New vehicle creation cancelled.");
                }
            }
            catch (System.Exception ex)
            {
                _console?.LogError($"Failed to create new vehicle: {ex.Message}");
            }
        }

        private void LoadVehicle()
        {
            try
            {
                _console?.LogInfo("Loading vehicle configuration...");

                string path = EditorUtility.OpenFilePanel("Load Vehicle Config", "Assets", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert to project-relative path
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }

                    var config = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                    if (config != null)
                    {
                        _context.CurrentConfig = config;

                        // Load associated prefab if GUID exists
                        if (!string.IsNullOrEmpty(config.prefabGuid))
                        {
                            var prefabPath = AssetDatabase.GUIDToAssetPath(config.prefabGuid);
                            _context.SelectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                            // Update preview with the loaded vehicle
                            SetPreviewVehicle(_context.SelectedPrefab);
                        }

                        _context.NotifyConfigChanged(config);
                        _console?.LogSuccess($"Loaded vehicle: {config.VehicleID}");

                        // Update tabs
                        UpdateTabButtons();

                        // Validate all modules
                        ValidateAllModules();
                    }
                    else
                    {
                        _console?.LogError("Failed to load vehicle configuration - invalid asset type");
                    }
                }
                else
                {
                    _console?.LogInfo("Vehicle load cancelled.");
                }
            }
            catch (System.Exception ex)
            {
                _console?.LogError($"Failed to load vehicle: {ex.Message}");
            }
        }

        private void ShowHelp()
        {
            EditorUtility.DisplayDialog(
                "Vehicle Editor Help",
                "Vehicle Editor Help\n\n" +
                "• New Vehicle: Create a new vehicle configuration\n" +
                "• Load Vehicle: Load an existing vehicle configuration\n" +
                "• Tabs: Different modules for configuring various vehicle systems\n" +
                "• Preview: 3D view of your vehicle with gizmo controls\n" +
                "• Console: Log messages and validation results",
                "OK");
        }
    }
}