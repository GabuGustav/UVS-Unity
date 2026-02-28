using UnityEngine;

public class VehicleSensorRig : MonoBehaviour
{
    public LayerMask obstacleLayers = ~0;
    public float forwardLength = 15f;
    public float forwardSideLength = 12f;
    public float sideLength = 6f;
    public float rearLength = 6f;
    public float sensorHeight = 0.8f;

    public SensorHit forward;
    public SensorHit forwardLeft;
    public SensorHit forwardRight;
    public SensorHit left;
    public SensorHit right;
    public SensorHit rear;

    public enum SensorSlot { Forward, ForwardLeft, ForwardRight, Left, Right, Rear }

    public void Refresh()
    {
        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        forward = Cast(origin, transform.forward, forwardLength);
        forwardLeft = Cast(origin, (transform.forward - transform.right).normalized, forwardSideLength);
        forwardRight = Cast(origin, (transform.forward + transform.right).normalized, forwardSideLength);
        left = Cast(origin, -transform.right, sideLength);
        right = Cast(origin, transform.right, sideLength);
        rear = Cast(origin, -transform.forward, rearLength);
    }

    public SensorHit Get(SensorSlot slot)
    {
        return slot switch
        {
            SensorSlot.Forward => forward,
            SensorSlot.ForwardLeft => forwardLeft,
            SensorSlot.ForwardRight => forwardRight,
            SensorSlot.Left => left,
            SensorSlot.Right => right,
            SensorSlot.Rear => rear,
            _ => forward
        };
    }

    private SensorHit Cast(Vector3 origin, Vector3 dir, float length)
    {
        if (Physics.Raycast(origin, dir, out var hit, length, obstacleLayers, QueryTriggerInteraction.Ignore))
            return new SensorHit(true, hit.distance, hit.point, hit.transform);
        return new SensorHit(false, length, origin + dir * length, null);
    }

    private void FixedUpdate()
    {
        Refresh();
    }

    public readonly struct SensorHit
    {
        public readonly bool hit;
        public readonly float distance;
        public readonly Vector3 point;
        public readonly Transform target;

        public SensorHit(bool hit, float distance, Vector3 point, Transform target)
        {
            this.hit = hit;
            this.distance = distance;
            this.point = point;
            this.target = target;
        }
    }
}
