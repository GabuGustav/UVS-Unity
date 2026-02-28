using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "UVS/Input Profile", fileName = "VehicleInputProfile")]
public class VehicleInputProfile : ScriptableObject
{
    public const string DefaultResourcePath = "VehicleInputProfile_Default";

    [Header("Land / Track / Boat")]
    public InputActionReference throttle;
    public InputActionReference brake;
    public InputActionReference steer;
    public InputActionReference handbrake;

    [Header("Air / VTOL")]
    public InputActionReference pitch;
    public InputActionReference roll;
    public InputActionReference yaw;
    public InputActionReference vertical;

    [Header("Lowrider")]
    public InputActionReference hop;
    public InputActionReference frontLift;
    public InputActionReference rearLift;
    public InputActionReference leftTilt;
    public InputActionReference rightTilt;
    public InputActionReference slam;

    [Header("Assist")]
    public InputActionReference recover;

    public static VehicleInputProfile GetDefault()
    {
        return Resources.Load<VehicleInputProfile>(DefaultResourcePath);
    }
}
