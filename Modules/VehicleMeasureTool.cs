using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UVS.Modules
{
    public static class VehicleMeasurementModule
    {
        public static void Measure(VehicleConfig config, List<Transform> wheelTransforms)
        {
            if (config == null) throw new System.ArgumentNullException(nameof(config));
            if (config.prefabReference == null)
                throw new System.InvalidOperationException("config.prefabReference must be set before measuring.");

            var root = config.prefabReference.transform;

            // 1) overall renderer bounds
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                config.measurements.length    = bounds.size.z;
                config.measurements.width     = bounds.size.x;
                config.measurements.height    = bounds.size.y;
                config.measurements.centerOfMassEstimate = bounds.center - root.position;
            }

            // 2) wheel-based calculations
            if (wheelTransforms != null && wheelTransforms.Count >= 2)
            {
                var localPos = new List<Vector3>();
                foreach (var w in wheelTransforms)
                    localPos.Add(root.InverseTransformPoint(w.position));

                localPos.Sort((a, b) => a.z.CompareTo(b.z));
                float rearZ  = localPos[0].z;
                float frontZ = localPos[localPos.Count - 1].z;
                config.measurements.wheelbase = frontZ - rearZ;

                // track widths
                var front = localPos.FindAll(p => Mathf.Approximately(p.z, frontZ));
                if (front.Count >= 2)
                    config.measurements.frontTrackWidth = MaxX(front) - MinX(front);

                var rear = localPos.FindAll(p => Mathf.Approximately(p.z, rearZ));
                if (rear.Count >= 2)
                    config.measurements.rearTrackWidth = MaxX(rear) - MinX(rear);
            }

            // 3) ground clearance & ride height
            float lowestY = float.MaxValue;
            float pivotY  = root.position.y;
            foreach (var r in renderers)
            {
                float bottom = r.bounds.center.y - r.bounds.extents.y;
                lowestY = Mathf.Min(lowestY, bottom);
            }
            config.measurements.groundClearance = Mathf.Max(0f, lowestY - pivotY);
            config.measurements.rideHeight      = pivotY - lowestY;

            // 4) persist
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static float MinX(List<Vector3> pts)
        {
            float m = float.MaxValue;
            foreach (var p in pts) if (p.x < m) m = p.x;
            return m;
        }

        private static float MaxX(List<Vector3> pts)
        {
            float m = float.MinValue;
            foreach (var p in pts) if (p.x > m) m = p.x;
            return m;
        }
    }
}
