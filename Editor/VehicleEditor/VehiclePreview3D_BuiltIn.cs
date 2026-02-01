using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace UVS.Editor.Core
{
    public class VehiclePreview3D_Builtin : IVehiclePreview
    {
        // Public-ish state (kept private but accessible through interface)
        private GameObject _currentPrefab;       // original prefab pointer
        private GameObject _previewInstance;     // clone shown in preview
        private PreviewRenderUtility _previewUtility;

        // camera
        private Vector2 _cameraRotation = new(30f, 30f); // yaw/x, pitch/y
        private float _cameraDistance = 5f;
        private Vector3 _cameraPivot = Vector3.zero;

        // gizmos
        private bool _wheelsGizmo = true;
        private bool _collidersGizmo = true;
        private bool _suspensionGizmo = true;

        private readonly Dictionary<string, GizmoHandle> _gizmoHandles = new();
        private GizmoHandle _selectedGizmo;
        private bool _isDraggingGizmo;

        private Material _fallbackMaterial;

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

            // lights
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

            // fallback material
            Shader s = Shader.Find("Hidden/Internal-Colored");
            _fallbackMaterial = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            if (_fallbackMaterial.HasProperty("_Color")) _fallbackMaterial.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f));
        }

        #region IVehiclePreview
        public void SetVehicle(GameObject prefab)
        {
            // store original prefab (other systems expect this)
            _currentPrefab = prefab;

            // cleanup previous preview instance (but keep previewUtility)
            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            if (prefab == null) return;

            // instantiate clone
            _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (_previewInstance == null)
            {
                _previewInstance = UnityEngine.Object.Instantiate(prefab);
            }
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // add to preview utility so it is rendered
            _previewUtility.AddSingleGO(_previewInstance);

            // camera framing
            Bounds b = CalculateBounds(_previewInstance);
            _cameraPivot = b.center;
            _cameraDistance = Mathf.Max(b.size.magnitude * 1.5f, 2f);

            CreateGizmoHandles();
        }

        public void RenderPreview(Rect previewRect)
        {
            if (_previewUtility == null || previewRect.width <= 0 || previewRect.height <= 0) return;

            HandleCameraControls(previewRect);

            Quaternion rot = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
            Vector3 pos = _cameraPivot - rot * Vector3.forward * _cameraDistance;
            _previewUtility.camera.transform.SetPositionAndRotation(pos, rot);

            _previewUtility.BeginPreview(previewRect, GUIStyle.none);
            _previewUtility.Render();

            if (_previewInstance == null)
                DrawGrid();

            Texture tex = _previewUtility.EndPreview();
            if (tex != null) GUI.DrawTexture(previewRect, tex, ScaleMode.StretchToFill, false);

            if (_previewInstance != null)
            {
                DrawGizmosOverlay(previewRect);
                DrawGizmoHandles(previewRect);
            }
        }

        public void Cleanup()
        {
            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _gizmoHandles.Clear();
            _isDraggingGizmo = false;
            _selectedGizmo = null;

            if (_fallbackMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_fallbackMaterial);
                _fallbackMaterial = null;
            }

            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            _currentPrefab = null;
        }

        public void ToggleGizmo(string id, bool value)
        {
            switch (id)
            {
                case "wheels": _wheelsGizmo = value; break;
                case "colliders": _collidersGizmo = value; break;
                case "suspension": _suspensionGizmo = value; break;
            }
        }
        #endregion

        #region Internals
        private Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 2f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void CreateGizmoHandles()
        {
            _gizmoHandles.Clear();
            if (_previewInstance == null) return;

            // wheel colliders
            var wheels = _previewInstance.GetComponentsInChildren<WheelCollider>();
            foreach (var w in wheels)
            {
                var handle = new GizmoHandle
                {
                    id = $"wheel_{GetStablePath(w.transform)}",
                    position = w.transform.TransformPoint(w.center),
                    type = GizmoType.Wheel,
                    color = Color.yellow,
                    size = Mathf.Clamp(w.radius * 0.5f, 0.08f, 0.4f),
                    targetObject = w,
                    onPositionChanged = (newPos) =>
                    {
                        Vector3 local = w.transform.InverseTransformPoint(newPos);
                        w.center = local;
                    }
                };
                _gizmoHandles[handle.id] = handle;
            }

            // other colliders
            var colliders = _previewInstance.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (c is WheelCollider) continue;
                var handle = new GizmoHandle
                {
                    id = $"col_{GetStablePath(c.transform)}",
                    position = c.bounds.center,
                    type = GizmoType.Collider,
                    color = new Color(0f, 0.8f, 1f, 0.9f),
                    size = 0.16f,
                    targetObject = c,
                    onPositionChanged = (newPos) =>
                    {
                        Vector3 offset = newPos - c.bounds.center;
                        c.transform.position += offset;
                    }
                };
                _gizmoHandles[handle.id] = handle;
            }
        }

        private void HandleCameraControls(Rect r)
        {
            Event e = Event.current;
            if (!r.Contains(e.mousePosition) || _isDraggingGizmo) return;

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

        private void DrawGizmosOverlay(Rect previewRect)
        {
            // Using Handles (they draw on screen) — matrix reset ensures consistent drawing
            Matrix4x4 old = Handles.matrix;
            Color colOld = Handles.color;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                if (_wheelsGizmo)
                {
                    var wheels = _previewInstance.GetComponentsInChildren<WheelCollider>();
                    foreach (var w in wheels) DrawWheelGizmo(w);
                }

                if (_collidersGizmo)
                {
                    var cols = _previewInstance.GetComponentsInChildren<Collider>();
                    foreach (var c in cols)
                        if (c is not WheelCollider) DrawColliderGizmo(c);
                }
            }
            finally
            {
                Handles.matrix = old;
                Handles.color = colOld;
            }
        }

        private void DrawWheelGizmo(WheelCollider w)
        {
            Vector3 pos = w.transform.TransformPoint(w.center);
            float rad = w.radius;
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(pos, w.transform.up, rad);
            Handles.DrawLine(pos + w.transform.forward * rad, pos - w.transform.forward * rad);

            Handles.color = Color.red;
            DrawWireSphere(pos, 0.06f);

            if (_suspensionGizmo)
            {
                Handles.color = Color.green;
                Vector3 top = w.transform.position;
                Vector3 bottom = top - w.transform.up * w.suspensionDistance;
                DrawSpring(top, bottom, 7, 0.08f, 0.08f, 0.5f, 10);
            }
        }

        private void DrawColliderGizmo(Collider c)
        {
            Handles.color = new Color(0f, 0.8f, 1f, 0.8f);
            if (c is BoxCollider box)
            {
                Vector3 center = c.transform.TransformPoint(box.center);
                Vector3 size = Vector3.Scale(box.size, c.transform.lossyScale);
                DrawWireCube(center, size, c.transform.rotation);
            }
            else if (c is SphereCollider sphere)
            {
                Vector3 center = c.transform.TransformPoint(sphere.center);
                float rad = sphere.radius * Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.y, c.transform.lossyScale.z);
                Handles.DrawWireDisc(center, Vector3.up, rad);
                Handles.DrawWireDisc(center, Vector3.right, rad);
                Handles.DrawWireDisc(center, Vector3.forward, rad);
            }
            else if (c is CapsuleCollider capsule)
            {
                Vector3 center = c.transform.TransformPoint(capsule.center);
                float rad = capsule.radius;
                Handles.DrawWireDisc(center, Vector3.up, rad);
            }
        }

        private void DrawWireSphere(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private void DrawSpring(Vector3 p1, Vector3 p2, int coils, float startRadius, float endRadius, float radiusScale, int resolutionPerCoil)
        {
            if (p1 == p2 || coils <= 0) return;
            Quaternion rot = Quaternion.LookRotation(p2 - p1);
            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            Vector3 prev = p1;
            int segments = coils * resolutionPerCoil;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float rad = Mathf.Lerp(startRadius, endRadius, t);
                float angle = t * coils * Mathf.PI * 2f;
                Vector3 offset = rad * radiusScale * (up * Mathf.Sin(angle) + right * Mathf.Cos(angle));
                Vector3 point = Vector3.Lerp(p1, p2, t) + offset;
                Handles.DrawLine(prev, point);
                prev = point;
            }
        }

        private void DrawWireCube(Vector3 center, Vector3 size, Quaternion rot)
        {
            Vector3 half = size * 0.5f;
            Vector3[] pts = new Vector3[8]
            {
                center + rot * new Vector3(-half.x, -half.y, -half.z),
                center + rot * new Vector3(-half.x, -half.y, half.z),
                center + rot * new Vector3(-half.x, half.y, -half.z),
                center + rot * new Vector3(-half.x, half.y, half.z),
                center + rot * new Vector3(half.x, -half.y, -half.z),
                center + rot * new Vector3(half.x, -half.y, half.z),
                center + rot * new Vector3(half.x, half.y, -half.z),
                center + rot * new Vector3(half.x, half.y, half.z)
            };
            Handles.DrawLine(pts[0], pts[1]); Handles.DrawLine(pts[0], pts[2]); Handles.DrawLine(pts[0], pts[4]);
            Handles.DrawLine(pts[1], pts[3]); Handles.DrawLine(pts[1], pts[5]); Handles.DrawLine(pts[2], pts[3]);
            Handles.DrawLine(pts[2], pts[6]); Handles.DrawLine(pts[3], pts[7]); Handles.DrawLine(pts[4], pts[5]);
            Handles.DrawLine(pts[4], pts[6]); Handles.DrawLine(pts[5], pts[7]); Handles.DrawLine(pts[6], pts[7]);
        }

        private void DrawGizmoHandles(Rect previewRect)
        {
            if (_previewInstance == null || _gizmoHandles.Count == 0) return;

            Matrix4x4 old = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                foreach (var kv in _gizmoHandles)
                {
                    var h = kv.Value;
                    if (!IsGizmoEnabled(h.type)) continue;

                    Vector3 screen = _previewUtility.camera.WorldToScreenPoint(h.position);
                    if (screen.z <= 0) continue;

                    screen.y = previewRect.height - screen.y;
                    float scale = Mathf.Clamp(12f / Mathf.Max(1f, screen.z), 0.7f, 2f);

                    Rect r = new(previewRect.x + screen.x - 10f * scale, previewRect.y + screen.y - 10f * scale, 20f * scale, 20f * scale);
                    EditorGUI.DrawRect(r, new Color(h.color.r, h.color.g, h.color.b, 0.95f));

                    HandleGizmoInteraction(h, r, previewRect);
                }
            }
            finally
            {
                Handles.matrix = old;
            }
        }

        private void HandleGizmoInteraction(GizmoHandle handle, Rect rect, Rect previewRect)
        {
            Event e = Event.current;
            if (!previewRect.Contains(e.mousePosition)) return;

            switch (e.type)
            {
                case EventType.MouseDown when rect.Contains(e.mousePosition) && e.button == 0:
                    _selectedGizmo = handle;
                    _isDraggingGizmo = true;
                    e.Use();
                    break;

                case EventType.MouseDrag when _isDraggingGizmo && _selectedGizmo == handle && e.button == 0:
                    Plane plane = new(-_previewUtility.camera.transform.forward, handle.position);
                    Vector2 mPrev = e.mousePosition - e.delta;
                    Vector2 mCurr = e.mousePosition;

                    Ray prevRay = _previewUtility.camera.ScreenPointToRay(new Vector3(mPrev.x, previewRect.height - mPrev.y, 0));
                    Ray currRay = _previewUtility.camera.ScreenPointToRay(new Vector3(mCurr.x, previewRect.height - mCurr.y, 0));

                    if (plane.Raycast(prevRay, out float d1) && plane.Raycast(currRay, out float d2))
                    {
                        Vector3 delta = currRay.GetPoint(d2) - prevRay.GetPoint(d1);
                        Vector3 newPos = handle.position + delta;
                        handle.position = newPos;
                        handle.onPositionChanged?.Invoke(newPos);
                    }
                    e.Use();
                    break;

                case EventType.MouseUp when _isDraggingGizmo && e.button == 0:
                    _isDraggingGizmo = false;
                    _selectedGizmo = null;
                    e.Use();
                    break;
            }
        }

        private bool IsGizmoEnabled(GizmoType t)
        {
            return t switch
            {
                GizmoType.Wheel => _wheelsGizmo,
                GizmoType.Collider => _collidersGizmo,
                GizmoType.Suspension => _suspensionGizmo,
                _ => true
            };
        }

        private static string GetStablePath(Transform t)
        {
            if (t == null) return string.Empty;
            string path = t.name;
            var cur = t.parent;
            while (cur != null)
            {
                path = cur.name + "/" + path;
                cur = cur.parent;
            }
            return path;
        }
        #endregion
    }
}