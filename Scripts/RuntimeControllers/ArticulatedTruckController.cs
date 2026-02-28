using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArticulatedTruckController : MonoBehaviour
{
    public VehicleConfig config;
    public TrailerController trailer;

    private Rigidbody _rb;
    private ConfigurableJoint _joint;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        EnsureJoint();
    }

    private void FixedUpdate()
    {
        if (config == null || trailer == null) return;

        if (config.driveModel == VehicleConfig.VehicleDriveModel.Arcade)
            StabilizeTrailer();
    }

    private void EnsureJoint()
    {
        if (config == null || !config.trailer.hasTrailer) return;
        if (trailer == null) return;

        if (_joint == null)
            _joint = GetComponent<ConfigurableJoint>() ?? gameObject.AddComponent<ConfigurableJoint>();

        _joint.connectedBody = trailer.TrailerBody;

        var hitchA = config.articulation.tractorHitch != null ? config.articulation.tractorHitch : transform;
        var hitchB = config.articulation.trailerHitch != null ? config.articulation.trailerHitch : trailer.trailerHitch != null ? trailer.trailerHitch : trailer.transform;

        _joint.autoConfigureConnectedAnchor = false;
        _joint.anchor = transform.InverseTransformPoint(hitchA.position);
        _joint.connectedAnchor = trailer.transform.InverseTransformPoint(hitchB.position);

        _joint.xMotion = ConfigurableJointMotion.Locked;
        _joint.yMotion = ConfigurableJointMotion.Locked;
        _joint.zMotion = ConfigurableJointMotion.Locked;

        _joint.angularXMotion = ConfigurableJointMotion.Locked;
        _joint.angularZMotion = ConfigurableJointMotion.Locked;
        _joint.angularYMotion = ConfigurableJointMotion.Limited;

        var limit = _joint.angularYLimit;
        limit.limit = Mathf.Max(1f, config.articulation.hitchYawLimit);
        _joint.angularYLimit = limit;

        var spring = _joint.angularYZDrive;
        spring.positionSpring = config.articulation.hitchSpring;
        spring.positionDamper = config.articulation.hitchDamping;
        _joint.angularYZDrive = spring;

        if (trailer.TrailerBody != null)
            trailer.TrailerBody.mass = config.trailer.trailerMass;
    }

    private void StabilizeTrailer()
    {
        if (trailer == null || trailer.TrailerBody == null) return;
        Vector3 forward = transform.forward;
        Vector3 trailerForward = trailer.transform.forward;
        Vector3 torqueAxis = Vector3.Cross(trailerForward, forward);
        trailer.TrailerBody.AddTorque(torqueAxis * 2.5f, ForceMode.Acceleration);
    }
}
