using System.Collections.Generic;
using UnityEngine;

public class TrafficManager : MonoBehaviour
{
    public RoadNetworkAsset roadNetwork;
    public List<GameObject> vehiclePrefabs = new();
    public int spawnCount = 5;
    public float spawnSpacing = 12f;
    public bool autoSpawnOnStart = true;

    private readonly List<GameObject> _spawned = new();

    private void Start()
    {
        if (autoSpawnOnStart)
            SpawnTraffic();
    }

    public void SpawnTraffic()
    {
        ClearTraffic();
        if (roadNetwork == null || vehiclePrefabs.Count == 0) return;

        var points = new List<Vector3>();
        foreach (var edge in roadNetwork.edges)
        {
            if (edge == null || edge.spline == null) continue;
            var route = new PathGraphBase.Route { edgeIds = new List<int> { edge.id } };
            roadNetwork.BuildRouteSamples(route);
            points.AddRange(route.sampledPoints);
        }

        if (points.Count == 0) return;
        int count = Mathf.Min(spawnCount, points.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(i * spawnSpacing), 0, points.Count - 1);
            var prefab = vehiclePrefabs[i % vehiclePrefabs.Count];
            var go = Instantiate(prefab, points[index], Quaternion.identity, transform);
            _spawned.Add(go);

            var ai = go.GetComponent<VehicleAIController>();
            if (ai == null) ai = go.AddComponent<VehicleAIController>();
            ai.roadNetwork = roadNetwork;
        }
    }

    public void ClearTraffic()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }
}
