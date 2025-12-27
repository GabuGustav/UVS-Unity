using UnityEngine;

namespace UVS.Editor.Core
{
    // Minimal public contract both previews implement
    public interface IVehiclePreview
    {
        void SetVehicle(GameObject prefab);
        void RenderPreview(Rect rect);
        void Cleanup();
        void ToggleGizmo(string id, bool value);
    }
}
