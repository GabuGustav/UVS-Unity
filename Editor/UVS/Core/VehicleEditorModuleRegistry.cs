using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UVS.Editor.Modules.Specialized;

namespace UVS.Editor.Core
{
    /// Registry for managing vehicle editor modules
    public class VehicleEditorModuleRegistry
    {
        private readonly Dictionary<string, IVehicleEditorModule> _modules = new();
        private readonly List<IVehicleEditorModule> _sortedModules = new();
        private bool _isInitialized = false;
        
        public IReadOnlyList<IVehicleEditorModule> Modules => _sortedModules.AsReadOnly();
        
        /// Initialize the registry and discover all available modules
        public void Initialize()
        {
            if (_isInitialized) return;
            
            DiscoverModules();
            SortModules();
            _isInitialized = true;
        }
        
        /// Register a module manually
        public void RegisterModule(IVehicleEditorModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
                
            if (string.IsNullOrEmpty(module.ModuleId))
                throw new ArgumentException("Module ID cannot be null or empty", nameof(module));
                
            if (_modules.ContainsKey(module.ModuleId))
            {
                Debug.LogWarning($"Module with ID '{module.ModuleId}' is already registered. Replacing existing module.");
            }
            
            _modules[module.ModuleId] = module;
            SortModules();
        }
        
        /// Unregister a module
        public void UnregisterModule(string moduleId)
        {
            if (_modules.TryGetValue(moduleId, out var module))
            {
                module.Cleanup();
                _modules.Remove(moduleId);
                SortModules();
            }
        }
        
        /// Get a module by ID
        public T GetModule<T>(string moduleId) where T : class, IVehicleEditorModule
        {
            if (_modules.TryGetValue(moduleId, out var module))
            {
                return module as T;
            }
            return null;
        }
        
        /// Get all modules of a specific type
        public IEnumerable<T> GetModules<T>() where T : class, IVehicleEditorModule
        {
            return _sortedModules.OfType<T>();
        }
        
        /// Get modules that don't require a vehicle to be loaded
        public IEnumerable<IVehicleEditorModule> GetModulesWithoutVehicleRequirement()
        {
            return _sortedModules.Where(m => !m.RequiresVehicle);
        }
        
        /// Get modules that require a vehicle to be loaded
        public IEnumerable<IVehicleEditorModule> GetModulesWithVehicleRequirement()
        {
            return _sortedModules.Where(m => m.RequiresVehicle);
        }

        //Get specialized modules
        public IEnumerable<IVehicleEditorModule> GetSpecializedModules()
        {
            return _sortedModules.Where(m => m.RequiresSpecializedCategory);
        }

        /// Get construction module
        /// </summary>
        public IVehicleEditorModule GetConstructionModule()
        {
            return _sortedModules.FirstOrDefault(m => m.IsConstructionModule);
        }

        //Get tank module
        public IVehicleEditorModule GetTankModule()
        {
            return _sortedModules.FirstOrDefault(m => m.IsTankModule);
        }

        //Get VTOL module   
        public IVehicleEditorModule GetVTOLModule()
        {
            return _sortedModules.FirstOrDefault(m => m.IsVTOLModule);
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
                   Debug.LogError($"Failed to initialize module '{module.ModuleId}': {ex.Message}");
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
                   Debug.LogError($"Failed to cleanup module '{module.ModuleId}': {ex.Message}");
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
                    if (Activator.CreateInstance(type) is IVehicleEditorModule module)
                    {
                        RegisterModule(module);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of module type '{type.Name}': {ex.Message}");
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