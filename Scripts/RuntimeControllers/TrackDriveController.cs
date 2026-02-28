using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class TrackDriveController : MonoBehaviour
{
    public VehicleConfig config;

    [Header("Input Actions")]
    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction brakeAction;
    [SerializeField] private InputAction steerAction;
    [SerializeField] private InputAction handbrakeAction;
    [SerializeField] private VehicleInputHub inputHub;

    private Rigidbody rb;
    private WheelCollider[] wheels;
    private readonly List<WheelCollider> leftTracks = new();
    private readonly List<WheelCollider> rightTracks = new();
    private readonly List<WheelCollider> frontSteer = new();

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();

        if (config == null)
        {
            enabled = false;
            Debug.LogError("VehicleConfig missing!");
            return;
        }

        ResolveInputProfile();
        BuildWheelGroups();
        EnableActions();
        if (inputHub == null)
            inputHub = GetComponent<VehicleInputHub>();
    }

    protected virtual void OnDestroy()
    {
        DisableActions();
    }

    private void FixedUpdate()
    {
        var t = config.trackDrive;

        float throttle = 0f;
        float brake = 0f;
        float steer = 0f;
        bool handbrake = false;

        if (inputHub != null && inputHub.HasInput)
        {
            var state = inputHub.Current;
            throttle = state.throttle;
            brake = state.brake;
            steer = state.steer;
            handbrake = state.handbrake;
        }
        else
        {
            throttle = throttleAction?.ReadValue<float>() ?? 0f;
            brake = brakeAction?.ReadValue<float>() ?? 0f;
            steer = steerAction?.ReadValue<float>() ?? 0f;
            handbrake = handbrakeAction?.IsPressed() ?? false;
        }

        if (brake > 0.1f && throttle < 0.1f)
            throttle = -brake;

        float speed = rb.linearVelocity.magnitude;
        float speedFactor = t.maxTrackSpeed > 0f && speed > t.maxTrackSpeed
            ? Mathf.Clamp01(t.maxTrackSpeed / speed)
            : 1f;

        float torque = config.engine.torque * t.trackTorqueMultiplier * speedFactor;
        float diff = steer * t.trackDifferentialStrength;

        float leftInput = Mathf.Clamp(throttle - diff, -1f, 1f);
        float rightInput = Mathf.Clamp(throttle + diff, -1f, 1f);

        ApplyTrackTorque(leftTracks, leftInput * torque);
        ApplyTrackTorque(rightTracks, rightInput * torque);

        float brakeTorque = handbrake ? t.trackBrakeStrength : Mathf.Abs(brake) * t.trackBrakeStrength;
        ApplyBrake(leftTracks, brakeTorque);
        ApplyBrake(rightTracks, brakeTorque);

        float steerAngle = steer * config.steering.maxSteeringAngle * t.steerBlend;
        foreach (var w in frontSteer)
        {
            if (w != null) w.steerAngle = steerAngle;
        }

        if (t.trackDrag > 0f)
            rb.AddForce(-rb.linearVelocity * t.trackDrag, ForceMode.Acceleration);
    }

    private void BuildWheelGroups()
    {
        leftTracks.Clear();
        rightTracks.Clear();
        frontSteer.Clear();

        if (wheels == null) return;

        foreach (var w in wheels)
        {
            if (w == null) continue;
            var role = ResolveRole(w);

            switch (role)
            {
                case VehicleConfig.WheelRole.FrontSteer:
                    frontSteer.Add(w);
                    break;
                case VehicleConfig.WheelRole.TrackLeft:
                    leftTracks.Add(w);
                    break;
                case VehicleConfig.WheelRole.TrackRight:
                    rightTracks.Add(w);
                    break;
            }
        }

        // Fallback if no explicit roles were found
        if (leftTracks.Count == 0 && rightTracks.Count == 0)
        {
            foreach (var w in wheels)
            {
                if (w == null) continue;
                if (w.transform.localPosition.z > 0f)
                    frontSteer.Add(w);
                else if (w.transform.localPosition.x < 0f)
                    leftTracks.Add(w);
                else
                    rightTracks.Add(w);
            }
        }
    }

    private VehicleConfig.WheelRole ResolveRole(WheelCollider w)
    {
        if (config?.wheels == null || config.wheels.Count == 0)
            return FallbackRole(w);

        var localPos = w.transform.localPosition;
        float best = float.MaxValue;
        VehicleConfig.WheelRole role = VehicleConfig.WheelRole.Free;

        foreach (var ws in config.wheels)
        {
            float d = (ws.localPosition - localPos).sqrMagnitude;
            if (d < best)
            {
                best = d;
                role = ws.role;
            }
        }

        return best < 0.25f ? role : FallbackRole(w);
    }

    private static VehicleConfig.WheelRole FallbackRole(WheelCollider w)
    {
        if (w.transform.localPosition.z > 0f)
            return VehicleConfig.WheelRole.FrontSteer;
        return w.transform.localPosition.x < 0f ? VehicleConfig.WheelRole.TrackLeft : VehicleConfig.WheelRole.TrackRight;
    }

    private void ApplyTrackTorque(List<WheelCollider> wheels, float torque)
    {
        foreach (var w in wheels)
        {
            if (w == null) continue;
            w.motorTorque = torque;
        }
    }

    private void ApplyBrake(List<WheelCollider> wheels, float brakeTorque)
    {
        foreach (var w in wheels)
        {
            if (w == null) continue;
            w.brakeTorque = brakeTorque;
        }
    }

    private void ResolveInputProfile()
    {
        var profile = VehicleInputResolver.GetProfile(config);
        if (profile == null) return;

        throttleAction = VehicleInputResolver.Resolve(throttleAction, profile.throttle);
        brakeAction = VehicleInputResolver.Resolve(brakeAction, profile.brake);
        steerAction = VehicleInputResolver.Resolve(steerAction, profile.steer);
        handbrakeAction = VehicleInputResolver.Resolve(handbrakeAction, profile.handbrake);
    }

    private void EnableActions()
    {
        throttleAction?.Enable();
        brakeAction?.Enable();
        steerAction?.Enable();
        handbrakeAction?.Enable();
    }

    private void DisableActions()
    {
        throttleAction?.Disable();
        brakeAction?.Disable();
        steerAction?.Disable();
        handbrakeAction?.Disable();
    }
}
