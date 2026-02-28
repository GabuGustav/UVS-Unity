using UnityEngine;

public class VehicleInputHub : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputSourceBehaviour;
    [SerializeField] private bool allowManualOverride = true;

    private IVehicleInputSource _source;
    private bool _manualOverride;

    public VehicleInputState Current { get; private set; }
    public bool HasInput { get; private set; }

    private void Awake()
    {
        ResolveSource();
    }

    private void Update()
    {
        if (_manualOverride)
            return;

        if (_source != null && _source.IsActive)
        {
            Current = _source.ReadInput();
            HasInput = true;
        }
        else
        {
            HasInput = false;
        }
    }

    public void SetState(VehicleInputState state)
    {
        if (!allowManualOverride)
            return;

        Current = state;
        HasInput = true;
        _manualOverride = true;
    }

    public void ClearManualOverride()
    {
        _manualOverride = false;
    }

    public void ResolveSource()
    {
        _source = null;

        if (inputSourceBehaviour != null && inputSourceBehaviour is IVehicleInputSource source)
        {
            _source = source;
            return;
        }

        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb is IVehicleInputSource candidate)
            {
                _source = candidate;
                break;
            }
        }
    }
}
