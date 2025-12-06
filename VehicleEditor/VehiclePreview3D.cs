using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UVS.Editor.Core
{
    /// <summary>
    /// Enhanced 3D Preview system with interactive gizmos for vehicle editor
    /// </summary>
    public class VehiclePreview3D
    {
        private GameObject _currentVehicle;
        private PreviewRenderUtility _previewUtility;
        private Vector2 _cameraRotation = new Vector2(0, 0);
        private float _cameraDistance = 5f;
        private Vector3 _cameraPivot = Vector3.zero;
        
        // Gizmo states
        private bool _wheelsGizmo = true;
        private bool _collidersGizmo = true;
        private bool _suspensionGizmo = true;
        
        // Interactive gizmo data
        private Dictionary<string, GizmoHandle> _gizmoHandles = new Dictionary<string, GizmoHandle>();
        private GizmoHandle _selectedGizmo;
        private bool _isDraggingGizmo = false;

        // Default material for preview
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
            _previewUtility.camera.nearClipPlane = 0.1f;
            _previewUtility.camera.farClipPlane = 1000f;
            
            // Create default material
            _defaultMaterial = new Material(Shader.Find("Standard"));
            _defaultMaterial.color = Color.gray;
            
            // Add lights to the preview
            _previewUtility.lights[0].intensity = 1f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            
            if (_previewUtility.lights.Length > 1)
            {
                _previewUtility.lights[1].intensity = 0.5f;
                _previewUtility.lights[1].transform.rotation = Quaternion.Euler(30f, -30f, 0f);
            }
        }

        public void SetVehicle(GameObject vehicle)
        {
            _currentVehicle = vehicle;
            if (vehicle != null)
            {
                // Calculate bounds for camera positioning
                Bounds bounds = CalculateBounds(vehicle);
                _cameraPivot = bounds.center;
                _cameraDistance = bounds.size.magnitude * 1.5f;
                
                // Create gizmo handles for wheels and other components
                CreateGizmoHandles();
            }
        }

        private Bounds CalculateBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(gameObject.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void CreateGizmoHandles()
        {
            _gizmoHandles.Clear();
            
            if (_currentVehicle == null) return;

            // Find all wheel colliders and create gizmos for them
            WheelCollider[] wheelColliders = _currentVehicle.GetComponentsInChildren<WheelCollider>();
            foreach (WheelCollider wheel in wheelColliders)
            {
                var handle = new GizmoHandle
                {
                    id = $"wheel_{wheel.name}",
                    position = wheel.transform.position,
                    type = GizmoType.Wheel,
                    color = Color.yellow,
                    size = 0.2f,
                    onPositionChanged = (newPos) => 
                    {
                        Vector3 offset = newPos - wheel.transform.position;
                        wheel.transform.position = newPos;
                        // You might want to update the vehicle configuration here
                    }
                };
                _gizmoHandles.Add(handle.id, handle);
            }

            // Find all colliders and create gizmos for them
            Collider[] colliders = _currentVehicle.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider is WheelCollider) continue; // Skip wheel colliders as we already handled them
                
                var handle = new GizmoHandle
                {
                    id = $"collider_{collider.name}",
                    position = collider.bounds.center,
                    type = GizmoType.Collider,
                    color = Color.blue,
                    size = 0.15f,
                    onPositionChanged = (newPos) => 
                    {
                        Vector3 offset = newPos - collider.bounds.center;
                        collider.transform.position += offset;
                    }
                };
                _gizmoHandles.Add(handle.id, handle);
            }
        }

        public void ToggleGizmo(string gizmoType, bool state)
        {
            switch (gizmoType.ToLower())
            {
                case "wheels":
                    _wheelsGizmo = state;
                    break;
                case "colliders":
                    _collidersGizmo = state;
                    break;
                case "suspension":
                    _suspensionGizmo = state;
                    break;
            }
        }

        public void RenderPreview(Rect previewRect)
        {
            if (_previewUtility == null || previewRect.width <= 0 || previewRect.height <= 0) 
                return;

            // Handle camera controls
            HandleCameraControls(previewRect);

            // Setup camera
            Quaternion rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0);
            Vector3 position = _cameraPivot - rotation * Vector3.forward * _cameraDistance;
            
            _previewUtility.camera.transform.position = position;
            _previewUtility.camera.transform.rotation = rotation;

            // Begin preview
            _previewUtility.BeginPreview(previewRect, GUIStyle.none);
            
            if (_currentVehicle != null)
            {
                // Draw the vehicle
                DrawVehicleRecursive(_currentVehicle.transform);
            }
            else
            {
                // Draw placeholder grid
                DrawGrid();
            }

            _previewUtility.Render();
            
            // Get the preview texture and draw it
            Texture previewTexture = _previewUtility.EndPreview();
            if (previewTexture != null)
            {
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);
            }
            
            // Draw gizmos using Handles (2D overlay)
            if (_currentVehicle != null)
            {
                DrawGizmosOverlay(previewRect);
            }
            
            // Draw gizmo handles on top
            DrawGizmoHandles(previewRect);
        }

        private void HandleCameraControls(Rect previewRect)
        {
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (!previewRect.Contains(e.mousePosition) || _isDraggingGizmo)
                return;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (e.button == 0) // Left click - rotate
                    {
                        _cameraRotation.x += e.delta.x * 0.5f;
                        _cameraRotation.y -= e.delta.y * 0.5f;
                        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y, -90f, 90f);
                        e.Use();
                    }
                    else if (e.button == 1) // Right click - pan
                    {
                        Vector3 right = _previewUtility.camera.transform.right * -e.delta.x * 0.01f * _cameraDistance;
                        Vector3 up = _previewUtility.camera.transform.up * e.delta.y * 0.01f * _cameraDistance;
                        _cameraPivot += right + up;
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    _cameraDistance += e.delta.y * 0.1f * _cameraDistance;
                    _cameraDistance = Mathf.Clamp(_cameraDistance, 1f, 50f);
                    e.Use();
                    break;
            }
        }

        private void DrawVehicleRecursive(Transform transform)
        {
            foreach (Transform child in transform)
            {
                // Draw mesh if it has a renderer
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                Renderer renderer = child.GetComponent<Renderer>();
                
                if (meshFilter != null && meshFilter.sharedMesh != null && renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i] != null ? materials[i] : _defaultMaterial;
                        
                        _previewUtility.DrawMesh(
                            meshFilter.sharedMesh,
                            child.localToWorldMatrix,
                            material,
                            i
                        );
                    }
                }
                
                DrawVehicleRecursive(child);
            }
        }

        private void DrawGizmosOverlay(Rect previewRect)
        {
            // Save the current Handles matrix
            Matrix4x4 originalMatrix = Handles.matrix;
            Color originalColor = Handles.color;

            try
            {
                // Set up Handles for the preview camera
                Handles.matrix = Matrix4x4.identity;
                
                // Draw wheel gizmos
                if (_wheelsGizmo)
                {
                    WheelCollider[] wheels = _currentVehicle.GetComponentsInChildren<WheelCollider>();
                    foreach (WheelCollider wheel in wheels)
                    {
                        DrawWheelGizmo(wheel);
                    }
                }

                // Draw collider gizmos
                if (_collidersGizmo)
                {
                    Collider[] colliders = _currentVehicle.GetComponentsInChildren<Collider>();
                    foreach (Collider collider in colliders)
                    {
                        DrawColliderGizmo(collider);
                    }
                }
            }
            finally
            {
                // Restore original Handles state
                Handles.matrix = originalMatrix;
                Handles.color = originalColor;
            }
        }

        private void DrawWheelGizmo(WheelCollider wheel)
        {
            Vector3 position = wheel.transform.TransformPoint(wheel.center);
            float radius = wheel.radius;
            
            // Draw wheel circle
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(position, wheel.transform.up, radius);
            
            // Draw suspension
            Handles.color = Color.green;
            Vector3 springStart = position;
            Vector3 springEnd = springStart - wheel.transform.up * wheel.suspensionDistance;
            Handles.DrawLine(springStart, springEnd);
        }

        private void DrawColliderGizmo(Collider collider)
        {
            Handles.color = Color.blue;
            
            if (collider is BoxCollider boxCollider)
            {
                Vector3 center = collider.transform.TransformPoint(boxCollider.center);
                Vector3 size = Vector3.Scale(boxCollider.size, collider.transform.lossyScale);
                DrawWireCube(center, size, collider.transform.rotation);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Vector3 center = collider.transform.TransformPoint(sphereCollider.center);
                float radius = sphereCollider.radius * Mathf.Max(
                    collider.transform.lossyScale.x,
                    collider.transform.lossyScale.y,
                    collider.transform.lossyScale.z
                );
                Handles.DrawWireDisc(center, Vector3.up, radius);
                Handles.DrawWireDisc(center, Vector3.right, radius);
                Handles.DrawWireDisc(center, Vector3.forward, radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Vector3 center = collider.transform.TransformPoint(capsuleCollider.center);
                float radius = capsuleCollider.radius;
                
                // Simplified capsule drawing
                Handles.DrawWireDisc(center, Vector3.up, radius);
            }
        }

        private void DrawWireCube(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 halfSize = size * 0.5f;
            
            Vector3[] points = new Vector3[8]
            {
                center + rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
                center + rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
                center + rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z),
                center + rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                center + rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z),
                center + rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z),
                center + rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z)
            };

            // Draw the 12 edges of the cube
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
            // Draw a simple grid using lines
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            float gridSize = 10f;
            int divisions = 20;
            
            for (int i = -divisions; i <= divisions; i++)
            {
                float pos = i * (gridSize / divisions);
                Handles.DrawLine(new Vector3(-gridSize, 0, pos), new Vector3(gridSize, 0, pos));
                Handles.DrawLine(new Vector3(pos, 0, -gridSize), new Vector3(pos, 0, gridSize));
            }
        }

        private void DrawGizmoHandles(Rect previewRect)
        {
            if (_currentVehicle == null) return;

            // Set up Handles for 2D overlay
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                foreach (var kvp in _gizmoHandles)
                {
                    GizmoHandle handle = kvp.Value;
                    
                    // Skip if this type is disabled
                    if (!IsGizmoTypeEnabled(handle.type))
                        continue;

                    // Convert world position to screen position
                    Vector3 screenPos = _previewUtility.camera.WorldToScreenPoint(handle.position);
                    screenPos.y = previewRect.height - screenPos.y; // Flip Y coordinate

                    // Only draw if handle is in front of camera
                    if (screenPos.z > 0)
                    {
                        Rect handleRect = new Rect(
                            screenPos.x - 10 + previewRect.x,
                            screenPos.y - 10 + previewRect.y,
                            20, 20
                        );

                        // Draw handle as a 2D rectangle
                        EditorGUI.DrawRect(handleRect, handle.color);
                        
                        // Handle mouse interaction
                        HandleGizmoInteraction(handle, handleRect, previewRect);
                    }
                }
            }
            finally
            {
                Handles.matrix = originalMatrix;
            }
        }

        private void HandleGizmoInteraction(GizmoHandle handle, Rect handleRect, Rect previewRect)
        {
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (!previewRect.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (handleRect.Contains(e.mousePosition) && e.button == 0)
                    {
                        _selectedGizmo = handle;
                        _isDraggingGizmo = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDraggingGizmo && _selectedGizmo == handle && e.button == 0)
                    {
                        // Convert mouse movement to world space movement
                        Vector3 delta = _previewUtility.camera.transform.right * e.delta.x * 0.01f +
                                       _previewUtility.camera.transform.up * -e.delta.y * 0.01f;
                        
                        Vector3 newPosition = handle.position + delta;
                        handle.position = newPosition;
                        handle.onPositionChanged?.Invoke(newPosition);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDraggingGizmo && e.button == 0)
                    {
                        _isDraggingGizmo = false;
                        _selectedGizmo = null;
                        e.Use();
                    }
                    break;
            }
        }

        private bool IsGizmoTypeEnabled(GizmoType type)
        {
            return type switch
            {
                GizmoType.Wheel => _wheelsGizmo,
                GizmoType.Collider => _collidersGizmo,
                GizmoType.Suspension => _suspensionGizmo,
                _ => true
            };
        }

        public void Cleanup()
        {
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