using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VehicleInputHub))]
public class VehicleAIController : MonoBehaviour, IVehicleInputSource
{
    public VehicleConfig config;
    public RoadNetworkAsset roadNetwork;
    public GridNetworkAsset gridNetwork;
    public int startNodeId;
    public int endNodeId;
    public Vector2Int gridStart;
    public Vector2Int gridEnd;
    public Transform[] fallbackWaypoints;
    public bool loopRoute = true;

    [Header("Sensors")]
    public VehicleSensorRig sensors;

    [Header("AI State")]
    public DriverProfile overrideProfile;
    public AIState currentState = AIState.Cruise;

    private VehicleInputState _state;
    private Rigidbody _rb;
    private int _currentIndex;
    private List<Vector3> _routePoints = new();
    private float _nextDecisionTime;
    private float _driftUntil;

    public enum AIState
    {
        Cruise,
        Follow,
        Avoid,
        Yield,
        Overtake,
        Drift,
        Park
    }

    public bool IsActive => enabled;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (sensors == null)
            sensors = GetComponent<VehicleSensorRig>();
        BuildRoute();
        ScheduleDecision();
    }

    private void BuildRoute()
    {
        _routePoints.Clear();

        if (roadNetwork != null && roadNetwork.TryFindRoute(startNodeId, endNodeId, out var route))
        {
            _routePoints.AddRange(route.sampledPoints);
        }
        else if (gridNetwork != null && gridNetwork.TryFindPath(gridStart, gridEnd, out var gridPoints))
        {
            _routePoints.AddRange(gridPoints);
        }
        else if (fallbackWaypoints != null)
        {
            foreach (var t in fallbackWaypoints)
                if (t != null) _routePoints.Add(t.position);
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= _nextDecisionTime)
        {
            EvaluateState();
            UpdateInputState();
            ScheduleDecision();
        }
    }

    private void EvaluateState()
    {
        if (_routePoints.Count == 0)
        {
            currentState = AIState.Park;
            return;
        }

        if (!loopRoute && _currentIndex >= _routePoints.Count - 1)
        {
            float endDist = Vector3.Distance(transform.position, _routePoints[^1]);
            if (endDist < 3f)
            {
                currentState = AIState.Park;
                return;
            }
        }

        if (_driftUntil > Time.time)
        {
            currentState = AIState.Drift;
            return;
        }

        if (sensors != null)
        {
            var forward = sensors.forward;
            if (forward.hit && forward.distance < GetFollowDistance() * 0.6f)
            {
                currentState = AIState.Avoid;
                return;
            }
            if (forward.hit && forward.distance < GetFollowDistance())
            {
                currentState = AIState.Follow;
                return;
            }
        }

        currentState = AIState.Cruise;

        var profile = overrideProfile != null ? overrideProfile : config != null ? config.trafficAI.profile : null;
        if (profile != null && profile.driftTendency > 0.6f && Random.value < profile.driftTendency * 0.02f)
        {
            _driftUntil = Time.time + Random.Range(0.6f, 1.2f);
            currentState = AIState.Drift;
        }
    }

    private void UpdateInputState()
    {
        var target = GetTargetPoint();
        Vector3 toTarget = (target - transform.position);
        Vector3 local = transform.InverseTransformDirection(toTarget.normalized);
        float steer = Mathf.Clamp(local.x, -1f, 1f);

        float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
        float desired = GetTargetSpeed();
        float throttle = Mathf.Clamp((desired - speed) * 0.1f, 0f, 1f);
        float brake = Mathf.Clamp((speed - desired) * 0.1f, 0f, 1f);

        if (currentState == AIState.Follow)
        {
            throttle *= 0.4f;
            brake = Mathf.Max(brake, 0.3f);
        }
        else if (currentState == AIState.Avoid)
        {
            brake = Mathf.Max(brake, 0.6f);
            if (sensors != null)
            {
                float leftDist = sensors.left.distance;
                float rightDist = sensors.right.distance;
                steer = leftDist > rightDist ? -1f : 1f;
            }
        }
        else if (currentState == AIState.Drift)
        {
            throttle = Mathf.Max(throttle, 0.6f);
            steer = Mathf.Sign(steer) * Mathf.Clamp(Mathf.Abs(steer) + 0.4f, -1f, 1f);
        }
        else if (currentState == AIState.Park)
        {
            throttle = 0f;
            brake = 1f;
            steer = 0f;
        }

        _state = VehicleInputState.Zero;
        _state.throttle = throttle;
        _state.brake = brake;
        _state.steer = steer;
    }

    private Vector3 GetTargetPoint()
    {
        if (_routePoints.Count == 0)
            return transform.position + transform.forward * 5f;

        float dist = Vector3.Distance(transform.position, _routePoints[_currentIndex]);
        if (dist < 4f)
        {
            _currentIndex++;
            if (_currentIndex >= _routePoints.Count)
                _currentIndex = loopRoute ? 0 : _routePoints.Count - 1;
        }

        return _routePoints[_currentIndex];
    }

    private float GetTargetSpeed()
    {
        var settings = config != null ? config.trafficAI : null;
        return settings != null ? settings.targetSpeed : 12f;
    }

    private float GetFollowDistance()
    {
        var settings = config != null ? config.trafficAI : null;
        return settings != null ? settings.followDistance : 8f;
    }

    private void ScheduleDecision()
    {
        var profile = overrideProfile != null ? overrideProfile : config != null ? config.trafficAI.profile : null;
        float delay = profile != null ? profile.SampleReactionTime() : Random.Range(0.2f, 0.6f);
        _nextDecisionTime = Time.time + delay;
    }

    public VehicleInputState ReadInput()
    {
        return _state;
    }
}
