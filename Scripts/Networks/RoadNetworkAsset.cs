using UnityEngine;

[CreateAssetMenu(menuName = "UVS/Networks/Road Network", fileName = "RoadNetwork")]
public class RoadNetworkAsset : PathGraphBase
{
    [Header("Road Defaults")]
    public float laneWidth = 3.5f;
    public int lanesPerDirection = 1;
    public float defaultSpeedLimit = 15f;
}
