using UnityEngine;

public struct VehicleInputState
{
    public float throttle;
    public float brake;
    public float steer;
    public bool handbrake;

    public float pitch;
    public float roll;
    public float yaw;
    public float vertical;

    public float hop;
    public float frontLift;
    public float rearLift;
    public float leftTilt;
    public float rightTilt;
    public float slam;

    public bool recover;

    public static VehicleInputState Zero => default;
}
