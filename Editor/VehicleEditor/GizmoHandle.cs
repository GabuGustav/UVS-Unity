using UnityEngine;
using System;

namespace UVS.Editor.Core
{
    public enum GizmoType { Position, Rotation, Wheel, Collider, Suspension }

    [Serializable]
    public class GizmoHandle
    {
        public string id;
        public Vector3 position;
        public GizmoType type;
        public Action<Vector3> onPositionChanged;
        public float size = 0.12f;
        public Color color = Color.red;
        public System.Object targetObject; // optional reference to controlled object
    }
}
