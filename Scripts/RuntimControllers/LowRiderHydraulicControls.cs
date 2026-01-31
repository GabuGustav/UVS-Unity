using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class LowriderHydraulicsController : MonoBehaviour
{
    public VehicleConfig config;
    public WheelCollider[] wheelColliders;
    public Transform[] springVisuals; // Optional: assign coiled spring meshes

    private Rigidbody rb;
    private float danceTimer;

    // ── New Input System references ──
    [Header("Input Actions")]
    public InputAction hopAction;        // Hop all (was Space)
    public InputAction frontLiftAction;  // Front up (was UpArrow)
    public InputAction rearLiftAction;   // Rear up (was DownArrow)
    public InputAction leftTiltAction;   // Tilt left (was LeftArrow)
    public InputAction rightTiltAction;  // Tilt right (was RightArrow)
    public InputAction slamAction;       // Slam (was S)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!config.lowrider.enableHydraulics)
        {
            enabled = false;
            return;
        }

        // Enable inputs
        hopAction?.Enable();
        frontLiftAction?.Enable();
        rearLiftAction?.Enable();
        leftTiltAction?.Enable();
        rightTiltAction?.Enable();
        slamAction?.Enable();
    }

    void OnDestroy()
    {
        // Good practice: disable / dispose
        hopAction?.Disable();
        frontLiftAction?.Disable();
        rearLiftAction?.Disable();
        leftTiltAction?.Disable();
        rightTiltAction?.Disable();
        slamAction?.Disable();
    }

    void FixedUpdate()
    {
        HandleInput();

        if (config.lowrider.enableDanceMode)
            AutoDance();
    }

    void HandleInput()
    {
        var l = config.lowrider;

        // Hop all (on press)
        if (hopAction?.WasPressedThisFrame() == true)
        {
            HopAll(l.hopForce);
        }

        // Front lift (hold)
        if (frontLiftAction?.IsPressed() == true)
        {
            LiftWheels(0, 1, l.hopForce * 1.3f);
        }

        // Rear lift (hold)
        if (rearLiftAction?.IsPressed() == true)
        {
            LiftWheels(2, 3, l.hopForce * 1.3f);
        }

        // Side tilt
        if (leftTiltAction?.IsPressed() == true)
        {
            TiltLeftRight(true, l.tiltSpeed);
        }
        if (rightTiltAction?.IsPressed() == true)
        {
            TiltLeftRight(false, l.tiltSpeed);
        }

        // Slam (hold)
        if (slamAction?.IsPressed() == true)
        {
            SlamAll(l.slamForce);
        }
    }

    void HopAll(float force)
    {
        foreach (var wc in wheelColliders)
        {
            rb.AddForceAtPosition(Vector3.up * force, wc.transform.position, ForceMode.Impulse);
        }
    }

    void LiftWheels(int i1, int i2, float force)
    {
        // Original used continuous force → we keep similar behavior
        float applied = force * Time.fixedDeltaTime;

        rb.AddForceAtPosition(Vector3.up * applied, wheelColliders[i1].transform.position);
        rb.AddForceAtPosition(Vector3.up * applied, wheelColliders[i2].transform.position);
    }

    void TiltLeftRight(bool left, float speed)
    {
        float dir = left ? 1f : -1f;
        // Original used 1000f multiplier → kept
        rb.AddTorque(1000f * dir * speed * transform.forward);
    }

    void SlamAll(float force)
    {
        float applied = force * Time.fixedDeltaTime;

        foreach (var wc in wheelColliders)
        {
            rb.AddForceAtPosition(Vector3.down * applied, wc.transform.position);
        }
    }

    void AutoDance()
    {
        danceTimer += Time.fixedDeltaTime;

        float wave = Mathf.Sin(danceTimer * config.lowrider.danceSpeed * Mathf.PI * 2f);
        float force = wave * config.lowrider.bounceAmplitude * config.lowrider.hopForce;

        foreach (var wc in wheelColliders)
        {
            rb.AddForceAtPosition(Vector3.up * force, wc.transform.position);
        }
    }
}