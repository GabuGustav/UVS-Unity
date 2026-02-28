using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class LandVehicleController : MonoBehaviour
{
    [Header("Config Asset")]
    public VehicleConfig config;

    [Header("Input Actions")]
    [SerializeField] private InputAction accelerateAction;
    [SerializeField] private InputAction brakeAction;
    [SerializeField] private InputAction steerAction;
    [SerializeField] private InputAction handbrakeAction;
    [SerializeField] private InputAction recoverAction;

    [Header("Optional Wheel Visual Sync")]
    [SerializeField] private WheelVisualSync wheelVisualSync;
    [SerializeField] private VehicleInputHub inputHub;

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
    private WheelCollider frontLeft;
    private WheelCollider frontRight;
    private WheelCollider rearLeft;
    private WheelCollider rearRight;
    private readonly Dictionary<WheelCollider, VehicleConfig.WheelSettings> wheelConfigMap = new();
    private bool useConfiguredSteering;
    private bool useConfiguredPower;

    private float flipCooldownTimer;

    private float currentMotorTorque;
    private float currentBrakeTorque;
    private float currentSteerAngle;
    private float currentSteerLerp;
    private float shiftTimer;

    private const float SHIFT_COOLDOWN = 0.35f;
    private const float RPM_LERP = 8f;

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
        BuildWheelConfigMap();

        ResolveInputProfile();
        EnableActions();

        CacheWheelPairs();
        if (wheelVisualSync == null)
            wheelVisualSync = GetComponent<WheelVisualSync>();
        if (inputHub == null)
            inputHub = GetComponent<VehicleInputHub>();
    }

    protected virtual void OnDestroy()
    {
        DisableActions();
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

    protected virtual void FixedUpdate()
    {
        float accel = 0f;
        float brake = 0f;
        float steerInput = 0f;
        bool handbrake = false;
        bool recover = false;

        if (inputHub != null && inputHub.HasInput)
        {
            var state = inputHub.Current;
            accel = state.throttle;
            brake = state.brake;
            steerInput = state.steer;
            handbrake = state.handbrake;
            recover = state.recover;
        }
        else
        {
            accel = accelerateAction != null ? accelerateAction.ReadValue<float>() : 0f;
            brake = brakeAction != null ? brakeAction.ReadValue<float>() : 0f;
            steerInput = steerAction != null ? steerAction.ReadValue<float>() : 0f;
            handbrake = handbrakeAction != null && handbrakeAction.IsPressed();
            recover = recoverAction != null && recoverAction.IsPressed();
        }

        float fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        var assist = config.drivingAssist;
        bool nearlyStopped = Mathf.Abs(fwdSpeed) < assist.reverseEngageSpeed;

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

        // Exit reverse when moving forward enough
        if (Gear == 0 && fwdSpeed > assist.reverseExitSpeed && shiftTimer <= 0f)
        {
            Gear = 2;
            shiftTimer = SHIFT_COOLDOWN;
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

        ApplyStopAssist(accel, brake, handbrake);
        ApplyAntiRoll();
        ApplyFlipRecovery(recover);
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
        if (w != null && useConfiguredPower && wheelConfigMap.TryGetValue(w, out var ws))
            return ws.isPowered || ws.role == VehicleConfig.WheelRole.RearDrive ||
                   ws.role == VehicleConfig.WheelRole.TrackLeft || ws.role == VehicleConfig.WheelRole.TrackRight;

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
            if (IsSteeringWheel(w))
                w.steerAngle = currentSteerAngle;
    }

    private void ApplyBrakes(bool handbrake)
    {
        float bias = config.brakes.brakeBias;

        foreach (var w in wheels)
        {
            bool front = IsFrontWheel(w);
            w.brakeTorque = currentBrakeTorque * (front ? bias : 1f - bias);

            if (handbrake && !front)
                w.brakeTorque *= 1.8f;
        }
    }

    private void ResolveInputProfile()
    {
        var profile = VehicleInputResolver.GetProfile(config);
        if (profile == null) return;

        accelerateAction = VehicleInputResolver.Resolve(accelerateAction, profile.throttle);
        brakeAction = VehicleInputResolver.Resolve(brakeAction, profile.brake);
        steerAction = VehicleInputResolver.Resolve(steerAction, profile.steer);
        handbrakeAction = VehicleInputResolver.Resolve(handbrakeAction, profile.handbrake);
        recoverAction = VehicleInputResolver.Resolve(recoverAction, profile.recover);
    }

    private void EnableActions()
    {
        accelerateAction?.Enable();
        brakeAction?.Enable();
        steerAction?.Enable();
        handbrakeAction?.Enable();
        recoverAction?.Enable();
    }

    private void DisableActions()
    {
        accelerateAction?.Disable();
        brakeAction?.Disable();
        steerAction?.Disable();
        handbrakeAction?.Disable();
        recoverAction?.Disable();
    }

    private void CacheWheelPairs()
    {
        if (wheels == null) return;
        frontLeft = frontRight = rearLeft = rearRight = null;

        foreach (var w in wheels)
        {
            if (w == null) continue;
            bool front = IsFrontWheel(w);
            bool left = w.transform.localPosition.x < 0f;

            if (front && left)
                frontLeft = SelectOuterByX(frontLeft, w, preferLeft: true);
            else if (front && !left)
                frontRight = SelectOuterByX(frontRight, w, preferLeft: false);
            else if (!front && left)
                rearLeft = SelectOuterByX(rearLeft, w, preferLeft: true);
            else
                rearRight = SelectOuterByX(rearRight, w, preferLeft: false);
        }
    }

    private void ApplyAntiRoll()
    {
        float stiffness = config != null ? config.suspension.antiRollBarStiffness : 0f;
        if (stiffness <= 0f || rb == null) return;

        ApplyAntiRollPair(frontLeft, frontRight, stiffness);
        ApplyAntiRollPair(rearLeft, rearRight, stiffness);
    }

    private void ApplyAntiRollPair(WheelCollider left, WheelCollider right, float stiffness)
    {
        if (left == null || right == null) return;

        float travelL = 1f;
        float travelR = 1f;
        bool groundedL = left.GetGroundHit(out WheelHit hitL);
        bool groundedR = right.GetGroundHit(out WheelHit hitR);

        if (groundedL)
            travelL = (-left.transform.InverseTransformPoint(hitL.point).y - left.radius) / left.suspensionDistance;
        if (groundedR)
            travelR = (-right.transform.InverseTransformPoint(hitR.point).y - right.radius) / right.suspensionDistance;

        float antiRollForce = (travelL - travelR) * stiffness;
        if (groundedL)
            rb.AddForceAtPosition(left.transform.up * -antiRollForce, left.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(right.transform.up * antiRollForce, right.transform.position);
    }

    private void ApplyStopAssist(float accel, float brake, bool handbrake)
    {
        if (config == null || rb == null || wheels == null) return;
        var a = config.drivingAssist;

        if (handbrake) return;
        if (Mathf.Abs(accel) > 0.05f || Mathf.Abs(brake) > 0.05f) return;
        if (rb.linearVelocity.magnitude > a.stopAssistSpeed) return;

        foreach (var w in wheels)
        {
            if (w == null) continue;
            if (Mathf.Abs(w.rpm) > a.rpmStopThreshold)
                w.brakeTorque = Mathf.Max(w.brakeTorque, a.spinKillBrakeTorque);
        }
    }

    private void ApplyFlipRecovery(bool manualRecover)
    {
        if (config == null || rb == null) return;
        var a = config.drivingAssist;

        if (flipCooldownTimer > 0f)
            flipCooldownTimer -= Time.fixedDeltaTime;

        float speed = rb.linearVelocity.magnitude;
        if (speed > a.flipMaxSpeed) return;

        float angle = Vector3.Angle(transform.up, Vector3.up);
        bool isUpsideDown = angle > a.flipAngleThreshold;

        if (!a.autoFlipEnabled && !manualRecover) return;
        if (!manualRecover && !isUpsideDown) return;
        if (flipCooldownTimer > 0f) return;

        Vector3 torqueAxis = Vector3.Cross(transform.up, Vector3.up);
        if (torqueAxis.sqrMagnitude < 0.01f) return;

        rb.AddTorque(torqueAxis.normalized * a.flipTorque, ForceMode.Acceleration);
        flipCooldownTimer = a.flipCooldown;
    }

    private void LateUpdate()
    {
        if (wheelVisualSync != null && !wheelVisualSync.enabled)
            wheelVisualSync.SyncNow();
    }

    private void BuildWheelConfigMap()
    {
        wheelConfigMap.Clear();
        useConfiguredSteering = false;
        useConfiguredPower = false;

        if (config?.wheels == null || config.wheels.Count == 0 || wheels == null || wheels.Length == 0)
            return;

        foreach (var wheelCollider in wheels)
        {
            if (wheelCollider == null) continue;
            var best = FindBestWheelSetting(wheelCollider.transform.localPosition);
            if (best == null) continue;
            wheelConfigMap[wheelCollider] = best;
        }

        useConfiguredSteering = wheelConfigMap.Values.Any(ws => ws != null && ws.isSteering);
        useConfiguredPower = wheelConfigMap.Values.Any(ws => ws != null &&
            (ws.isPowered || ws.role == VehicleConfig.WheelRole.RearDrive ||
             ws.role == VehicleConfig.WheelRole.TrackLeft || ws.role == VehicleConfig.WheelRole.TrackRight));
    }

    private VehicleConfig.WheelSettings FindBestWheelSetting(Vector3 colliderLocalPosition)
    {
        VehicleConfig.WheelSettings best = null;
        float bestScore = float.MaxValue;

        foreach (var ws in config.wheels)
        {
            float score = (ws.localPosition - colliderLocalPosition).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = ws;
            }
        }

        return best;
    }

    private bool IsSteeringWheel(WheelCollider w)
    {
        if (w != null && useConfiguredSteering && wheelConfigMap.TryGetValue(w, out var ws))
            return ws.isSteering || ws.role == VehicleConfig.WheelRole.FrontSteer;

        return w != null && w.transform.localPosition.z > 0f;
    }

    private bool IsFrontWheel(WheelCollider w)
    {
        if (w != null && useConfiguredSteering && wheelConfigMap.TryGetValue(w, out var ws))
            return ws.isSteering || ws.role == VehicleConfig.WheelRole.FrontSteer;

        return w != null && w.transform.localPosition.z > 0f;
    }

    private static WheelCollider SelectOuterByX(WheelCollider current, WheelCollider candidate, bool preferLeft)
    {
        if (candidate == null) return current;
        if (current == null) return candidate;

        float curX = current.transform.localPosition.x;
        float newX = candidate.transform.localPosition.x;
        return preferLeft
            ? (newX < curX ? candidate : current)
            : (newX > curX ? candidate : current);
    }
}
