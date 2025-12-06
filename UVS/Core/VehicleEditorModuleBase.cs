using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UVS.Editor.Core
{
    public interface IVehicleEditorModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        int Priority { get; }
        bool RequiresVehicle { get; }

        void Initialize(VehicleEditorContext context);
        VisualElement CreateUI();
        void OnActivate();
        void OnDeactivate();
        void OnUpdate();
        void OnSave();
        void OnLoad();
        ValidationResult Validate();
        void Cleanup();
        void OnModuleGUI();
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public class ValidationMessage
    {
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    public class ValidationResult
    {
        private List<ValidationMessage> messages = new List<ValidationMessage>();

        public bool IsValid
        {
            get { return !messages.Any(m => m.Severity == ValidationSeverity.Error); }
        }

        public string ErrorMessage { get; set; }
        public ValidationSeverity Severity { get; set; }

        public void AddError(string message)
        {
            messages.Add(new ValidationMessage { Message = message, Severity = ValidationSeverity.Error });
        }

        public void AddWarning(string message)
        {
            messages.Add(new ValidationMessage { Message = message, Severity = ValidationSeverity.Warning });
        }

        public static ValidationResult Success()
        {
            return new ValidationResult();
        }

        public static ValidationResult Error(string message)
        {
            var result = new ValidationResult();
            result.AddError(message);
            result.ErrorMessage = message;
            result.Severity = ValidationSeverity.Error;
            return result;
        }

        public static ValidationResult Warning(string message)
        {
            var result = new ValidationResult();
            result.AddWarning(message);
            result.ErrorMessage = message;
            result.Severity = ValidationSeverity.Warning;
            return result;
        }
    }

    public abstract class VehicleEditorModuleBase : IVehicleEditorModule
    {
        protected VehicleEditorContext _context;

        public abstract string ModuleId { get; }
        public abstract string DisplayName { get; }
        public abstract int Priority { get; }
        public virtual bool RequiresVehicle { get { return true; } }

        public VisualElement CreateUI()
        {
            return CreateModuleUI();
        }

        public void Initialize(VehicleEditorContext context)
        {
            _context = context;
            if (_context != null)
            {
                _context.OnConfigChanged += OnConfigChanged;
                _context.OnPrefabChanged += OnPrefabChanged;
            }
        }

        public void OnActivate()
        {
            OnModuleActivated();
        }

        public void OnDeactivate()
        {
            OnModuleDeactivated();
        }

        public ValidationResult Validate()
        {
            return ValidateModule();
        }

        public virtual void OnSave() { }
        public virtual void OnLoad() { }
        public virtual void OnUpdate() { }
        public virtual void OnModuleGUI() { }

        public void Cleanup()
        {
            if (_context != null)
            {
                _context.OnConfigChanged -= OnConfigChanged;
                _context.OnPrefabChanged -= OnPrefabChanged;
            }
        }

        protected abstract VisualElement CreateModuleUI();
        protected abstract ValidationResult ValidateModule();
        protected abstract void OnConfigChanged(VehicleConfig config);
        protected abstract void OnModuleActivated();

        protected virtual void OnModuleDeactivated() { }
        protected virtual void OnPrefabChanged(GameObject prefab) { }

        protected bool HasValidVehicle()
        {
            return _context != null && _context.CurrentConfig != null && _context.SelectedPrefab != null;
        }

        protected void LogMessage(string message)
        {
            if (_context != null && _context.Console != null)
                _context.Console.LogInfo(message);
        }

        protected void LogWarning(string message)
        {
            if (_context != null && _context.Console != null)
                _context.Console.LogWarning(message);
        }

        protected void LogError(string message)
        {
            if (_context != null && _context.Console != null)
                _context.Console.LogError(message);
        }
    }
}