using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        public IVehiclePreview Current => _current;
        public Mode mode => _mode;

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

            bool useURP =
                _mode == Mode.URP ||
                (_mode == Mode.Auto && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset);

            _current = useURP
                ? new VehiclePreview3D_URP()
                : new VehiclePreview3D_Builtin();

            _lastPipeline = GraphicsSettings.currentRenderPipeline;
        }

        public void SetVehicle(GameObject prefab)
        {
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
