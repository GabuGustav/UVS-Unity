using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    /// <summary>
    /// Enhanced 3D Preview system with interactive gizmos for vehicle editor — URP-ready
    /// </summary>
    public class VehiclePreview3D
    {
        private GameObject _currentVehicle;
        private GameObject _previewInstance; // Clone for full URP rendering

        private PreviewRenderUtility _previewUtility;

        private Vector2 _cameraRotation = new(0, 0);
        private float _cameraDistance = 5f;
        private Vector3 _cameraPivot = Vector3.zero;

        // Gizmo states
        private bool _wheelsGizmo = true;
        private bool _collidersGizmo = true;
        private bool _suspensionGizmo = true;

        // Interactive gizmo data
        private readonly Dictionary<string, GizmoHandle> _gizmoHandles = new();
        private GizmoHandle _selectedGizmo;
        private bool _isDraggingGizmo = false;

        // Default fallback material
        private Material _defaultMaterial;

        public class GizmoHandle
        {
            public string id;
            public Vector3 position;
            public GizmoType type;
            public System.Action<Vector3> onPositionChanged;
            public float size = 0.1f;
            public Color color = Color.red;
        }

        public enum GizmoType
        {
            Position,
            Rotation,
            Wheel,
            Collider,
            Suspension
        }

        public VehiclePreview3D()
        {
            InitializePreview();
        }

        private void InitializePreview()
        {
            _previewUtility = new PreviewRenderUtility();
            _previewUtility.camera.fieldOfView = 30f;
            _previewUtility.camera.nearClipPlane = 0.01f;
            _previewUtility.camera.farClipPlane = 1000f;

            // Smart fallback material (URP/HDRP/Built-in safe)
            Shader fallback =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("HDRP/Unlit") ??
                Shader.Find("Hidden/Internal-Colored");

            _defaultMaterial = new Material(fallback) { hideFlags = HideFlags.HideAndDontSave };

            if (fallback != null && (fallback.name.Contains("Unlit") || fallback.name.Contains("Universal")))
                _defaultMaterial.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f));
            else if (fallback != null)
                _defaultMaterial.SetColor("_Color", new Color(0.75f, 0.75f, 0.75f));

            // Enhanced lighting
            _previewUtility.ambientColor = new Color(0.25f, 0.25f, 0.3f);
            _previewUtility.lights[0].intensity = 1.6f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            if (_previewUtility.lights.Length > 1)
            {
                _previewUtility.lights[1].intensity = 1.0f;
                _previewUtility.lights[1].transform.rotation = Quaternion.Euler(-40f, 140f, 0f);
            }
        }

        public void SetVehicle(GameObject vehicle)
        {
            // Destroy old preview instance
            if (_previewInstance != null)
            {
                _previewUtility?.Cleanup();
                Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _currentVehicle = vehicle;

            if (vehicle != null)
            {
                // Clone the vehicle for preview
                _previewInstance = Object.Instantiate(vehicle);
                _previewInstance.hideFlags = HideFlags.HideAndDontSave;
                _previewInstance.transform.position = Vector3.zero;

                // Add clone to preview utility (persists until Cleanup)
                _previewUtility.AddSingleGO(_previewInstance);

                Bounds bounds = CalculateBounds(_previewInstance);

                _cameraPivot = bounds.center;
                _cameraDistance = Mathf.Max(bounds.size.magnitude * 1.5f, 3f);

                CreateGizmoHandles();
            }
        }

        private Bounds CalculateBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one * 2f);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            return b;
        }

        private void CreateGizmoHandles()
        {
            _gizmoHandles.Clear();
            if (_currentVehicle == null) return;

            // Wheels
            WheelCollider[] wheels = _currentVehicle.GetComponentsInChildren<WheelCollider>();
            foreach (WheelCollider w in wheels)
            {
                var h = new GizmoHandle
                {
                    id = $"wheel_{w.name}",
                    position = w.transform.TransformPoint(w.center),
                    type = GizmoType.Wheel,
                    color = Color.yellow,
                    size = 0.25f,
                    onPositionChanged = (newPos) =>
                    {
                        Vector3 local = w.transform.InverseTransformPoint(newPos);
                        w.center = local;
                    }
                };
                _gizmoHandles.Add(h.id, h);
            }

            // Other colliders
            Collider[] colliders = _currentVehicle.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                if (c is WheelCollider) continue;

                var h = new GizmoHandle
                {
                    id = $"col_{c.name}",
                    position = c.bounds.center,
                    type = GizmoType.Collider,
                    color = Color.cyan,
                    size = 0.2f,
                    onPositionChanged = (newPos) =>
                    {
                        Vector3 offset = newPos - c.bounds.center;
                        c.transform.position += offset;
                    }
                };
                _gizmoHandles.Add(h.id, h);
            }
        }

        public void ToggleGizmo(string gizmoType, bool state)
        {
            switch (gizmoType.ToLower())
            {
                case "wheels": _wheelsGizmo = state; break;
                case "colliders": _collidersGizmo = state; break;
                case "suspension": _suspensionGizmo = state; break;
            }
        }

        public void RenderPreview(Rect previewRect)
        {
            if (_previewUtility == null || previewRect.width <= 0 || previewRect.height <= 0)
                return;

            HandleCameraControls(previewRect);

            Quaternion rot = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0);
            Vector3 pos = _cameraPivot - rot * Vector3.forward * _cameraDistance;

            _previewUtility.camera.transform.SetPositionAndRotation(pos, rot);

            // FIXED: Simple loop - Begin/Render/End (added GO renders automatically)
            _previewUtility.BeginPreview(previewRect, GUIStyle.none);
            _previewUtility.Render();

            if (_previewInstance == null)
            {
                DrawGrid();
            }

            Texture tex = _previewUtility.EndPreview();
            if (tex != null)
                GUI.DrawTexture(previewRect, tex, ScaleMode.StretchToFill, false);

            if (_currentVehicle != null)
            {
                DrawGizmosOverlay(previewRect);
                DrawGizmoHandles(previewRect);
            }
        }

        private void DrawVehicleRecursive(Transform transform)
        {
            if (transform == null) return;

            foreach (Transform child in transform)
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                Renderer r = child.GetComponent<Renderer>();

                if (mf != null ? mf.sharedMesh : null != null && r != null)
                {
                    Material[] mats = r.sharedMaterials;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        Material mat = mats[i];

                        if (mat == null || !mat.shader.isSupported)
                            mat = _defaultMaterial;

                        _previewUtility.DrawMesh(
                            mf.sharedMesh,
                            child.localToWorldMatrix,
                            mat,
                            i
                        );
                    }
                }

                DrawVehicleRecursive(child);
            }
        }

        private void HandleCameraControls(Rect r)
        {
            Event e = Event.current;
            if (!r.Contains(e.mousePosition) || _isDraggingGizmo)
                return;

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
                    _cameraPivot += (_cameraDistance * 0.01f) *
                        (-e.delta.x * _previewUtility.camera.transform.right +
                          e.delta.y * _previewUtility.camera.transform.up);

                    e.Use();
                }
            }
            else if (e.type == EventType.ScrollWheel)
            {
                _cameraDistance = Mathf.Clamp(
                    _cameraDistance + e.delta.y * 0.1f * _cameraDistance,
                    0.5f,
                    100f
                );
                e.Use();
            }
        }

        private void DrawGizmosOverlay(Rect previewRect)
        {
            Matrix4x4 oldMatrix = Handles.matrix;
            Color oldColor = Handles.color;

            Handles.matrix = Matrix4x4.identity;

            try
            {
                if (_wheelsGizmo)
                {
                    foreach (WheelCollider w in _currentVehicle.GetComponentsInChildren<WheelCollider>())
                        DrawWheelGizmo(w);
                }

                if (_collidersGizmo)
                {
                    foreach (Collider c in _currentVehicle.GetComponentsInChildren<Collider>())
                        if (c is not WheelCollider)
                            DrawColliderGizmo(c);
                }
            }
            finally
            {
                Handles.matrix = oldMatrix;
                Handles.color = oldColor;
            }
        }

        private void DrawWheelGizmo(WheelCollider wheel)
        {
            Vector3 pos = wheel.transform.TransformPoint(wheel.center);
            float rad = wheel.radius;

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(pos, wheel.transform.up, rad);
            Handles.DrawLine(
                pos + wheel.transform.forward * rad,
                pos - wheel.transform.forward * rad
            );

            Handles.color = Color.red;
            DrawWireSphere(pos, 0.06f);

            if (_suspensionGizmo)
            {
                Handles.color = Color.green;

                Vector3 top = wheel.transform.position;
                Vector3 bottom = top - wheel.transform.up * wheel.suspensionDistance;

                DrawSpring(top, bottom, 7, 0.08f, 0.08f, 0.5f, 10);
            }
        }

        private void DrawWireSphere(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private void DrawSpring(
            Vector3 p1,
            Vector3 p2,
            int coils,
            float startRadius,
            float endRadius,
            float radiusScale,
            int resolutionPerCoil,
            float phaseOffsetDeg = 0f
        )
        {
            if (p1 == p2 || coils <= 0) return;

            Quaternion rot = Quaternion.LookRotation(p2 - p1);
            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;

            Vector3 prev = p1;
            int segments = coils * resolutionPerCoil;
            float offsetRad = phaseOffsetDeg * Mathf.Deg2Rad;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float rad = Mathf.Lerp(startRadius, endRadius, t);
                float angle = t * coils * Mathf.PI * 2f + offsetRad;

                Vector3 offset = rad * radiusScale *
                    (up * Mathf.Sin(angle) + right * Mathf.Cos(angle));

                Vector3 point = Vector3.Lerp(p1, p2, t) + offset;

                Handles.DrawLine(prev, point);
                prev = point;
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
                float rad = sphere.radius *
                    Mathf.Max(
                        c.transform.lossyScale.x,
                        c.transform.lossyScale.y,
                        c.transform.lossyScale.z
                    );

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

        private void DrawWireCube(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 half = size * 0.5f;

            Vector3[] points = new Vector3[8]
            {
                center + rotation * new Vector3(-half.x, -half.y, -half.z),
                center + rotation * new Vector3(-half.x, -half.y, half.z),
                center + rotation * new Vector3(-half.x, half.y, -half.z),
                center + rotation * new Vector3(-half.x, half.y, half.z),
                center + rotation * new Vector3(half.x, -half.y, -half.z),
                center + rotation * new Vector3(half.x, -half.y, half.z),
                center + rotation * new Vector3(half.x, half.y, -half.z),
                center + rotation * new Vector3(half.x, half.y, half.z)
            };

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);
            Handles.DrawLine(points[6], points[7]);
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

        private void DrawGizmoHandles(Rect previewRect)
        {
            if (_currentVehicle == null) return;

            Matrix4x4 old = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                foreach (var kvp in _gizmoHandles)
                {
                    GizmoHandle h = kvp.Value;
                    if (!IsGizmoTypeEnabled(h.type)) continue;

                    Vector3 screen = _previewUtility.camera.WorldToScreenPoint(h.position);
                    if (screen.z <= 0) continue;

                    screen.y = previewRect.height - screen.y;

                    float scale = Mathf.Clamp(12f / screen.z, 0.7f, 2f);

                    Rect rect = new(
                        screen.x - 10 * scale + previewRect.x,
                        screen.y - 10 * scale + previewRect.y,
                        20 * scale,
                        20 * scale
                    );

                    EditorGUI.DrawRect(rect, new Color(h.color.r, h.color.g, h.color.b, 0.9f));

                    HandleGizmoInteraction(h, rect, previewRect);
                }
            }
            finally
            {
                Handles.matrix = old;
            }
        }

        private void HandleGizmoInteraction(
            GizmoHandle handle,
            Rect rect,
            Rect previewRect
        )
        {
            Event e = Event.current;

            if (!previewRect.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDown
                    when rect.Contains(e.mousePosition) && e.button == 0:
                    _selectedGizmo = handle;
                    _isDraggingGizmo = true;
                    e.Use();
                    break;

                case EventType.MouseDrag
                    when _isDraggingGizmo && _selectedGizmo == handle && e.button == 0:
                    Plane plane = new(-_previewUtility.camera.transform.forward, handle.position);

                    Vector3 mousePrev = e.mousePosition - e.delta;
                    Vector3 mouseCurr = e.mousePosition;

                    Ray prevRay = _previewUtility.camera.ScreenPointToRay(
                        new Vector3(
                            mousePrev.x,
                            previewRect.height - mousePrev.y,
                            0
                        )
                    );
                    Ray currRay = _previewUtility.camera.ScreenPointToRay(
                        new Vector3(
                            mouseCurr.x,
                            previewRect.height - mouseCurr.y,
                            0
                        )
                    );

                    if (plane.Raycast(prevRay, out float d1) &&
                        plane.Raycast(currRay, out float d2))
                    {
                        Vector3 delta = currRay.GetPoint(d2) - prevRay.GetPoint(d1);
                        Vector3 newPos = handle.position + delta;

                        handle.position = newPos;
                        handle.onPositionChanged?.Invoke(newPos);
                    }

                    e.Use();
                    break;

                case EventType.MouseUp
                    when _isDraggingGizmo && e.button == 0:
                    _isDraggingGizmo = false;
                    _selectedGizmo = null;
                    e.Use();
                    break;
            }
        }

        private bool IsGizmoTypeEnabled(GizmoType t) =>
            t switch
            {
                GizmoType.Wheel => _wheelsGizmo,
                GizmoType.Collider => _collidersGizmo,
                GizmoType.Suspension => _suspensionGizmo,
                _ => true
            };

        public void Cleanup()
        {
            if (_previewInstance != null)
            {
                Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _gizmoHandles.Clear();
            _selectedGizmo = null;

            if (_defaultMaterial != null)
            {
                Object.DestroyImmediate(_defaultMaterial);
                _defaultMaterial = null;
            }

            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            _currentVehicle = null;
        }
    }
}
