using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UVS.Editor.Core
{
    public sealed class VehiclePreviewManager
    {
        public enum Mode
        {
            Auto,
            BuiltIn,
            URP
        }

        private IVehiclePreview _current;
        private Mode _mode = Mode.Auto;
        private RenderPipelineAsset _lastPipeline;
        private GameObject _currentVehicle; // Preserve vehicle across renderer switches

        public IVehiclePreview Current => _current;
        public Mode mode => _mode;

        public VehiclePreviewManager()
        {
            Rebuild();
        }

        public void SetMode(Mode newMode)
        {
            if (_mode == newMode) return;
            _mode = newMode;
            Rebuild();
        }

        public void Update()
        {
            if (_mode == Mode.Auto)
            {
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (_lastPipeline != pipeline)
                    Rebuild();
            }
        }

        private void Rebuild()
        {
            Cleanup();

            bool useURP = false;

#if UNITY_URP
            useURP =
                _mode == Mode.URP ||
                (_mode == Mode.Auto && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset);
#endif
            // If URP is not installed, useURP stays false regardless of Mode setting.
            // Mode.URP selected by the user but package missing → silently falls back to Built-in.

#if UNITY_URP
            _current = useURP
                ? new VehiclePreview3D_URP()
                : new VehiclePreview3D_Builtin();
#else
            _current = new VehiclePreview3D_Builtin();
#endif

            _lastPipeline = GraphicsSettings.currentRenderPipeline;

            UnityEngine.Debug.Log($"[PREVIEW] Rebuilt preview system: {(_current != null ? _current.GetType().Name : "NULL")} (Mode: {_mode}, URP: {useURP})");

            // Reapply the vehicle to the new renderer
            if (_currentVehicle != null)
            {
                _current?.SetVehicle(_currentVehicle);
                UnityEngine.Debug.Log($"[PREVIEW] Reapplied vehicle '{_currentVehicle.name}' to new renderer");
            }
        }

        public void SetVehicle(GameObject prefab)
        {
            _currentVehicle = prefab; // Cache for Rebuild
            UnityEngine.Debug.Log($"[PREVIEW] SetVehicle called on manager, _current is {(_current != null ? "VALID" : "NULL")}");
            _current?.SetVehicle(prefab);
        }

        public void ToggleGizmo(string id, bool value)
        {
            _current?.ToggleGizmo(id, value);
        }

        public void Cleanup()
        {
            _current?.Cleanup();
            _current = null;
        }
    }
}