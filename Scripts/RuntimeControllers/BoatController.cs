using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    public VehicleConfig config;
    public WaterSurface waterSurface;

    [Header("Input Actions")]
    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction brakeAction;
    [SerializeField] private InputAction steerAction;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (config == null)
        {
            enabled = false;
            Debug.LogError("VehicleConfig missing!");
            return;
        }

        if (waterSurface == null)
            waterSurface = FindObjectOfType<WaterSurface>();

        if (waterSurface != null && config != null)
            waterSurface.ApplyRenderSettings(config.waterRender);

        ResolveInputProfile();
        EnableActions();
    }

    private void OnDestroy()
    {
        DisableActions();
    }

    private void FixedUpdate()
    {
        var w = config.water;

        ApplyBuoyancy(w);
        ApplyWaterDrag(w);
        ApplyPropulsion(w);
    }

    private void ApplyBuoyancy(VehicleConfig.WaterSettings w)
    {
        if (w.buoyancyPoints == null || w.buoyancyPoints.Count == 0) return;

        foreach (var p in w.buoyancyPoints)
        {
            Vector3 worldPos = transform.TransformPoint(p.localPosition);
            float waterY = 0f;
            Vector3 waterNormal = Vector3.up;
            if (waterSurface != null)
            {
                var sample = waterSurface.SampleHeightAndNormal(worldPos);
                waterY = sample.height;
                waterNormal = sample.normal;
            }
            float submersion = waterY - worldPos.y;

            if (submersion > 0f)
            {
                float displaced = Mathf.Clamp01(submersion / Mathf.Max(0.01f, p.maxSubmersion)) * p.volume;
                float force = w.waterDensity * Physics.gravity.magnitude * displaced * w.buoyancyForce;
                rb.AddForceAtPosition(waterNormal * force, worldPos, ForceMode.Force);
            }
        }
    }

    private void ApplyWaterDrag(VehicleConfig.WaterSettings w)
    {
        if (rb == null) return;
        rb.AddForce(-rb.linearVelocity * w.linearDrag);
        rb.AddTorque(-rb.angularVelocity * w.angularDrag);
    }

    private void ApplyPropulsion(VehicleConfig.WaterSettings w)
    {
        float throttle = throttleAction != null ? throttleAction.ReadValue<float>() : 0f;
        float brake = brakeAction != null ? brakeAction.ReadValue<float>() : 0f;
        float steer = steerAction != null ? steerAction.ReadValue<float>() : 0f;

        float drive = throttle;
        if (brake > 0.1f && throttle < 0.1f)
            drive = -brake;

        rb.AddForce(transform.forward * (drive * w.propulsionForce));
        rb.AddTorque(transform.up * (steer * w.turnTorque));
    }

    private void ResolveInputProfile()
    {
        var profile = VehicleInputResolver.GetProfile(config);
        if (profile == null) return;

        throttleAction = VehicleInputResolver.Resolve(throttleAction, profile.throttle);
        brakeAction = VehicleInputResolver.Resolve(brakeAction, profile.brake);
        steerAction = VehicleInputResolver.Resolve(steerAction, profile.steer);
    }

    private void EnableActions()
    {
        throttleAction?.Enable();
        brakeAction?.Enable();
        steerAction?.Enable();
    }

    private void DisableActions()
    {
        throttleAction?.Disable();
        brakeAction?.Disable();
        steerAction?.Disable();
    }
}
