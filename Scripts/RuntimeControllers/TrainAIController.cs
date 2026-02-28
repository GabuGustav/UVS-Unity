using UnityEngine;

[RequireComponent(typeof(VehicleInputHub))]
public class TrainAIController : MonoBehaviour, IVehicleInputSource
{
    public VehicleConfig config;
    public float desiredSpeed = 0.8f;
    public RailSignal[] signals;
    public string blockId;

    private VehicleInputState _state;
    private bool _hasBlock;

    public bool IsActive => enabled;

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(blockId))
            _hasBlock = RailBlockReservation.TryReserve(blockId);
        else
            _hasBlock = true;
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(blockId))
            RailBlockReservation.Release(blockId);
        _hasBlock = false;
    }

    private void FixedUpdate()
    {
        float throttle = desiredSpeed;
        float brake = 0f;

        if (!_hasBlock)
        {
            throttle = 0f;
            brake = 1f;
        }

        if (signals != null)
        {
            foreach (var s in signals)
            {
                if (s != null && s.ShouldStop(transform.position))
                {
                    throttle = 0f;
                    brake = 1f;
                    break;
                }
            }
        }

        _state = VehicleInputState.Zero;
        _state.throttle = throttle;
        _state.brake = brake;
    }

    public VehicleInputState ReadInput()
    {
        return _state;
    }
}
