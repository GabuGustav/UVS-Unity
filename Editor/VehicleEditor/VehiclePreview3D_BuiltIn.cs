using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    public class VehiclePreview3D_Builtin : IVehiclePreview, ISeatPreview
    {
        private GameObject _previewInstance;
        private PreviewRenderUtility _previewUtility;

        private Vector2 _cameraRotation = new(30f, 30f);
        private float _cameraDistance = 5f;
        private Vector3 _cameraPivot = Vector3.zero;

        private readonly VehiclePreviewGizmos _gizmos = new();
        private readonly List<Material> _tempMaterials = new();

        private VehicleConfig _seatConfig;
        private System.Action<int, Vector3, Vector3> _seatChanged;
        private bool _topDown;

        public VehiclePreview3D_Builtin()
        {
            Initialize();
        }

        private void Initialize()
        {
            _previewUtility = new PreviewRenderUtility();
            _previewUtility.camera.fieldOfView = 35f;
            _previewUtility.camera.nearClipPlane = 0.01f;
            _previewUtility.camera.farClipPlane = 1000f;
            _previewUtility.ambientColor = new Color(0.25f, 0.25f, 0.3f);

            if (_previewUtility.lights.Length > 0)
            {
                _previewUtility.lights[0].intensity = 1.6f;
                _previewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            if (_previewUtility.lights.Length > 1)
            {
                _previewUtility.lights[1].intensity = 1.0f;
                _previewUtility.lights[1].transform.rotation = Quaternion.Euler(-40f, 140f, 0f);
            }
        }

        #region IVehiclePreview
        public void SetVehicle(GameObject prefab)
        {
            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            if (prefab == null) return;

            _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (_previewInstance == null)
            {
                _previewInstance = UnityEngine.Object.Instantiate(prefab);
            }
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _previewUtility.AddSingleGO(_previewInstance);
            PreviewMaterialUtility.CleanupMaterials(_tempMaterials);

            foreach (var renderer in _previewInstance.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var resolved = PreviewMaterialUtility.ResolvePreviewMaterial(
                        mats[i],
                        PipelineShaderFallbackProfile.RenderPipelineTarget.BuiltIn,
                        _tempMaterials);
                    if (!ReferenceEquals(resolved, mats[i]))
                    {
                        mats[i] = resolved;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = mats;
            }

            Bounds b = CalculateBounds(_previewInstance);
            _cameraPivot = b.center;
            _cameraDistance = Mathf.Max(b.size.magnitude * 1.5f, 2f);

            _gizmos.Rebuild(_previewInstance);
            UpdateSeatGizmos();
        }

        public void RenderPreview(Rect previewRect)
        {
            if (_previewUtility == null || previewRect.width <= 0 || previewRect.height <= 0) return;

            HandleCameraControls(previewRect);

            Quaternion rot;
            Vector3 pos;
            if (_topDown)
            {
                rot = Quaternion.Euler(90f, 0f, 0f);
                pos = _cameraPivot + Vector3.up * _cameraDistance;
            }
            else
            {
                rot = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
                pos = _cameraPivot - rot * Vector3.forward * _cameraDistance;
            }
            _previewUtility.camera.transform.SetPositionAndRotation(pos, rot);

            _previewUtility.BeginPreview(previewRect, GUIStyle.none);
            _previewUtility.Render();

            if (_previewInstance == null)
                DrawGrid();

            Texture tex = _previewUtility.EndPreview();
            if (tex != null) GUI.DrawTexture(previewRect, tex, ScaleMode.StretchToFill, false);

            if (_previewInstance != null)
            {
                _gizmos.DrawOverlay(_previewUtility.camera, previewRect, _previewInstance);
                _gizmos.DrawHandles(_previewUtility.camera, previewRect);
            }
        }

        public void Cleanup()
        {
            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _gizmos.Clear();
            PreviewMaterialUtility.CleanupMaterials(_tempMaterials);

            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }
        }

        public void ToggleGizmo(string id, bool value)
        {
            _gizmos.Toggle(id, value);
        }
        #endregion

        public void SetSeatData(VehicleConfig config, System.Action<int, Vector3, Vector3> onSeatChanged)
        {
            _seatConfig = config;
            _seatChanged = onSeatChanged;
            UpdateSeatGizmos();
        }

        public void SetTopDown(bool enabled)
        {
            _topDown = enabled;
        }

        #region Internals
        private Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 2f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void HandleCameraControls(Rect r)
        {
            Event e = Event.current;
            if (!r.Contains(e.mousePosition) || _gizmos.IsDragging) return;

            if (_topDown)
            {
                if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 1))
                {
                    var cam = _previewUtility.camera;
                    _cameraPivot += (_cameraDistance * 0.01f) * (-e.delta.x * cam.transform.right + e.delta.y * cam.transform.up);
                    e.Use();
                }
                else if (e.type == EventType.ScrollWheel)
                {
                    _cameraDistance = Mathf.Clamp(_cameraDistance + e.delta.y * 0.1f * _cameraDistance, 0.5f, 100f);
                    e.Use();
                }
                return;
            }

            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    _cameraRotation.x += e.delta.x * 0.5f;
                    _cameraRotation.y -= e.delta.y * 0.5f;
                    _cameraRotation.y = Mathf.Clamp(_cameraRotation.y, -89f, 89f);
                    e.Use();
                }
                else if (e.button == 1)
                {
                    var cam = _previewUtility.camera;
                    _cameraPivot += (_cameraDistance * 0.01f) * (-e.delta.x * cam.transform.right + e.delta.y * cam.transform.up);
                    e.Use();
                }
            }
            else if (e.type == EventType.ScrollWheel)
            {
                _cameraDistance = Mathf.Clamp(_cameraDistance + e.delta.y * 0.1f * _cameraDistance, 0.5f, 100f);
                e.Use();
            }
        }

        private void UpdateSeatGizmos()
        {
            if (_previewInstance == null || _seatConfig == null || _seatConfig.seats == null || _seatConfig.seats.Count == 0)
            {
                _gizmos.SetSeatHandles(null);
                return;
            }

            var handles = new System.Collections.Generic.List<GizmoHandle>();
            for (int i = 0; i < _seatConfig.seats.Count; i++)
            {
                int index = i;
                var seat = _seatConfig.seats[index];
                Vector3 localPos = seat.localPosition;
                Vector3 localEuler = seat.localEuler;

                if (seat.overrideTransform != null)
                {
                    localPos = seat.overrideTransform.localPosition;
                    localEuler = seat.overrideTransform.localEulerAngles;
                }

                var worldPos = _previewInstance.transform.TransformPoint(localPos);
                var handle = new GizmoHandle
                {
                    id = $"seat_{index}_{seat.id}",
                    position = worldPos,
                    euler = localEuler,
                    type = GizmoType.Seat,
                    color = seat.role == VehicleConfig.SeatRole.Driver ? new Color(1f, 0.6f, 0.1f) : new Color(0.1f, 0.8f, 1f),
                    size = 0.12f,
                    onPositionChanged = newPos =>
                    {
                        if (_seatConfig == null || _previewInstance == null) return;
                        var local = _previewInstance.transform.InverseTransformPoint(newPos);
                        seat.localPosition = local;
                        _seatChanged?.Invoke(index, seat.localPosition, seat.localEuler);
                    },
                    onRotationChanged = newEuler =>
                    {
                        if (_seatConfig == null) return;
                        seat.localEuler = newEuler;
                        _seatChanged?.Invoke(index, seat.localPosition, seat.localEuler);
                    }
                };
                handles.Add(handle);
            }

            _gizmos.SetSeatHandles(handles);
        }

        private void DrawGrid()
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            float size = 10f;
            int div = 20;
            for (int i = -div; i <= div; i++)
            {
                float p = i * size / div;
                Handles.DrawLine(new Vector3(-size, 0, p), new Vector3(size, 0, p));
                Handles.DrawLine(new Vector3(p, 0, -size), new Vector3(p, 0, size));
            }
        }
        #endregion
    }
}


