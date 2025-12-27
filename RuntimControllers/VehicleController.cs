using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Config Asset")]
    public VehicleConfig config;

    [Header("Input Actions")]
    public InputAction accelerateAction;   // W / UpArrow / Right trigger
    public InputAction brakeAction;        // S / DownArrow / Left trigger
    public InputAction steerAction;        // A/D / Left/Right stick X
    public InputAction handbrakeAction;    // Space

    [Header("Tuning")]
    [Range(0.1f, 1f)] public float steeringSmoothness = 0.2f; // 0.2 = smooth, 1 = instant
    public float reverseTorqueMultiplier = 0.6f; // reverse is weaker

    private Rigidbody rb;
    private WheelCollider[] wheels;

    private float currentTorque;
    private float currentSteerAngle;
    private float currentBrakeTorque;

    private float steerInput;
    private float currentSteerLerp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();

        if (config == null)
        {
            enabled = false;
            Debug.LogError("VehicleController: No VehicleConfig assigned!");
            return;
        }

        // Apply body mass and CoM
        rb.mass = config.body.mass;
        rb.centerOfMass = config.body.centerOfMassOffset;

        // Apply suspension settings
        var susp = config.suspension;
        foreach (var wheel in wheels)
        {
            var spring = wheel.suspensionSpring;
            spring.spring = susp.springStiffness;
            spring.damper = susp.damperStiffness;
            wheel.suspensionSpring = spring;
            wheel.suspensionDistance = susp.suspensionDistance;
        }

        // Enable inputs
        accelerateAction.Enable();
        brakeAction.Enable();
        steerAction.Enable();
        handbrakeAction.Enable();
    }

    private void OnDestroy()
    {
        accelerateAction.Disable();
        brakeAction.Disable();
        steerAction.Disable();
        handbrakeAction.Disable();
    }

    private void FixedUpdate()
    {
        if (config == null) return;

        float accelInput = accelerateAction.ReadValue<float>();   // 0 to 1
        float brakeInput = brakeAction.ReadValue<float>();        // 0 to 1
        steerInput = steerAction.ReadValue<float>();
        bool handbrake = handbrakeAction.IsPressed();

        var engine = config.engine;
        var brakes = config.brakes;

        float maxTorque = engine.horsepower * 100f;

        float speed = rb.linearVelocity.magnitude;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        bool isMovingForward = forwardSpeed > 1f;
        bool isMovingReverse = forwardSpeed < -1f;
        bool isNearlyStopped = Mathf.Abs(forwardSpeed) < 2f;

        // === TORQUE: ONLY WHEN PRESSING ACCEL ===
        currentTorque = 0f; // Default: no torque

        if (accelInput > 0.1f)
        {
            currentTorque = accelInput * maxTorque;
        }
        else if (brakeInput > 0.1f && isNearlyStopped)
        {
            // Reverse only when nearly stopped and pressing brake
            currentTorque = -brakeInput * maxTorque * reverseTorqueMultiplier;
        }

        // === NATURAL SLOWDOWN (Engine Braking + Drag) ===
        float engineBrakeTorque = 300f; // Tune this � higher = stronger slowdown when off throttle
        float dragMultiplier = 0.98f;   // Air + rolling resistance

        if (currentTorque == 0f) // No input = coasting with engine braking
        {
            // Apply small opposite torque (engine braking)
            if (isMovingForward)
                currentTorque = -engineBrakeTorque;
            else if (isMovingReverse)
                currentTorque = engineBrakeTorque;
        }

        // Apply drag (slows down over time)
        rb.linearVelocity *= dragMultiplier;

        // === BRAKING ===
        currentBrakeTorque = handbrake ? brakes.frontDiscDiameter * 150f : brakeInput * brakes.frontDiscDiameter * 80f;

        // If pressing brake while moving, add extra brake torque
        if (brakeInput > 0.1f && !isNearlyStopped)
        {
            currentBrakeTorque += brakeInput * 500f; // Stronger braking
        }

        // Steering smooth lerp
        float targetSteer = steerInput * config.steering.maxSteeringAngle;
        currentSteerLerp = Mathf.Lerp(currentSteerLerp, targetSteer, steeringSmoothness);
        currentSteerAngle = currentSteerLerp;

        // Apply everything
        ApplyDrivetrain(engine.drivetrain);
        ApplySteering();
        ApplyBrakes(handbrake);
    }

    private void ApplyDrivetrain(VehicleConfig.EngineSettings.Drivetrain drivetrain)
    {
        foreach (var wheel in wheels)
        {
            bool powered = false;

            if (drivetrain == VehicleConfig.EngineSettings.Drivetrain.FWD && wheel.transform.localPosition.z > 0) powered = true;
            if (drivetrain == VehicleConfig.EngineSettings.Drivetrain.RWD && wheel.transform.localPosition.z <= 0) powered = true;
            if (drivetrain == VehicleConfig.EngineSettings.Drivetrain.AWD) powered = true;

            wheel.motorTorque = powered ? currentTorque / GetPoweredWheelCount(drivetrain) : 0f;
        }
    }

    private int GetPoweredWheelCount(VehicleConfig.EngineSettings.Drivetrain drivetrain)
    {
        return drivetrain switch
        {
            VehicleConfig.EngineSettings.Drivetrain.AWD => wheels.Length,
            _ => 2
        };
    }

    private void ApplySteering()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.transform.localPosition.z > 0) // front wheels
                wheel.steerAngle = currentSteerAngle;
        }
    }

    private void ApplyBrakes(bool handbrake)
    {
        foreach (var wheel in wheels)
        {
            wheel.brakeTorque = currentBrakeTorque;

            // Handbrake only rear
            if (handbrake && wheel.transform.localPosition.z <= 0)
                wheel.brakeTorque *= 2f;
        }
    }
}