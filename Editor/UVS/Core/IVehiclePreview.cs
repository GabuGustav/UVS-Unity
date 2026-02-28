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

    public interface ISeatPreview
    {
        void SetSeatData(VehicleConfig config, System.Action<int, Vector3, Vector3> onSeatChanged);
        void SetTopDown(bool enabled);
    }
}
