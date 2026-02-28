using UnityEngine;
using UnityEngine.InputSystem;
using UVS.Shared;

[RequireComponent(typeof(Rigidbody))]
public class VTOLController : MonoBehaviour
{
    public VehicleConfig config;

    [Header("Input Actions")]
    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction pitchAction;
    [SerializeField] private InputAction rollAction;
    [SerializeField] private InputAction yawAction;
    [SerializeField] private InputAction verticalAction; // Optional - ascend/descend (-1..1)

    private Rigidbody rb;
    private bool _actionsEnabled;
    private bool _warnedMissingConfig;
    private const float InputDeadzone = 0.06f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        TryResolveConfig();

        if (config != null)
            ResolveInputProfile();

        EnableActions();
    }

    private void OnDestroy()
    {
        DisableActions();
    }

    private void FixedUpdate()
    {
        if (config == null)
        {
            if (!TryResolveConfig())
            {
                if (!_warnedMissingConfig)
                {
                    _warnedMissingConfig = true;
                    Debug.LogWarning($"[{name}] VTOLController is waiting for VehicleConfig.");
                }
                return;
            }

            ResolveInputProfile();
            EnableActions();
        }

        var a = config.air;
        var v = config.vtol;

        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        // Forward thrust
        float throttle = throttleAction != null ? Mathf.Clamp01(throttleAction.ReadValue<float>()) : 0f;
        if (throttle <= 0.001f)
        {
            if (Keyboard.current != null && Keyboard.current.wKey.isPressed)
                throttle = 1f;
            else if (Gamepad.current != null)
                throttle = Mathf.Clamp01(Gamepad.current.rightTrigger.ReadValue());
        }
        rb.AddForce(transform.forward * (throttle * v.enginePower));

        // Lift + drag from air settings
        float lift = 0.5f * a.airDensity * speed * speed * a.wingArea * a.liftCoefficient;
        rb.AddForce(transform.up * lift);

        if (speed > 0.01f)
        {
            float drag = 0.5f * a.airDensity * speed * speed * a.dragCoefficient;
            rb.AddForce(-velocity.normalized * drag);
        }

        // Vertical thrust / hover
        float verticalInput = verticalAction != null ? verticalAction.ReadValue<float>() : 0f;
        if (v.defaultFlightMode == FlightMode.Hover || v.autoHover)
        {
            float targetHeight = v.hoverHeight + (verticalInput * 2f);
            float error = targetHeight - transform.position.y;
            float liftForce = (error * v.hoverStability) - (rb.linearVelocity.y * v.hoverStability * 0.5f);
            float maxLift = v.enginePower;
            rb.AddForce(Vector3.up * Mathf.Clamp(liftForce, -maxLift, maxLift));
        }
        else
        {
            rb.AddForce(Vector3.up * (verticalInput * v.enginePower));
        }

        // Control torques
        float pitch = ApplyDeadzone(pitchAction != null ? pitchAction.ReadValue<float>() : 0f);
        float roll = ApplyDeadzone(rollAction != null ? rollAction.ReadValue<float>() : 0f);
        float yaw = ApplyDeadzone(yawAction != null ? yawAction.ReadValue<float>() : 0f);

        if (Mathf.Abs(roll) <= 0.001f)
        {
            float keyboardRoll = 0f;
            if (Keyboard.current != null)
                keyboardRoll = (Keyboard.current.eKey.isPressed ? 1f : 0f) - (Keyboard.current.qKey.isPressed ? 1f : 0f);

            float gamepadRoll = 0f;
            if (Gamepad.current != null)
                gamepadRoll = (Gamepad.current.rightShoulder.isPressed ? 1f : 0f) - (Gamepad.current.leftShoulder.isPressed ? 1f : 0f);

            roll = ApplyDeadzone(Mathf.Abs(gamepadRoll) > 0f ? gamepadRoll : keyboardRoll);
        }

        Vector3 torque = Vector3.zero;
        torque += transform.right * (pitch * a.pitchTorque);
        torque += transform.forward * (-roll * a.rollTorque);
        torque += transform.up * (yaw * a.yawTorque);

        rb.AddTorque(torque);
    }

    private void ResolveInputProfile()
    {
        var profile = VehicleInputResolver.GetProfile(config);
        if (profile == null) return;

        throttleAction = VehicleInputResolver.Resolve(throttleAction, profile.throttle);
        pitchAction = VehicleInputResolver.Resolve(pitchAction, profile.pitch);
        rollAction = VehicleInputResolver.Resolve(rollAction, profile.roll);
        yawAction = VehicleInputResolver.Resolve(yawAction, profile.yaw);
        verticalAction = VehicleInputResolver.Resolve(verticalAction, profile.vertical);
    }

    private void EnableActions()
    {
        if (_actionsEnabled) return;
        throttleAction?.Enable();
        pitchAction?.Enable();
        rollAction?.Enable();
        yawAction?.Enable();
        verticalAction?.Enable();
        _actionsEnabled = true;
    }

    private void DisableActions()
    {
        if (!_actionsEnabled) return;
        throttleAction?.Disable();
        pitchAction?.Disable();
        rollAction?.Disable();
        yawAction?.Disable();
        verticalAction?.Disable();
        _actionsEnabled = false;
    }

    private bool TryResolveConfig()
    {
        if (config != null) return true;

        var router = GetComponent<VehiclePhysicsRouter>();
        if (router != null && router.config != null)
        {
            config = router.config;
            return true;
        }

        return false;
    }

    private static float ApplyDeadzone(float value)
    {
        return Mathf.Abs(value) < InputDeadzone ? 0f : value;
    }
}
