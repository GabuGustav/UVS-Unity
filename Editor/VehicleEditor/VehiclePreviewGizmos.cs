using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UVS.Editor.Core
{
    public sealed class VehiclePreviewGizmos
    {
        private readonly Dictionary<string, GizmoHandle> _handles = new();
        private GizmoHandle _selected;
        private bool _isDragging;

        private bool _showWheels = true;
        private bool _showColliders = true;
        private bool _showSuspension = true;
        private bool _showSeats = true;

        public bool IsDragging => _isDragging;

        public void Toggle(string id, bool value)
        {
            switch (id)
            {
                case "wheels": _showWheels = value; break;
                case "colliders": _showColliders = value; break;
                case "suspension": _showSuspension = value; break;
                case "seats": _showSeats = value; break;
            }
        }

        public void Rebuild(GameObject instance)
        {
            _handles.Clear();
            if (instance == null) return;

            // Wheel colliders
            var wheels = instance.GetComponentsInChildren<WheelCollider>();
            foreach (var w in wheels)
            {
                w.GetWorldPose(out var pos, out _);
                var handle = new GizmoHandle
                {
                    id = $"wheel_{GetStablePath(w.transform)}",
                    position = pos,
                    type = GizmoType.Wheel,
                    color = Color.yellow,
                    size = Mathf.Clamp(w.radius * 0.5f, 0.08f, 0.4f),
                    targetObject = w,
                    onPositionChanged = newPos =>
                    {
                        Vector3 local = w.transform.InverseTransformPoint(newPos);
                        w.center = local;
                    }
                };
                _handles[handle.id] = handle;
            }

            // Other colliders
            var colliders = instance.GetComponentsInChildren<Collider>();
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
                    onPositionChanged = newPos =>
                    {
                        Vector3 offset = newPos - c.bounds.center;
                        c.transform.position += offset;
                    }
                };
                _handles[handle.id] = handle;
            }
        }

        public void SetSeatHandles(List<GizmoHandle> handles)
        {
            var toRemove = new List<string>();
            foreach (var kv in _handles)
            {
                if (kv.Value.type == GizmoType.Seat)
                    toRemove.Add(kv.Key);
            }

            foreach (var key in toRemove)
                _handles.Remove(key);

            if (handles == null || handles.Count == 0) return;
            foreach (var h in handles)
                _handles[h.id] = h;
        }

        public void Clear()
        {
            _handles.Clear();
            _selected = null;
            _isDragging = false;
        }

        public void DrawOverlay(Camera cam, Rect previewRect, GameObject instance)
        {
            if (instance == null || cam == null) return;

            Matrix4x4 old = Handles.matrix;
            Color colOld = Handles.color;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                if (_showWheels)
                {
                    var wheels = instance.GetComponentsInChildren<WheelCollider>();
                    foreach (var w in wheels) DrawWheelGizmo(w);
                }

                if (_showColliders)
                {
                    var cols = instance.GetComponentsInChildren<Collider>();
                    foreach (var c in cols)
                        if (c is not WheelCollider) DrawColliderGizmo(c);
                }

                if (_showSeats)
                {
                    foreach (var h in _handles.Values)
                    {
                        if (h.type != GizmoType.Seat) continue;
                        Handles.color = h.color;
                        var rot = instance.transform.rotation * Quaternion.Euler(h.euler);
                        DrawSeatShape(h.position, rot, h.size);
                    }
                }
            }
            finally
            {
                Handles.matrix = old;
                Handles.color = colOld;
            }
        }

        public void DrawHandles(Camera cam, Rect previewRect)
        {
            if (cam == null || _handles.Count == 0) return;

            Matrix4x4 old = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            try
            {
                foreach (var kv in _handles)
                {
                    var h = kv.Value;
                    if (!IsGizmoEnabled(h.type)) continue;

                    Vector3 screen = cam.WorldToScreenPoint(h.position);
                    if (screen.z <= 0) continue;

                    screen.y = previewRect.height - screen.y;
                    float scale = Mathf.Clamp(12f / Mathf.Max(1f, screen.z), 0.7f, 2f);

                    Rect r = new(previewRect.x + screen.x - 10f * scale, previewRect.y + screen.y - 10f * scale, 20f * scale, 20f * scale);
                    EditorGUI.DrawRect(r, new Color(h.color.r, h.color.g, h.color.b, 0.95f));

                    HandleGizmoInteraction(h, r, previewRect, cam);
                }
            }
            finally
            {
                Handles.matrix = old;
            }
        }

        private void HandleGizmoInteraction(GizmoHandle handle, Rect rect, Rect previewRect, Camera cam)
        {
            Event e = Event.current;
            if (!previewRect.Contains(e.mousePosition)) return;

            switch (e.type)
            {
                case EventType.MouseDown when rect.Contains(e.mousePosition) && e.button == 0:
                    _selected = handle;
                    _isDragging = true;
                    e.Use();
                    break;

                case EventType.MouseDrag when _isDragging && _selected == handle && e.button == 0:
                    if (handle.type == GizmoType.Seat && e.shift)
                    {
                        handle.euler = new Vector3(
                            handle.euler.x,
                            handle.euler.y + e.delta.x * 0.6f,
                            handle.euler.z);
                        handle.onRotationChanged?.Invoke(handle.euler);
                        e.Use();
                        break;
                    }
                    Plane plane = new(-cam.transform.forward, handle.position);
                    Vector2 mPrev = e.mousePosition - e.delta;
                    Vector2 mCurr = e.mousePosition;

                    Ray prevRay = cam.ScreenPointToRay(new Vector3(mPrev.x, previewRect.height - mPrev.y, 0));
                    Ray currRay = cam.ScreenPointToRay(new Vector3(mCurr.x, previewRect.height - mCurr.y, 0));

                    if (plane.Raycast(prevRay, out float d1) && plane.Raycast(currRay, out float d2))
                    {
                        Vector3 delta = currRay.GetPoint(d2) - prevRay.GetPoint(d1);
                        Vector3 newPos = handle.position + delta;
                        handle.position = newPos;
                        handle.onPositionChanged?.Invoke(newPos);
                    }
                    e.Use();
                    break;

                case EventType.MouseUp when _isDragging && e.button == 0:
                    _isDragging = false;
                    _selected = null;
                    e.Use();
                    break;
            }
        }

        private bool IsGizmoEnabled(GizmoType t)
        {
            return t switch
            {
                GizmoType.Wheel => _showWheels,
                GizmoType.Collider => _showColliders,
                GizmoType.Suspension => _showSuspension,
                GizmoType.Seat => _showSeats,
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

        private void DrawWheelGizmo(WheelCollider w)
        {
            w.GetWorldPose(out var pos, out var rot);
            float rad = w.radius;
            float halfWidth = Mathf.Clamp(rad * 0.3f, 0.05f, 0.35f);
            Vector3 axis = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            Vector3 forward = rot * Vector3.forward;

            Handles.color = Color.yellow;
            Vector3 p1 = pos + axis * halfWidth;
            Vector3 p2 = pos - axis * halfWidth;
            Handles.DrawWireDisc(p1, axis, rad);
            Handles.DrawWireDisc(p2, axis, rad);
            Handles.DrawLine(p1 + up * rad, p2 + up * rad);
            Handles.DrawLine(p1 - up * rad, p2 - up * rad);
            Handles.DrawLine(p1 + forward * rad, p2 + forward * rad);
            Handles.DrawLine(p1 - forward * rad, p2 - forward * rad);

            Handles.color = Color.red;
            DrawWireSphere(pos, 0.05f);

            if (_showSuspension)
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
                DrawWireCapsule(capsule);
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

        private void DrawWireCapsule(CapsuleCollider capsule)
        {
            Transform t = capsule.transform;
            Vector3 center = t.TransformPoint(capsule.center);
            Vector3 scale = t.lossyScale;
            float radiusScale = capsule.direction switch
            {
                0 => Mathf.Max(scale.y, scale.z),
                1 => Mathf.Max(scale.x, scale.z),
                _ => Mathf.Max(scale.x, scale.y)
            };
            float heightScale = capsule.direction switch
            {
                0 => scale.x,
                1 => scale.y,
                _ => scale.z
            };

            float radius = capsule.radius * radiusScale;
            float height = Mathf.Max(capsule.height * heightScale, radius * 2f);

            Vector3 axis = capsule.direction switch
            {
                0 => t.right,
                1 => t.up,
                _ => t.forward
            };

            float cylinder = Mathf.Max(0f, (height * 0.5f) - radius);
            Vector3 top = center + axis * cylinder;
            Vector3 bottom = center - axis * cylinder;

            Handles.DrawWireDisc(top, axis, radius);
            Handles.DrawWireDisc(bottom, axis, radius);

            Vector3 orthoA = Vector3.Cross(axis, t.up);
            if (orthoA.sqrMagnitude < 0.001f)
                orthoA = Vector3.Cross(axis, t.right);
            orthoA.Normalize();
            Vector3 orthoB = Vector3.Cross(axis, orthoA).normalized;

            Handles.DrawLine(top + orthoA * radius, bottom + orthoA * radius);
            Handles.DrawLine(top - orthoA * radius, bottom - orthoA * radius);
            Handles.DrawLine(top + orthoB * radius, bottom + orthoB * radius);
            Handles.DrawLine(top - orthoB * radius, bottom - orthoB * radius);
        }

        private void DrawSeatShape(Vector3 position, Quaternion rotation, float size)
        {
            Matrix4x4 old = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            float baseW = size * 2.2f;
            float baseD = size * 2.0f;
            float baseH = size * 0.5f;

            Vector3 baseCenter = new Vector3(0f, baseH * 0.5f, 0f);
            Handles.DrawWireCube(baseCenter, new Vector3(baseW, baseH, baseD));

            Vector3 backCenter = new Vector3(0f, baseH + size * 0.9f, -baseD * 0.35f);
            Handles.DrawWireCube(backCenter, new Vector3(baseW * 0.9f, size * 1.6f, baseD * 0.35f));

            Vector3 headCenter = new Vector3(0f, baseH + size * 1.7f, -baseD * 0.45f);
            Handles.DrawWireCube(headCenter, new Vector3(baseW * 0.6f, size * 0.5f, baseD * 0.25f));

            Handles.matrix = old;
        }
    }
}
