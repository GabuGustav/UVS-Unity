using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UVS.Editor.Modules.Specialized;

namespace UVS.Editor.Core
{
    /// <summary>
    /// Registry for managing vehicle editor modules
    /// </summary>
    public class VehicleEditorModuleRegistry
    {
        private readonly Dictionary<string, IVehicleEditorModule> _modules = new Dictionary<string, IVehicleEditorModule>();
        private readonly List<IVehicleEditorModule> _sortedModules = new List<IVehicleEditorModule>();
        private bool _isInitialized = false;
        
        public IReadOnlyList<IVehicleEditorModule> Modules => _sortedModules.AsReadOnly();
        
        /// <summary>
        /// Initialize the registry and discover all available modules
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            DiscoverModules();
            SortModules();
            _isInitialized = true;
        }
        
        /// <summary>
        /// Register a module manually
        /// </summary>
        public void RegisterModule(IVehicleEditorModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
                
            if (string.IsNullOrEmpty(module.ModuleId))
                throw new ArgumentException("Module ID cannot be null or empty", nameof(module));
                
            if (_modules.ContainsKey(module.ModuleId))
            {
                UnityEngine.Debug.LogWarning($"Module with ID '{module.ModuleId}' is already registered. Replacing existing module.");
            }
            
            _modules[module.ModuleId] = module;
            SortModules();
        }
        
        /// <summary>
        /// Unregister a module
        /// </summary>
        public void UnregisterModule(string moduleId)
        {
            if (_modules.TryGetValue(moduleId, out var module))
            {
                module.Cleanup();
                _modules.Remove(moduleId);
                SortModules();
            }
        }
        
        /// <summary>
        /// Get a module by ID
        /// </summary>
        public T GetModule<T>(string moduleId) where T : class, IVehicleEditorModule
        {
            if (_modules.TryGetValue(moduleId, out var module))
            {
                return module as T;
            }
            return null;
        }
        
        /// <summary>
        /// Get all modules of a specific type
        /// </summary>
        public IEnumerable<T> GetModules<T>() where T : class, IVehicleEditorModule
        {
            return _sortedModules.OfType<T>();
        }
        
        /// <summary>
        /// Get modules that don't require a vehicle to be loaded
        /// </summary>
        public IEnumerable<IVehicleEditorModule> GetModulesWithoutVehicleRequirement()
        {
            return _sortedModules.Where(m => !m.RequiresVehicle);
        }
        
        /// <summary>
        /// Get modules that require a vehicle to be loaded
        /// </summary>
        public IEnumerable<IVehicleEditorModule> GetModulesWithVehicleRequirement()
        {
            return _sortedModules.Where(m => m.RequiresVehicle);
        }
        
        /// <summary>
        /// Initialize all modules with the given context
        /// </summary>
        public void InitializeModules(VehicleEditorContext context)
        {
            foreach (var module in _sortedModules)
            {
                try
                {
                    module.Initialize(context);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize module '{module.ModuleId}': {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Cleanup all modules
        /// </summary>
        public void CleanupModules()
        {
            foreach (var module in _sortedModules)
            {
                try
                {
                    module.Cleanup();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to cleanup module '{module.ModuleId}': {ex.Message}");
                }
            }
        }

        private void DiscoverModules()
        {
            // Find all types that implement IVehicleEditorModule
            var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IVehicleEditorModule).IsAssignableFrom(type) &&
                              !type.IsAbstract &&
                              !type.IsInterface &&
                              type != typeof(VehicleEditorModuleBase))
                .ToList();

            foreach (var type in moduleTypes)
            {
                try
                {
                    var module = Activator.CreateInstance(type) as IVehicleEditorModule;
                    if (module != null)
                    {
                        RegisterModule(module);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to create instance of module type '{type.Name}': {ex.Message}");
                }
            }

            RegisterModule(new TankModule());
            RegisterModule(new VTOLModule());
            RegisterModule(new ConstructionModule());
            RegisterModule(new DeformationModule());
        }
        
        private void SortModules()
        {
            _sortedModules.Clear();
            _sortedModules.AddRange(_modules.Values.OrderBy(m => m.Priority).ThenBy(m => m.DisplayName));
        }
    }
}