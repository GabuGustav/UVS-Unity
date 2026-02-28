using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class TrainController : MonoBehaviour
{
    public VehicleConfig config;
    public RailNetworkAsset railNetwork;
    public int startNodeId;
    public int endNodeId;
    public bool loopRoute = true;
    public SplineContainer manualSpline;

    [Header("Input")]
    [SerializeField] private VehicleInputHub inputHub;

    private Rigidbody _rb;
    private List<PathGraphBase.Edge> _routeEdges = new();
    private int _edgeIndex;
    private float _t;
    private float _speed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (inputHub == null)
            inputHub = GetComponent<VehicleInputHub>();
        BuildRoute();
    }

    private void BuildRoute()
    {
        _routeEdges.Clear();
        if (railNetwork != null && railNetwork.TryFindRoute(startNodeId, endNodeId, out var route))
        {
            foreach (var id in route.edgeIds)
            {
                var edge = railNetwork.edges.Find(e => e.id == id);
                if (edge != null) _routeEdges.Add(edge);
            }
        }
    }

    private void FixedUpdate()
    {
        if (config == null) return;

        float throttle = 0f;
        float brake = 0f;

        if (inputHub != null && inputHub.HasInput)
        {
            throttle = inputHub.Current.throttle;
            brake = inputHub.Current.brake;
        }

        float targetSpeed = throttle * config.train.maxSpeed;
        _speed = Mathf.MoveTowards(_speed, targetSpeed, config.train.accel * Time.fixedDeltaTime);
        if (brake > 0.1f)
            _speed = Mathf.MoveTowards(_speed, 0f, config.train.brake * Time.fixedDeltaTime);

        if (manualSpline != null)
        {
            MoveAlongSpline(manualSpline);
        }
        else if (_routeEdges.Count > 0)
        {
            MoveAlongEdges();
        }
    }

    private void MoveAlongSpline(SplineContainer spline)
    {
        float length = spline.Spline.GetLength();
        float delta = _speed * Time.fixedDeltaTime / Mathf.Max(0.1f, length);
        _t += delta;
        if (_t > 1f)
        {
            _t = loopRoute ? _t - 1f : 1f;
        }

        Vector3 localPos = spline.Spline.EvaluatePosition(_t);
        float3 localTan = spline.Spline.EvaluateTangent(_t);
        transform.position = spline.transform.TransformPoint(localPos);
        Vector3 worldTan = spline.transform.TransformDirection((Vector3)math.normalize(localTan));
        transform.rotation = Quaternion.LookRotation(worldTan, spline.transform.up);
    }

    private void MoveAlongEdges()
    {
        var edge = _routeEdges[_edgeIndex];
        if (edge == null || edge.spline == null) return;

        float length = edge.spline.Spline.GetLength();
        float delta = _speed * Time.fixedDeltaTime / Mathf.Max(0.1f, length);
        _t += delta;
        if (_t > 1f)
        {
            _t = 0f;
            _edgeIndex++;
            if (_edgeIndex >= _routeEdges.Count)
                _edgeIndex = loopRoute ? 0 : _routeEdges.Count - 1;
        }

        Vector3 localPos = edge.spline.Spline.EvaluatePosition(_t);
        float3 localTan = edge.spline.Spline.EvaluateTangent(_t);
        transform.position = edge.spline.transform.TransformPoint(localPos);
        Vector3 worldTan = edge.spline.transform.TransformDirection((Vector3)math.normalize(localTan));
        transform.rotation = Quaternion.LookRotation(worldTan, edge.spline.transform.up);
    }
}
