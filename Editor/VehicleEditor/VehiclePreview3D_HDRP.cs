#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION || UNITY_PIPELINE_HDRP || UNITY_HDRP

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace UVS.Editor.Core
{
    public sealed class VehiclePreview3D_HDRP : IVehiclePreview, ISeatPreview
    {
        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private Scene _previewScene;
        private readonly List<Material> _tempMaterials = new();

        private Bounds _bounds;
        private Vector2 _orbit = new(30f, 30f);
        private float _distance = 6f;
        private const float ZoomSpeed = 0.5f;

        private readonly VehiclePreviewGizmos _gizmos = new();
        private VehicleConfig _seatConfig;
        private System.Action<int, Vector3, Vector3> _seatChanged;
        private bool _topDown;

        public VehiclePreview3D_HDRP()
        {
            Initialize();
        }

        private void Initialize()
        {
            _preview = new PreviewRenderUtility(true);
            _preview.camera.fieldOfView = 30f;
            _preview.camera.nearClipPlane = 0.05f;
            _preview.camera.farClipPlane = 500f;
            _preview.camera.clearFlags = CameraClearFlags.Skybox;
            _preview.camera.backgroundColor = Color.gray;
            _preview.camera.forceIntoRenderTexture = true;

            if (!_preview.camera.TryGetComponent<HDAdditionalCameraData>(out _))
                _preview.camera.gameObject.AddComponent<HDAdditionalCameraData>();

            _preview.lights[0].intensity = 1.3f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            if (_preview.lights.Length > 1)
            {
                _preview.lights[1].intensity = 0.8f;
                _preview.lights[1].transform.rotation = Quaternion.Euler(-35f, 140f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);

            _previewScene = EditorSceneManager.NewPreviewScene();
        }

        public void Cleanup()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }

            _gizmos.Clear();
            PreviewMaterialUtility.CleanupMaterials(_tempMaterials);

            if (_previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(_previewScene);

            _preview?.Cleanup();
            _preview = null;
        }

        public void SetVehicle(GameObject prefab)
        {
            if (_instance != null)
                Object.DestroyImmediate(_instance);

            if (prefab == null)
                return;

            _instance = Object.Instantiate(prefab);
            _instance.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(_instance, _previewScene);
            _preview.AddSingleGO(_instance);

            PreviewMaterialUtility.CleanupMaterials(_tempMaterials);

            foreach (var renderer in _instance.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var resolved = PreviewMaterialUtility.ResolvePreviewMaterial(
                        mats[i],
                        PipelineShaderFallbackProfile.RenderPipelineTarget.HDRP,
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

            CalculateBounds();
            PositionCamera();
            _gizmos.Rebuild(_instance);
            UpdateSeatGizmos();
        }

        private void CalculateBounds()
        {
            if (_instance == null)
            {
                _bounds = new Bounds(Vector3.zero, Vector3.one);
                return;
            }

            var renderers = _instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                _bounds = new Bounds(_instance.transform.position, Vector3.one);
                return;
            }

            _bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                _bounds.Encapsulate(renderers[i].bounds);

            _distance = Mathf.Max(_bounds.size.x, _bounds.size.y, _bounds.size.z) * 2f;
        }

        private void PositionCamera()
        {
            Vector3 center = _bounds.center;
            Quaternion rot;
            Vector3 offset;

            if (_topDown)
            {
                rot = Quaternion.Euler(90f, 0f, 0f);
                offset = Vector3.up * _distance;
            }
            else
            {
                rot = Quaternion.Euler(_orbit.y, _orbit.x, 0f);
                offset = rot * Vector3.back * _distance;
            }

            _preview.camera.transform.position = center + offset;
            _preview.camera.transform.LookAt(center);
        }

        public void RenderPreview(Rect rect)
        {
            if (_preview == null) return;

            HandleInput(rect);
            if (_instance != null) PositionCamera();

            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            var tex = _preview.EndPreview();
            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);

            if (_instance != null)
            {
                _gizmos.DrawOverlay(_preview.camera, rect, _instance);
                _gizmos.DrawHandles(_preview.camera, rect);
            }
        }

        private void HandleInput(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition) || _gizmos.IsDragging)
                return;

            if (_topDown)
            {
                if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 1))
                {
                    _bounds.center += (_distance * 0.01f) * (-e.delta.x * _preview.camera.transform.right + e.delta.y * _preview.camera.transform.up);
                    e.Use();
                }
                if (e.type == EventType.ScrollWheel)
                {
                    _distance += e.delta.y * ZoomSpeed;
                    _distance = Mathf.Clamp(_distance, 1f, 50f);
                    e.Use();
                }
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                _orbit.x += e.delta.x * 0.5f;
                _orbit.y -= e.delta.y * 0.5f;
                _orbit.y = Mathf.Clamp(_orbit.y, 5f, 85f);
                e.Use();
            }

            if (e.type == EventType.ScrollWheel)
            {
                _distance += e.delta.y * ZoomSpeed;
                _distance = Mathf.Clamp(_distance, 1f, 50f);
                e.Use();
            }
        }

        public void ToggleGizmo(string id, bool value)
        {
            _gizmos.Toggle(id, value);
        }

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

        private void UpdateSeatGizmos()
        {
            if (_instance == null || _seatConfig == null || _seatConfig.seats == null || _seatConfig.seats.Count == 0)
            {
                _gizmos.SetSeatHandles(null);
                return;
            }

            var handles = new List<GizmoHandle>();
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

                var worldPos = _instance.transform.TransformPoint(localPos);
                handles.Add(new GizmoHandle
                {
                    id = $"seat_{index}_{seat.id}",
                    position = worldPos,
                    euler = localEuler,
                    type = GizmoType.Seat,
                    color = seat.role == VehicleConfig.SeatRole.Driver ? new Color(1f, 0.6f, 0.1f) : new Color(0.1f, 0.8f, 1f),
                    size = 0.12f,
                    onPositionChanged = newPos =>
                    {
                        if (_seatConfig == null || _instance == null) return;
                        seat.localPosition = _instance.transform.InverseTransformPoint(newPos);
                        _seatChanged?.Invoke(index, seat.localPosition, seat.localEuler);
                    },
                    onRotationChanged = newEuler =>
                    {
                        seat.localEuler = newEuler;
                        _seatChanged?.Invoke(index, seat.localPosition, seat.localEuler);
                    }
                });
            }

            _gizmos.SetSeatHandles(handles);
        }
    }
}

#endif
