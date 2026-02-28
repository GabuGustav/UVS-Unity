using UnityEngine;

[CreateAssetMenu(menuName = "UVS/Networks/Rail Network", fileName = "RailNetwork")]
public class RailNetworkAsset : PathGraphBase
{
    [Header("Rail Defaults")]
    public float trackGauge = 1.435f;
    public float defaultSpeedLimit = 22f;
}
