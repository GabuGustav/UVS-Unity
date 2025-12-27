using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LowriderHydraulicsController : MonoBehaviour
{
    public VehicleConfig config;
    public WheelCollider[] wheelColliders;
    public Transform[] springVisuals; // Optional: assign coiled spring meshes

    private Rigidbody rb;
    private float danceTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!config.lowrider.enableHydraulics) enabled = false;
    }

    void FixedUpdate()
    {
        HandleInput();
        if (config.lowrider.enableDanceMode) AutoDance();
    }

    void HandleInput()
    {
        var l = config.lowrider;

        // Hop all
        if (Input.GetKeyDown(KeyCode.Space))
            HopAll(l.hopForce);

        // Front up / Rear up
        if (Input.GetKey(KeyCode.UpArrow)) LiftWheels(0, 1, l.hopForce * 1.3f);
        if (Input.GetKey(KeyCode.DownArrow)) LiftWheels(2, 3, l.hopForce * 1.3f);

        // Side tilt
        if (Input.GetKey(KeyCode.LeftArrow)) TiltLeftRight(true, l.tiltSpeed);
        if (Input.GetKey(KeyCode.RightArrow)) TiltLeftRight(false, l.tiltSpeed);

        // Slam
        if (Input.GetKey(KeyCode.S))
            SlamAll(l.slamForce);
    }

    void HopAll(float force)
    {
        foreach (var wc in wheelColliders)
            rb.AddForceAtPosition(Vector3.up * force, wc.transform.position, ForceMode.Impulse);
    }

    void LiftWheels(int i1, int i2, float force)
    {
        rb.AddForceAtPosition(Vector3.up * force * Time.fixedDeltaTime, wheelColliders[i1].transform.position);
        rb.AddForceAtPosition(Vector3.up * force * Time.fixedDeltaTime, wheelColliders[i2].transform.position);
    }

    void TiltLeftRight(bool left, float speed)
    {
        float dir = left ? 1 : -1;
        rb.AddTorque(transform.forward * dir * speed * 1000f);
    }

    void SlamAll(float force)
    {
        foreach (var wc in wheelColliders)
            rb.AddForceAtPosition(Vector3.down * force * Time.fixedDeltaTime, wc.transform.position);
    }

    void AutoDance()
    {
        danceTimer += Time.fixedDeltaTime;
        float wave = Mathf.Sin(danceTimer * config.lowrider.danceSpeed * Mathf.PI * 2);
        float force = wave * config.lowrider.bounceAmplitude * config.lowrider.hopForce;

        foreach (var wc in wheelColliders)
            rb.AddForceAtPosition(Vector3.up * force, wc.transform.position);
    }
}