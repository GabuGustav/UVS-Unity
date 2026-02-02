using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Config Asset")]
    public VehicleConfig config;

    [Header("Input Actions")]
    public InputAction accelerateAction;
    public InputAction brakeAction;
    public InputAction steerAction;
    public InputAction handbrakeAction;

    [Header("Tuning")]
    [Range(0.1f, 1f)] public float steeringSmoothness = 0.2f;
    public float reverseTorqueMultiplier = 0.7f;
    public float torqueMultiplier = 10f;         // Increased default - crank higher if needed (15-20)
    public float brakeStrengthMultiplier = 200f; // Increased for stronger brakes
    public float handbrakeMultiplier = 400f;     // Stronger handbrake
    public float holdingBrakeTorque = 100f;      // Tune this for wheel stop (50-200)

    [Header("Transmission")]
    public bool isAutomatic = true;
    public int Gear { get; private set; } = 1; // 0 = R, 1 = N, 2+ = forward
    private float[] gearRatios;
    public float engineRPM;
    private float wheelRPM;

    private Rigidbody rb;
    private WheelCollider[] wheels;
    private float currentMotorTorque;
    private float currentBrakeTorque;
    private float currentSteerAngle;
    private float steerInput;
    private float currentSteerLerp;
    private float shiftTimer;
    private const float SHIFT_COOLDOWN = 0.35f;
    private const float RPM_LERP = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();
        if (config == null)
        {
            enabled = false;
            Debug.LogError("No VehicleConfig!");
            return;
        }

        rb.mass = config.body.mass;
        rb.centerOfMass = config.body.centerOfMassOffset;

        var susp = config.suspension;
        foreach (var w in wheels)
        {
            var spring = w.suspensionSpring;
            spring.spring = susp.springStiffness;
            spring.damper = susp.damperStiffness;
            w.suspensionSpring = spring;
            w.suspensionDistance = susp.suspensionDistance;
        }

        BuildGearRatios();

        accelerateAction.Enable();
        brakeAction.Enable();
        steerAction.Enable();
        handbrakeAction.Enable();
    }

    private void BuildGearRatios()
    {
        var trans = config.transmission;
        gearRatios = new float[trans.gearCount + 2];
        gearRatios[0] = trans.reverseGearRatio;
        gearRatios[1] = 0f;
        for (int i = 0; i < trans.gearCount; i++)
        {
            gearRatios[i + 2] = (i < trans.gearRatios.Length) ? trans.gearRatios[i] : 1f;
        }
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

        float accel = accelerateAction.ReadValue<float>();
        float brake = brakeAction.ReadValue<float>();
        steerInput = steerAction.ReadValue<float>();
        bool handbrake = handbrakeAction.IsPressed();

        var eng = config.engine;
        var brk = config.brakes;
        var trans = config.transmission;
        var steer = config.steering;

        float fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool nearlyStopped = Mathf.Abs(fwdSpeed) < 1.5f;
        bool movingFwd = fwdSpeed > 0.5f;
        bool movingRev = fwdSpeed < -0.5f;

        bool wantFwd = accel > 0.1f;
        bool wantRev = brake > 0.1f && !wantFwd;

        // Gear shift for direction when stopped
        if (nearlyStopped)
        {
            if (wantRev && Gear != 0)
            {
                Gear = 0;
                shiftTimer = SHIFT_COOLDOWN;
            }
            else if (wantFwd && Gear < 2)
            {
                Gear = 2;
                shiftTimer = SHIFT_COOLDOWN;
            }
        }

        // Torque (positive always, sign from gear)
        currentMotorTorque = 0f;
        
        // Only apply motor torque if NOT actively braking or handbraking
        bool isBraking = (brake > 0.1f && !wantRev) || handbrake;
        
        if (!isBraking)
        {
            if (wantFwd && Gear >= 2)
            {
                currentMotorTorque = accel * eng.torque * torqueMultiplier;
            }
            else if (wantRev && Gear == 0)
            {
                currentMotorTorque = brake * eng.torque * torqueMultiplier * reverseTorqueMultiplier;
            }
        }

        // Engine braking / drag
        float engBrake = eng.torque * 0.08f;
        float drag = 0.995f;
        if (currentMotorTorque == 0f)
        {
            currentMotorTorque = movingFwd ? -engBrake : (movingRev ? engBrake : 0f);
        }
        rb.linearVelocity *= drag;

        // Brakes - Apply brake torque when braking (but not when reversing)
        currentBrakeTorque = 0f;
        if (brake > 0.1f && !wantRev)
        {
            currentBrakeTorque = brake * brk.frontDiscDiameter * brakeStrengthMultiplier;
        }
        if (handbrake)
        {
            currentBrakeTorque = Mathf.Max(currentBrakeTorque, brk.rearDiscDiameter * handbrakeMultiplier);
        }

        // Stronger holding brake when stopped or handbrake
        if ((Mathf.Abs(fwdSpeed) < 0.6f && accel < 0.05f && brake < 0.05f) || handbrake)
        {
            currentBrakeTorque = Mathf.Max(currentBrakeTorque, holdingBrakeTorque);
        }

        // Steering
        float targetSteer = steerInput * steer.maxSteeringAngle;
        currentSteerLerp = Mathf.Lerp(currentSteerLerp, targetSteer, steeringSmoothness);
        currentSteerAngle = currentSteerLerp;

        // RPM calc & auto shift
        CalculateRPM(eng.drivetrain);
        if (isAutomatic && Gear >= 2)
        {
            HandleAutoShift(trans);
        }

        ApplyMotorTorque(eng.drivetrain);
        ApplySteering();
        ApplyBrakes(handbrake, brk.brakeBias);

        // Debug velocity (m/s)
        Debug.Log("Car Velocity: " + rb.linearVelocity.magnitude.ToString("F2") + " m/s");
    }

    private void CalculateRPM(VehicleConfig.EngineSettings.Drivetrain dt)
    {
        float avgRPM = 0f;
        int count = 0;
        foreach (var w in wheels)
        {
            if (IsPowered(w, dt))
            {
                avgRPM += w.rpm;
                count++;
            }
        }
        wheelRPM = count > 0 ? avgRPM / count : 0f;

        float gearR = Mathf.Abs(GetGearRatio());
        float finalR = config.transmission.finalDriveRatio > 0 ? config.transmission.finalDriveRatio : 3.8f;
        engineRPM = Mathf.Lerp(engineRPM, Mathf.Abs(wheelRPM) * gearR * finalR, Time.fixedDeltaTime * RPM_LERP);

        if (currentMotorTorque < 1f && rb.linearVelocity.magnitude < 2f)
            engineRPM = Mathf.Lerp(engineRPM, config.engine.idleRPM, Time.fixedDeltaTime * 6f);

        engineRPM = Mathf.Clamp(engineRPM, config.engine.idleRPM, config.engine.redlineRPM);
    }

    private void HandleAutoShift(VehicleConfig.TransmissionSettings trans)
    {
        if (shiftTimer > 0f)
        {
            shiftTimer -= Time.fixedDeltaTime;
            return;
        }

        if (engineRPM > config.engine.redlineRPM * 0.92f && Gear < trans.gearCount + 1)
        {
            Gear++;
            shiftTimer = SHIFT_COOLDOWN;
        }
        else if (engineRPM < config.engine.idleRPM * 2.5f && Gear > 2)
        {
            Gear--;
            shiftTimer = SHIFT_COOLDOWN;
        }
    }

    private float GetGearRatio() => Gear < gearRatios.Length ? gearRatios[Gear] : 1f;

    private void ApplyMotorTorque(VehicleConfig.EngineSettings.Drivetrain dt)
    {
        float effTorque = currentMotorTorque * Mathf.Abs(GetGearRatio()) * config.transmission.finalDriveRatio;
        int powered = dt == VehicleConfig.EngineSettings.Drivetrain.AWD ? wheels.Length : 2;

        foreach (var w in wheels)
        {
            bool p = IsPowered(w, dt);
            w.motorTorque = p ? effTorque / powered * Mathf.Sign(GetGearRatio()) : 0f;
        }
    }

    private bool IsPowered(WheelCollider w, VehicleConfig.EngineSettings.Drivetrain dt)
    {
        float z = w.transform.localPosition.z;
        return dt == VehicleConfig.EngineSettings.Drivetrain.AWD ||
               (dt == VehicleConfig.EngineSettings.Drivetrain.FWD && z > 0) ||
               (dt == VehicleConfig.EngineSettings.Drivetrain.RWD && z <= 0);
    }

    private void ApplySteering()
    {
        foreach (var w in wheels)
        {
            if (w.transform.localPosition.z > 0)
                w.steerAngle = currentSteerAngle;
        }
    }

    private void ApplyBrakes(bool hb, float bias)
    {
        foreach (var w in wheels)
        {
            float b = (w.transform.localPosition.z > 0) ? bias : 1f - bias;
            w.brakeTorque = currentBrakeTorque * b;
            if (hb && w.transform.localPosition.z <= 0)
                w.brakeTorque *= 1.8f; // Extra rear lock
        }
    }
}