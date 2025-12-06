using UnityEngine;
using System;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    public class VehicleEditorContext
    {
        public VehicleConfig CurrentConfig { get; set; }
        public GameObject SelectedPrefab { get; set; }
        public VehicleIDRegistry Registry { get; set; }
        public Dictionary<string, VehicleConfig> GuidToConfigMap { get; set; }
        public bool IsFinalized { get; set; }
        public VehiclePreview3D Preview { get; set; }
        public EnhancedEditorConsole Console { get; set; }
        public Dictionary<VehicleConfig.VehiclePartType, List<Transform>> LastScan { get; set; }

        public VehicleConfig Config => CurrentConfig;

        public event Action<VehicleConfig> OnConfigChanged;
        public event Action<GameObject> OnPrefabChanged;
        public event Action<string> OnLogMessage;
        public event Action<string> OnLogError;
        public event Action OnValidationRequired;

        public VehicleEditorContext()
        {
            GuidToConfigMap = new Dictionary<string, VehicleConfig>();
            LastScan = new Dictionary<VehicleConfig.VehiclePartType, List<Transform>>();
        }

        public void NotifyConfigChanged(VehicleConfig config)
        {
            CurrentConfig = config;
            OnConfigChanged?.Invoke(config);
        }

        public void NotifyPrefabChanged(GameObject prefab)
        {
            SelectedPrefab = prefab;
            OnPrefabChanged?.Invoke(prefab);
        }

        public void LogMessage(string message)
        {
            OnLogMessage?.Invoke(message);
        }

        public void LogError(string error)
        {
            OnLogError?.Invoke(error);
        }

        public void RequestValidation()
        {
            OnValidationRequired?.Invoke();
        }
    }
}