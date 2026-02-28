using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class LowriderController : MonoBehaviour
{
    [Header("Config")]
    public VehicleConfig config;

    [Header("Optional References")]
    public WheelCollider[] wheelColliders;
    public Transform[] springVisuals; // Optional: assign coiled spring meshes

    [Header("Input Actions")]
    [SerializeField] private InputAction hopAction;
    [SerializeField] private InputAction frontAction;
    [SerializeField] private InputAction rearAction;
    [SerializeField] private InputAction leftAction;
    [SerializeField] private InputAction rightAction;
    [SerializeField] private InputAction slamAction;

    private Rigidbody rb;
    private WheelCollider[] wheels;
    private WheelCollider[] frontWheels;
    private WheelCollider[] rearWheels;
    private WheelCollider[] leftWheels;
    private WheelCollider[] rightWheels;

    private float danceTimer;
    private float hopBudget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = (wheelColliders != null && wheelColliders.Length > 0)
            ? wheelColliders
            : GetComponentsInChildren<WheelCollider>();

        if (config == null || !config.lowrider.enableHydraulics)
        {
            enabled = false;
            return;
        }

        rb.centerOfMass = new Vector3(0, -0.8f, 0);

        ResolveInputProfile();
        EnableActions();
        CacheWheelGroups();

        hopBudget = config.lowrider.maxHopImpulsePerSecond;
    }

    private void OnDestroy()
    {
        DisableActions();
    }

    private void FixedUpdate()
    {
        if (config == null || !config.lowrider.enableHydraulics) return;

        var l = config.lowrider;
        hopBudget = Mathf.MoveTowards(hopBudget, l.maxHopImpulsePerSecond, l.maxHopImpulsePerSecond * Time.fixedDeltaTime);

        if (hopAction?.WasPressedThisFrame() == true)
            HopAll(l.hopForce);

        if (slamAction?.WasPressedThisFrame() == true)
            HopAll(-l.slamForce);

        float frontTarget = (frontAction?.IsPressed() == true) ? l.maxLiftHeight : 0f;
        float rearTarget = (rearAction?.IsPressed() == true) ? l.maxLiftHeight : 0f;
        ApplyLiftSpring(frontWheels, frontTarget, l);
        ApplyLiftSpring(rearWheels, rearTarget, l);

        ApplyTilt(l);

        if (l.enableDanceMode)
        {
            danceTimer += Time.fixedDeltaTime;
            float wave = Mathf.Sin(danceTimer * l.danceSpeed * 2f * Mathf.PI) * l.bounceAmplitude;
            HopAll(wave * l.hopForce * 0.6f);
        }

        ClampVerticalVelocity(l.maxVerticalVelocity);
    }

    private void CacheWheelGroups()
    {
        if (wheels == null || wheels.Length == 0)
        {
            frontWheels = rearWheels = leftWheels = rightWheels = new WheelCollider[0];
            return;
        }

        frontWheels = System.Array.FindAll(wheels, w => w != null && w.transform.localPosition.z > 0f);
        rearWheels = System.Array.FindAll(wheels, w => w != null && w.transform.localPosition.z <= 0f);
        leftWheels = System.Array.FindAll(wheels, w => w != null && w.transform.localPosition.x < 0f);
        rightWheels = System.Array.FindAll(wheels, w => w != null && w.transform.localPosition.x >= 0f);
    }

    private void HopAll(float force)
    {
        if (wheels == null || wheels.Length == 0) return;
        foreach (var wheel in wheels)
        {
            if (wheel == null) continue;
            ApplyImpulseAtWheel(wheel, force);
        }
    }

    private void ApplyImpulseAtWheel(WheelCollider wheel, float force)
    {
        float cost = Mathf.Abs(force);
        if (hopBudget < cost) return;
        hopBudget -= cost;
        rb.AddForceAtPosition(Vector3.up * force, wheel.transform.position, ForceMode.Impulse);
    }

    private void ApplyLiftSpring(WheelCollider[] targetWheels, float targetHeight, VehicleConfig.Lowrider l)
    {
        if (targetWheels == null || targetWheels.Length == 0) return;

        foreach (var wheel in targetWheels)
        {
            if (wheel == null) continue;
            Vector3 pos = wheel.transform.position;
            float velY = rb.GetPointVelocity(pos).y;
            float force = (l.liftSpring * targetHeight) - (l.liftDamping * velY);
            rb.AddForceAtPosition(Vector3.up * force, pos, ForceMode.Force);
        }
    }

    private void ApplyTilt(VehicleConfig.Lowrider l)
    {
        float roll = SignedAngleAroundAxis(transform.up, Vector3.up, transform.forward);
        float pitch = SignedAngleAroundAxis(transform.up, Vector3.up, transform.right);

        if (leftAction?.IsPressed() == true && roll > -l.maxTiltAngle)
            rb.AddTorque(1000f * l.tiltSpeed * transform.forward);
        else if (rightAction?.IsPressed() == true && roll < l.maxTiltAngle)
            rb.AddTorque(1000f * -l.tiltSpeed * transform.forward);

        if (frontAction?.IsPressed() == true && pitch < l.maxTiltAngle)
            rb.AddTorque(800f * l.tiltSpeed * transform.right);
        else if (rearAction?.IsPressed() == true && pitch > -l.maxTiltAngle)
            rb.AddTorque(800f * -l.tiltSpeed * transform.right);

        // Restoring torque if exceeding limits
        if (Mathf.Abs(roll) > l.maxTiltAngle)
            rb.AddTorque(transform.forward * -Mathf.Sign(roll) * l.tiltSpeed * 1200f);
        if (Mathf.Abs(pitch) > l.maxTiltAngle)
            rb.AddTorque(transform.right * -Mathf.Sign(pitch) * l.tiltSpeed * 1000f);
    }

    private void ClampVerticalVelocity(float maxVel)
    {
        if (maxVel <= 0f) return;
        var v = rb.linearVelocity;
        if (v.y > maxVel)
            rb.linearVelocity = new Vector3(v.x, maxVel, v.z);
    }

    private static float SignedAngleAroundAxis(Vector3 from, Vector3 to, Vector3 axis)
    {
        float angle = Vector3.SignedAngle(from, to, axis);
        return Mathf.DeltaAngle(0f, angle);
    }

    private void ResolveInputProfile()
    {
        var profile = VehicleInputResolver.GetProfile(config);
        if (profile == null) return;

        hopAction = VehicleInputResolver.Resolve(hopAction, profile.hop);
        frontAction = VehicleInputResolver.Resolve(frontAction, profile.frontLift);
        rearAction = VehicleInputResolver.Resolve(rearAction, profile.rearLift);
        leftAction = VehicleInputResolver.Resolve(leftAction, profile.leftTilt);
        rightAction = VehicleInputResolver.Resolve(rightAction, profile.rightTilt);
        slamAction = VehicleInputResolver.Resolve(slamAction, profile.slam);
    }

    private void EnableActions()
    {
        hopAction?.Enable();
        frontAction?.Enable();
        rearAction?.Enable();
        leftAction?.Enable();
        rightAction?.Enable();
        slamAction?.Enable();
    }

    private void DisableActions()
    {
        hopAction?.Disable();
        frontAction?.Disable();
        rearAction?.Disable();
        leftAction?.Disable();
        rightAction?.Disable();
        slamAction?.Disable();
    }
}
