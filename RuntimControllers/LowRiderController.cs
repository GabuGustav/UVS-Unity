using UnityEngine;
using UnityEngine.InputSystem; // <-- This is the key using

[RequireComponent(typeof(Rigidbody))]
public class LowriderController : MonoBehaviour
{
    [Header("Config")]
    public VehicleConfig config;

    [Header("Input Actions (Create an Input Action Asset if you don't have one)")]
    public InputAction hopAction;        // Space or button for hop
    public InputAction frontAction;      // UpArrow
    public InputAction rearAction;       // DownArrow
    public InputAction leftAction;       // LeftArrow
    public InputAction rightAction;      // RightArrow
    public InputAction slamAction;       // S key

    private Rigidbody rb;
    private WheelCollider[] wheels;
    private float danceTimer;
    private bool danceEnabled = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();

        if (config == null || !config.lowrider.enableHydraulics)
        {
            enabled = false;
            return;
        }

        rb.centerOfMass = new Vector3(0, -0.8f, 0); // Better hopping stability

        // Enable all actions
        hopAction.Enable();
        frontAction.Enable();
        rearAction.Enable();
        leftAction.Enable();
        rightAction.Enable();
        slamAction.Enable();
    }

    private void OnDestroy()
    {
        // Clean up
        hopAction.Disable();
        frontAction.Disable();
        rearAction.Disable();
        leftAction.Disable();
        rightAction.Disable();
        slamAction.Disable();
    }

    private void FixedUpdate()
    {
        if (config == null || !config.lowrider.enableHydraulics) return;

        var l = config.lowrider;

        // Hop All (press once)
        if (hopAction.WasPressedThisFrame())
            HopAll(l.hopForce);

        // Front / Rear lift (hold)
        if (frontAction.IsPressed())
            LiftFront(l.hopForce * 1.3f);

        if (rearAction.IsPressed())
            LiftRear(l.hopForce * 1.3f);

        // Tilt left/right (hold)
        if (leftAction.IsPressed())
            TiltLeft(l.tiltSpeed);

        if (rightAction.IsPressed())
            TiltRight(l.tiltSpeed);

        // Slam (hold or press)
        if (slamAction.IsPressed())
            Slam(l.slamForce);

        // Auto Dance Mode
        if (l.enableDanceMode)
        {
            danceTimer += Time.fixedDeltaTime;
            float wave = Mathf.Sin(danceTimer * l.danceSpeed * 2f * Mathf.PI) * l.bounceAmplitude;
            HopAll(wave * l.hopForce * 0.6f);
        }
    }

    private void HopAll(float force)
    {
        foreach (var wheel in wheels)
        {
            rb.AddForceAtPosition(Vector3.up * force, wheel.transform.position, ForceMode.Impulse);
        }
    }

    private void LiftFront(float force)
    {
        ApplyForceToAxle(true, force * Time.fixedDeltaTime * 80f);
    }

    private void LiftRear(float force)
    {
        ApplyForceToAxle(false, force * Time.fixedDeltaTime * 80f);
    }

    private void ApplyForceToAxle(bool front, float force)
    {
        foreach (var wheel in wheels)
        {
            bool isFront = wheel.transform.localPosition.z > 0;
            if (isFront == front)
            {
                rb.AddForceAtPosition(Vector3.up * force, wheel.transform.position);
            }
        }
    }

    private void TiltLeft(float speed)
    {
        rb.AddTorque(transform.forward * speed * 1000f);
    }

    private void TiltRight(float speed)
    {
        rb.AddTorque(transform.forward * -speed * 1000f);
    }

    private void Slam(float force)
    {
        HopAll(-force * 0.8f);
    }
}