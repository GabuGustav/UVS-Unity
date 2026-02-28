using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteAlways]
public class RailTrackBuilder : MonoBehaviour
{
    public SplineContainer spline;
    public GameObject trackPrefab;
    public GameObject sleeperPrefab;
    public float trackSpacing = 2.5f;
    public float sleeperSpacing = 0.6f;
    public bool autoTrackSpacingFromPrefab = true;
    public float trackSpacingMultiplier = 1f;
    public bool autoSleeperSpacingFromPrefab = true;
    public float sleeperSpacingMultiplier = 1f;
    public bool alignToSpline = true;
    public bool conformToTerrain = true;
    public LayerMask terrainMask = ~0;
    public float rayStartHeight = 100f;
    public float rayDistance = 500f;
    public float terrainOffset = 0f;
    public bool alignToTerrainNormal = false;

    [ContextMenu("Rebuild Rail")]
    public void Rebuild()
    {
        if (spline == null) return;
        ClearChildren();

        int misses = 0;
        if (trackPrefab != null)
        {
            float resolved = ResolveSpacing(trackPrefab, trackSpacing, autoTrackSpacingFromPrefab, trackSpacingMultiplier);
            BuildPrefabAlongSpline(trackPrefab, resolved, ref misses);
        }

        if (sleeperPrefab != null)
        {
            float resolved = ResolveSpacing(sleeperPrefab, sleeperSpacing, autoSleeperSpacingFromPrefab, sleeperSpacingMultiplier);
            BuildPrefabAlongSpline(sleeperPrefab, resolved, ref misses);
        }

        if (conformToTerrain && misses > 0)
            Debug.LogWarning($"[RailTrackBuilder] Terrain raycast missed {misses} sample points for '{name}'. Used spline height fallback.");
    }

    private static float ResolveSpacing(GameObject prefab, float configuredSpacing, bool autoFromPrefab, float multiplier)
    {
        float fallback = Mathf.Max(0.1f, configuredSpacing);
        if (!autoFromPrefab || prefab == null)
            return fallback;

        if (!TryEstimatePrefabFootprint(prefab, out float footprint))
            return fallback;

        return Mathf.Max(0.1f, footprint * Mathf.Max(0.01f, multiplier));
    }

    private static bool TryEstimatePrefabFootprint(GameObject prefab, out float footprint)
    {
        footprint = 0f;
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.01f)
                return true;
        }

        var colliders = prefab.GetComponentsInChildren<Collider>(true);
        if (colliders != null && colliders.Length > 0)
        {
            var bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.01f)
                return true;
        }

        return false;
    }

    private void BuildPrefabAlongSpline(GameObject prefab, float spacing, ref int terrainMisses)
    {
        float length = spline.Spline.GetLength();
        int count = Mathf.Max(2, Mathf.CeilToInt(length / Mathf.Max(0.1f, spacing)));

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            Vector3 localPos = spline.Spline.EvaluatePosition(t);
            Vector3 pos = spline.transform.TransformPoint(localPos);
            Quaternion rot = spline.transform.rotation;

            if (alignToSpline)
            {
                float3 tangent = spline.Spline.EvaluateTangent(t);
                Vector3 worldTangent = spline.transform.TransformDirection((Vector3)math.normalize(tangent));
                if (worldTangent.sqrMagnitude > 0.001f)
                    rot = Quaternion.LookRotation(worldTangent, spline.transform.up);
            }

            ApplyTerrainConform(ref pos, ref rot, out bool hitTerrain);
            if (conformToTerrain && !hitTerrain)
                terrainMisses++;

            var go = Instantiate(prefab, pos, rot, transform);
            go.name = $"{prefab.name}_{i:000}";
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    private void ApplyTerrainConform(ref Vector3 position, ref Quaternion rotation, out bool hitTerrain)
    {
        hitTerrain = false;
        if (!conformToTerrain)
            return;

        Vector3 rayOrigin = position + Vector3.up * Mathf.Max(0.1f, rayStartHeight);
        if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, Mathf.Max(0.1f, rayDistance), terrainMask, QueryTriggerInteraction.Ignore))
            return;

        hitTerrain = true;
        position = hit.point + hit.normal * terrainOffset;

        if (!alignToTerrainNormal)
            return;

        Vector3 forward = rotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, hit.normal);
        if (projectedForward.sqrMagnitude > 0.0001f)
            rotation = Quaternion.LookRotation(projectedForward.normalized, hit.normal);
    }
}
