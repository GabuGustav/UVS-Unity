using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

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
    public float torqueMultiplier = 12f;
    public float brakeStrengthMultiplier = 200f;
    public float handbrakeMultiplier = 400f;
    public float holdingBrakeTorque = 120f;

    [Header("Transmission")]
    public bool isAutomatic = true;
    public int Gear { get; private set; } = 1; // 0=R, 1=N, 2+=D
    private float[] gearRatios;
    public float engineRPM;
    private float wheelRPM;

    private Rigidbody rb;
    private WheelCollider[] wheels;

    private float currentMotorTorque;
    private float currentBrakeTorque;
    private float currentSteerAngle;
    private float currentSteerLerp;
    private float shiftTimer;

    private const float SHIFT_COOLDOWN = 0.35f;
    private const float RPM_LERP = 8f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();

        if (config == null)
        {
            enabled = false;
            Debug.LogError("VehicleConfig missing!");
            return;
        }

        rb.mass = config.body.mass;
        rb.centerOfMass = config.body.centerOfMassOffset;

        foreach (var w in wheels)
        {
            var spring = w.suspensionSpring;
            spring.spring = config.suspension.springStiffness;
            spring.damper = config.suspension.damperStiffness;
            w.suspensionSpring = spring;
            w.suspensionDistance = config.suspension.suspensionDistance;
        }

        BuildGearRatios();

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

    private void BuildGearRatios()
    {
        var t = config.transmission;
        gearRatios = new float[t.gearCount + 2];
        gearRatios[0] = -Mathf.Abs(t.reverseGearRatio); // Reverse
        gearRatios[1] = 0f;                             // Neutral

        for (int i = 0; i < t.gearCount; i++)
            gearRatios[i + 2] = t.gearRatios[i];
    }

    private void FixedUpdate()
    {
        float accel = accelerateAction.ReadValue<float>();
        float brake = brakeAction.ReadValue<float>();
        float steerInput = steerAction.ReadValue<float>();
        bool handbrake = handbrakeAction.IsPressed();

        float fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool nearlyStopped = Mathf.Abs(fwdSpeed) < 1.2f;

        bool wantForward = accel > 0.1f;
        bool wantReverse = brake > 0.1f && !wantForward && nearlyStopped;

        // Direction shift only when stopped
        if (nearlyStopped && shiftTimer <= 0f)
        {
            if (wantReverse && Gear != 0)
            {
                Gear = 0;
                shiftTimer = SHIFT_COOLDOWN;
            }
            else if (wantForward && Gear < 2)
            {
                Gear = 2;
                shiftTimer = SHIFT_COOLDOWN;
            }
        }

        if (shiftTimer > 0f)
            shiftTimer -= Time.fixedDeltaTime;

        // Motor torque
        currentMotorTorque = 0f;
        if (!handbrake && brake < 0.01f)
        {
            if (Gear >= 2 && wantForward)
                currentMotorTorque = accel * config.engine.torque * torqueMultiplier;
            else if (Gear == 0 && brake > 0.1f)
                currentMotorTorque = brake * config.engine.torque * torqueMultiplier * reverseTorqueMultiplier;
        }

        // Engine braking ONLY when in gear
        if (currentMotorTorque == 0f && Gear != 1)
        {
            float engBrake = config.engine.torque * 0.06f;
            currentMotorTorque = Mathf.Sign(-fwdSpeed) * engBrake;
        }

        // Brakes
        currentBrakeTorque = brake * config.brakes.frontDiscDiameter * brakeStrengthMultiplier;

        if (handbrake)
            currentBrakeTorque = Mathf.Max(currentBrakeTorque,
                config.brakes.rearDiscDiameter * handbrakeMultiplier);

        if (nearlyStopped && accel < 0.05f && brake < 0.05f)
            currentBrakeTorque = Mathf.Max(currentBrakeTorque, holdingBrakeTorque);

        // Steering
        float targetSteer = steerInput * config.steering.maxSteeringAngle;
        currentSteerLerp = Mathf.Lerp(currentSteerLerp, targetSteer, steeringSmoothness);
        currentSteerAngle = currentSteerLerp;

        CalculateRPM();
        if (isAutomatic && Gear >= 2)
            HandleAutoShift();

        ApplyMotorTorque();
        ApplySteering();
        ApplyBrakes(handbrake);
    }

    private void CalculateRPM()
    {
        float sum = 0f;
        int count = 0;

        foreach (var w in wheels)
        {
            if (IsPowered(w))
            {
                sum += Mathf.Abs(w.rpm);
                count++;
            }
        }

        wheelRPM = count > 0 ? sum / count : 0f;

        float gear = Mathf.Abs(GetGearRatio());
        float finalDrive = config.transmission.finalDriveRatio;
        engineRPM = Mathf.Lerp(engineRPM, wheelRPM * gear * finalDrive, Time.fixedDeltaTime * RPM_LERP);

        if (Gear == 1 || rb.linearVelocity.magnitude < 1f)
            engineRPM = Mathf.Lerp(engineRPM, config.engine.idleRPM, Time.fixedDeltaTime * 5f);

        engineRPM = Mathf.Clamp(engineRPM, config.engine.idleRPM, config.engine.redlineRPM);
    }

    private void HandleAutoShift()
    {
        if (shiftTimer > 0f) return;

        if (engineRPM > config.engine.redlineRPM * 0.92f &&
            Gear < config.transmission.gearCount + 1)
        {
            Gear++;
            shiftTimer = SHIFT_COOLDOWN;
        }
        else if (engineRPM < config.engine.idleRPM * 2.2f && Gear > 2)
        {
            Gear--;
            shiftTimer = SHIFT_COOLDOWN;
        }
    }

    private float GetGearRatio()
    {
        return Gear < gearRatios.Length ? gearRatios[Gear] : 0f;
    }

    private void ApplyMotorTorque()
    {
        float ratio = GetGearRatio();
        if (Mathf.Approximately(ratio, 0f)) return;

        float effTorque = currentMotorTorque * Mathf.Abs(ratio) * config.transmission.finalDriveRatio;
        var poweredWheels = wheels.Where(IsPowered).ToArray();
        int count = poweredWheels.Length;

        foreach (var w in wheels)
            w.motorTorque = poweredWheels.Contains(w) ? effTorque / count * Mathf.Sign(ratio) : 0f;
    }

    private bool IsPowered(WheelCollider w)
    {
        float z = w.transform.localPosition.z;
        return config.engine.drivetrain switch
        {
            VehicleConfig.EngineSettings.Drivetrain.AWD => true,
            VehicleConfig.EngineSettings.Drivetrain.FWD => z > 0,
            VehicleConfig.EngineSettings.Drivetrain.RWD => z <= 0,
            _ => false
        };
    }

    private void ApplySteering()
    {
        foreach (var w in wheels)
            if (w.transform.localPosition.z > 0)
                w.steerAngle = currentSteerAngle;
    }

    private void ApplyBrakes(bool handbrake)
    {
        float bias = config.brakes.brakeBias;

        foreach (var w in wheels)
        {
            bool front = w.transform.localPosition.z > 0;
            w.brakeTorque = currentBrakeTorque * (front ? bias : 1f - bias);

            if (handbrake && !front)
                w.brakeTorque *= 1.8f;
        }
    }
}
