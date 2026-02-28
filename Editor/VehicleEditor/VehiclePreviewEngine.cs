using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_PIPELINE_URP || UNITY_URP
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION || UNITY_PIPELINE_HDRP || UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UVS.Editor.Core
{
    public sealed class VehiclePreviewManager
    {
        public enum Mode
        {
            Auto,
            BuiltIn,
            URP,
            HDRP
        }

        private IVehiclePreview _current;
        private Mode _mode = Mode.Auto;
        private RenderPipelineAsset _lastPipeline;
        private GameObject _currentVehicle;

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

        public bool IsModeSupported(Mode mode)
        {
            return mode switch
            {
                Mode.URP => IsUrpAvailable(),
                Mode.HDRP => IsHdrpAvailable(),
                _ => true
            };
        }

        private void Rebuild()
        {
            Cleanup();

            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            bool useURP = false;
            bool useHDRP = false;

            if (_mode == Mode.Auto)
            {
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION || UNITY_PIPELINE_HDRP || UNITY_HDRP
                useHDRP = currentPipeline is HDRenderPipelineAsset;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_PIPELINE_URP || UNITY_URP
                useURP = !useHDRP && currentPipeline is UniversalRenderPipelineAsset;
#endif
            }
            else if (_mode == Mode.URP)
            {
                useURP = IsUrpAvailable();
            }
            else if (_mode == Mode.HDRP)
            {
                useHDRP = IsHdrpAvailable();
            }

            if (useHDRP)
            {
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION || UNITY_PIPELINE_HDRP || UNITY_HDRP
                _current = new VehiclePreview3D_HDRP();
#else
                _current = new VehiclePreview3D_Builtin();
#endif
            }
            else if (useURP)
            {
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_PIPELINE_URP || UNITY_URP
                _current = new VehiclePreview3D_URP();
#else
                _current = new VehiclePreview3D_Builtin();
#endif
            }
            else
            {
                _current = new VehiclePreview3D_Builtin();
            }

            _lastPipeline = currentPipeline;

            UnityEngine.Debug.Log($"[PREVIEW] Rebuilt preview: {(_current != null ? _current.GetType().Name : "NULL")} (Mode: {_mode}, URP: {useURP}, HDRP: {useHDRP})");

            if (_currentVehicle != null)
            {
                _current?.SetVehicle(_currentVehicle);
            }
        }

        public void SetVehicle(GameObject prefab)
        {
            _currentVehicle = prefab;
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

        private bool IsUrpAvailable()
        {
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_PIPELINE_URP || UNITY_URP
            return true;
#else
            return false;
#endif
        }

        private bool IsHdrpAvailable()
        {
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION || UNITY_PIPELINE_HDRP || UNITY_HDRP
            return true;
#else
            return false;
#endif
        }
    }
}
