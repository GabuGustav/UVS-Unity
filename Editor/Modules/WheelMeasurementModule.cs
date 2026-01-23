using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVS.Modules
{
    public  class WheelMeasurementModule
    {
        [Serializable]
        public class WheelData
        {
            public string partPath;
            public float radius;
            public float width;
            public float suspensionDistance;
        }

        public static List<WheelData> Scan(GameObject root)
        {
            var list = new List<WheelData>();
            if (root == null) return list;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform) continue;
                string n = t.name.ToLowerInvariant();

                if (!(n.Contains("wheel") || n.Contains("tire"))) continue;

                var mf = t.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                var mesh = mf.sharedMesh;
                var scale = t.lossyScale;

                // Transform mesh vertices to world space to get true radius
                Vector3 center = mesh.bounds.center;
                float maxRadius = 0f;
                foreach (var v in mesh.vertices)
                {
                    Vector3 worldVertex = Vector3.Scale(v - center, scale); // scaled relative to center
                                                                            // Use XZ plane for radial distance
                    float radius = new Vector2(worldVertex.x, worldVertex.z).magnitude;
                    if (radius > maxRadius) maxRadius = radius;
                }

                float width = Mathf.Abs(mesh.bounds.size.x * scale.x); // full width along X
                float susp = maxRadius * 2f; // placeholder, you can refine later

                list.Add(new WheelData
                {
                    partPath = GetTransformPath(t, root.transform),
                    radius = maxRadius,
                    width = width,
                    suspensionDistance = susp
                });
            }

            return list;
        }

        public static void ApplyColliders(GameObject root, IEnumerable<WheelData> wheels)
        {
            foreach (var wd in wheels)
            {
                var t = FindChildByPath(root.transform, wd.partPath);
                if (t == null) continue;

                var col = t.GetComponent<WheelCollider>() ?? t.gameObject.AddComponent<WheelCollider>();
                col.radius = wd.radius;
                col.suspensionDistance = wd.suspensionDistance;
                col.center = Vector3.zero;
            }
        }

        // --- helpers ---
        private static string GetTransformPath(Transform t, Transform root)
        {
            var names = new List<string>();
            for (var cur = t; cur != null && cur != root; cur = cur.parent)
                names.Add(cur.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static Transform FindChildByPath(Transform root, string path) => root.Find(path);
    }
}
