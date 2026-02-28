using UnityEngine;
using System;

namespace UVS.Editor.Core
{
    public enum GizmoType { Position, Rotation, Wheel, Collider, Suspension, Seat }

    [Serializable]
    public class GizmoHandle
    {
        public string id;
        public Vector3 position;
        public Vector3 euler;
        public GizmoType type;
        public Action<Vector3> onPositionChanged;
        public Action<Vector3> onRotationChanged;
        public float size = 0.12f;
        public Color color = Color.red;
        public System.Object targetObject; // optional reference to controlled object
    }
}
