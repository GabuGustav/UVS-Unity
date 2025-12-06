using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using UVS.Editor.Core;

namespace UVS.Editor
{
    /// <summary>
    /// Modular Vehicle Editor Window using the new module system
    /// </summary>
    public class ModularVehicleEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Modular Vehicle Editor")]
        public static void ShowWindow() => GetWindow<ModularVehicleEditorWindow>("Modular Vehicle Editor");
        
        // Core systems
        private VehicleEditorModuleRegistry _moduleRegistry;
        private VehicleEditorContext _context;
        private EnhancedEditorConsole _console;
        
        // UI elements
        private VisualElement _tabStrip;
        private VisualElement _contentArea;
        private ScrollView _consoleArea;
        private IMGUIContainer _previewContainer;
        
        // State
        private IVehicleEditorModule _activeModule;
        private Dictionary<string, VisualElement> _moduleUI = new Dictionary<string, VisualElement>();
        
        private void OnEnable()
        {
            CreateUI(); // Create UI FIRST
            InitializeSystems(); // Then initialize systems that depend on UI
            LoadModules();
        }
        
        private void OnDisable()
        {
            Cleanup();
        }
        
        private void InitializeSystems()
        {
            // Initialize module registry
            _moduleRegistry = new VehicleEditorModuleRegistry();
            _moduleRegistry.Initialize();
            
            // Initialize context
            _context = new VehicleEditorContext();
            LoadRegistryAndConfigs();
            
            // Initialize console - NOW _consoleArea is not null
            _console = new EnhancedEditorConsole(_consoleArea);
            _context.Console = _console;
            
            // Subscribe to context events
            _context.OnLogMessage += _console.LogInfo;
            _context.OnLogError += _console.LogError;
            _context.OnValidationRequired += ValidateAllModules;

            // Log initialization message to verify console works
            _console.LogInfo("Modular Vehicle Editor initialized successfully!");
        }
        
        private void LoadRegistryAndConfigs()
        {
            // Load or create registry
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
            
            // Preload existing configs
            _context.GuidToConfigMap.Clear();
            foreach (var cfgGuid in AssetDatabase.FindAssets("t:VehicleConfig", new[] { "Assets/VehicleConfigs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(cfgGuid);
                var cfg = AssetDatabase.LoadAssetAtPath<VehicleConfig>(path);
                if (cfg != null && !string.IsNullOrEmpty(cfg.prefabGuid))
                {
                    _context.GuidToConfigMap[cfg.prefabGuid] = cfg;
                }
            }
        }
        
        private void CreateUI()
        {
            var root = rootVisualElement;
            root.Clear();
            
            // Load styles
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/UVS/ModularVehicleEditor.uss");
            if (uss != null) 
            {
                root.styleSheets.Add(uss);
            }
            else
            {
                Debug.LogWarning("ModularVehicleEditor.uss style sheet not found at Assets/Editor/UVS/ModularVehicleEditor.uss");
            }
            
            // Main container
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            root.Add(mainContainer);
            
            // Header
            CreateHeader(mainContainer);
            
            // Content area with tabs
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            mainContainer.Add(contentContainer);
            
            // Left panel (tabs and content)
            var leftPanel = new VisualElement();
            leftPanel.style.flexDirection = FlexDirection.Column;
            leftPanel.style.flexGrow = 1;
            leftPanel.style.minWidth = new StyleLength(400);
            contentContainer.Add(leftPanel);
            
            // Tab strip
            _tabStrip = new VisualElement();
            _tabStrip.style.flexDirection = FlexDirection.Row;
            _tabStrip.style.flexWrap = Wrap.Wrap;
            _tabStrip.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _tabStrip.style.paddingLeft = 5;
            _tabStrip.style.paddingRight = 5;
            _tabStrip.style.paddingTop = 5;
            _tabStrip.style.paddingBottom = 5;
            leftPanel.Add(_tabStrip);
            
            // Content area
            _contentArea = new VisualElement();
            _contentArea.style.flexGrow = 1;
            _contentArea.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            leftPanel.Add(_contentArea);
            
            // Right panel (preview and console)
            var rightPanel = new VisualElement();
            rightPanel.style.width = new StyleLength(300);
            rightPanel.style.flexDirection = FlexDirection.Column;
            contentContainer.Add(rightPanel);
            
            // Preview container
            _previewContainer = new IMGUIContainer();
            _previewContainer.style.height = new StyleLength(200);
            _previewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            rightPanel.Add(_previewContainer);
            
            // Console area
            _consoleArea = new ScrollView();
            _consoleArea.style.flexGrow = 1;
            _consoleArea.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            rightPanel.Add(_consoleArea);
            
            // Console controls
            var consoleControls = new VisualElement();
            consoleControls.style.flexDirection = FlexDirection.Row;
            consoleControls.style.paddingLeft = 5;
            consoleControls.style.paddingRight = 5;
            consoleControls.style.paddingTop = 5;
            consoleControls.style.paddingBottom = 5;
            
            var clearButton = new Button(() => _console?.Clear()) { text = "Clear" };
            var exportButton = new Button(ExportLogs) { text = "Export" };
            
            consoleControls.Add(clearButton);
            consoleControls.Add(exportButton);
            rightPanel.Add(consoleControls);
        }
        
        private void CreateHeader(VisualElement parent)
        {
            var header = new VisualElement();
            header.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 10;
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            
            var titleLabel = new Label("Modular Vehicle Editor");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = Color.white;
            header.Add(titleLabel);
            
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
        Debug.LogError("Module registry not initialized!");
        return;
    }

    // Initialize all modules with context
    _moduleRegistry.InitializeModules(_context);
    
    // Create UI for each module
    foreach (var module in _moduleRegistry.Modules)
    {
        try
        {
            var moduleUI = module.CreateUI();
            _moduleUI[module.ModuleId] = moduleUI;
            
            // Create tab button - FIXED: Store module in userData
            var tabButton = new Button(() => ActivateModule(module.ModuleId))
            {
                text = module.DisplayName
            };
            tabButton.name = $"{module.ModuleId}Tab";
            tabButton.AddToClassList("tab-button");
            tabButton.userData = module;  // ← THIS IS THE CRITICAL FIX
            
            // Enable/disable based on requirements
            bool canActivate = !module.RequiresVehicle || HasValidVehicle();
            tabButton.SetEnabled(canActivate);
            
            if (!canActivate)
            {
                tabButton.tooltip = "Requires a vehicle to be loaded";
            }
            
            _tabStrip.Add(tabButton);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create UI for module '{module.ModuleId}': {ex.Message}");
            _console?.LogError($"Failed to create UI for module '{module.ModuleId}': {ex.Message}");
        }
    }
    
    // Activate the first available module
    var firstModule = _moduleRegistry.Modules.Count > 0 ? _moduleRegistry.Modules[0] : null;
    if (firstModule != null)
    {
        ActivateModule(firstModule.ModuleId);
    }
    else
    {
        _console?.LogWarning("No modules were loaded!");
    }
}

        private void ActivateModule(string moduleId)
        {
            // Deactivate current module
            if (_activeModule != null)
            {
                _activeModule.OnDeactivate();
            }

            // Get the module
            var module = _moduleRegistry.GetModule<IVehicleEditorModule>(moduleId);
            if (module == null)
            {
                Debug.LogError($"Module '{moduleId}' not found");
                _console?.LogError($"Module '{moduleId}' not found");
                return;
            }

            // Check if module can be activated
            if (module.RequiresVehicle && !HasValidVehicle())
            {
                _console?.LogWarning($"Cannot activate module '{module.DisplayName}' - no vehicle loaded");
                return;
            }

            // Clear content area
            _contentArea.Clear();

            // Add module UI
            if (_moduleUI.TryGetValue(moduleId, out var moduleUI))
            {
                _contentArea.Add(moduleUI);
            }
            else
            {
                _console?.LogError($"No UI found for module '{moduleId}'");
                return;
            }

            // Activate module
            module.OnActivate();
            _activeModule = module;

            // Update tab buttons
            UpdateTabButtons();

            _console?.LogInfo($"Activated module: {module.DisplayName}");
        }
        
        private void UpdateTabButtons()
        {
            foreach (var button in _tabStrip.Query<Button>().ToList())
            {
                bool isActive = button.name == $"{_activeModule?.ModuleId}Tab";
                button.EnableInClassList("tab-active", isActive);
                
                // Update enabled state based on requirements
                if (button.userData is IVehicleEditorModule module)
                {
                    bool canActivate = !module.RequiresVehicle || HasValidVehicle();
                    button.SetEnabled(canActivate);
                }
            }
        }
        
        private bool HasValidVehicle()
        {
            return _context.CurrentConfig != null && _context.SelectedPrefab != null;
        }
        
        private void ValidateAllModules()
        {
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
            
            // Update status
            var statusLabel = rootVisualElement.Q<Label>("statusLabel");
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
            // Save all modules
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
                
                // Cleanup modules
                _moduleRegistry.CleanupModules();
            }
            
            // Cleanup preview
            // TODO: Cleanup preview if needed
        }
        
        private void OnGUI()
        {
            // Handle preview rendering
            if (_previewContainer != null)
            {
                // TODO: Implement preview rendering
                EditorGUI.LabelField(new Rect(10, 10, 280, 20), "3D Preview");
                EditorGUI.LabelField(new Rect(10, 30, 280, 20), "Preview functionality");
                EditorGUI.LabelField(new Rect(10, 50, 280, 20), "coming soon...");
            }
        }
    }
}