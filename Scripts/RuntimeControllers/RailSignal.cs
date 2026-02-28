using UnityEngine;

public class RailSignal : MonoBehaviour
{
    public bool stop = true;
    public float stopDistance = 8f;

    public bool ShouldStop(Vector3 trainPosition)
    {
        if (!stop) return false;
        return Vector3.Distance(trainPosition, transform.position) <= stopDistance;
    }
}
