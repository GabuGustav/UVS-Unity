using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    /// <summary>
    /// URP-compatible 3D preview using PreviewRenderUtility (IMGUI-safe)
    /// </summary>
    public sealed class VehiclePreview3D_URP : IVehiclePreview
    {
        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private Scene _previewScene;

        private Bounds _bounds;
        private Vector2 _orbit = new(30f, 30f);
        private float _distance = 6f;
        private const float ZOOM_SPEED = 0.5f;

        private bool _showWheels = true;
        private bool _showColliders = true;
        private bool _showSuspension = true;

        public VehiclePreview3D_URP()
        {
            Initialize();
        }

        // ------------------------------------------------------------
        // INITIALIZATION
        // ------------------------------------------------------------

        private void Initialize()
        {
            _preview = new PreviewRenderUtility(true);
            _preview.camera.fieldOfView = 30f;
            _preview.camera.nearClipPlane = 0.05f;
            _preview.camera.farClipPlane = 500f;
            _preview.camera.clearFlags = CameraClearFlags.Skybox;
            _preview.camera.backgroundColor = Color.gray;
            _preview.camera.forceIntoRenderTexture = true;

            // 🔴 REQUIRED FOR URP (THIS IS THE CORE FIX)
            if (!_preview.camera.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
                camData = _preview.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = false;
            camData.requiresColorOption = CameraOverrideOption.On;
            camData.requiresDepthOption = CameraOverrideOption.On;

            // Lighting (URP DOES NOT AUTO-INFER THIS)
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

        // ------------------------------------------------------------
        // CLEANUP
        // ------------------------------------------------------------

        public void Cleanup()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }

            if (_previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(_previewScene);

            _preview?.Cleanup();
            _preview = null;
        }

        // ------------------------------------------------------------
        // VEHICLE SETUP
        // ------------------------------------------------------------

        public void SetVehicle(GameObject prefab)
        {
            if (_instance != null)
                Object.DestroyImmediate(_instance);

            if (prefab == null)
                return;

            _instance = Object.Instantiate(prefab);
            _instance.hideFlags = HideFlags.HideAndDontSave;

            SceneManager.MoveGameObjectToScene(_instance, _previewScene);

            // 🔧 URP MATERIAL SAFETY (preview-only)
            foreach (var r in _instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null && mat.shader.name == "Standard")
                        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }

            CalculateBounds();
            PositionCamera();
        }

        // ------------------------------------------------------------
        // CAMERA & BOUNDS
        // ------------------------------------------------------------

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
            Quaternion rot = Quaternion.Euler(_orbit.y, _orbit.x, 0f);
            Vector3 offset = rot * Vector3.back * _distance;

            _preview.camera.transform.position = center + offset;
            _preview.camera.transform.LookAt(center);
        }

        // ------------------------------------------------------------
        // RENDERING
        // ------------------------------------------------------------

        public void RenderPreview(Rect rect)
        {
            if (_preview == null)
                return;

            HandleInput(rect);

            if (_instance != null)
                PositionCamera();

            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            Texture tex = _preview.EndPreview();

            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);

            DrawOverlay(rect);
        }

        // ------------------------------------------------------------
        // INPUT
        // ------------------------------------------------------------

        private void HandleInput(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                _orbit.x += e.delta.x * 0.5f;
                _orbit.y -= e.delta.y * 0.5f;
                _orbit.y = Mathf.Clamp(_orbit.y, 5f, 85f);
                e.Use();
            }

            if (e.type == EventType.ScrollWheel)
            {
                _distance += e.delta.y * ZOOM_SPEED;
                _distance = Mathf.Clamp(_distance, 1f, 50f);
                e.Use();
            }
        }

        // ------------------------------------------------------------
        // GIZMOS / OVERLAY
        // ------------------------------------------------------------

        public void ToggleGizmo(string id, bool value)
        {
            switch (id)
            {
                case "wheels": _showWheels = value; break;
                case "colliders": _showColliders = value; break;
                case "suspension": _showSuspension = value; break;
            }
        }

        private void DrawOverlay(Rect rect)
        {
            Handles.BeginGUI();

            float y = rect.y + 10f;
            if (_showWheels) Handles.Label(new Vector2(rect.x + 10, y), "Wheels"); y += 18;
            if (_showColliders) Handles.Label(new Vector2(rect.x + 10, y), "Colliders"); y += 18;
            if (_showSuspension) Handles.Label(new Vector2(rect.x + 10, y), "Suspension");

            Handles.EndGUI();
        }
    }
}
