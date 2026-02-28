#if UNITY_NETCODE_GAMEOBJECTS
using Unity.Netcode;
using UnityEngine;

public class UVSNetcodeBackend : IVehicleNetworkBackend
{
    public string BackendId => "ngo";
    public bool IsAvailable => true;
    public bool IsInitialized { get; private set; }

    public bool Initialize()
    {
        IsInitialized = NetworkManager.Singleton != null;
        if (!IsInitialized)
            Debug.LogWarning("[UVS] NGO backend selected but no NetworkManager found. Falling back to local behavior.");
        return IsInitialized;
    }

    public bool RequestSeatEnter(int playerId, VehicleSeatManager seatManager, VehicleConfig.SeatRole preferredRole)
    {
        // Foundation behavior: route through local seat manager until authoritative seat RPC is added.
        if (seatManager == null) return false;
        return seatManager.TryEnterSeat(playerId, preferredRole);
    }

    public void NotifySeatExit(int playerId, VehicleSeatManager seatManager)
    {
        seatManager?.ExitSeat(playerId);
    }

    public void Tick(float deltaTime) { }
}
#else
public class UVSNetcodeBackend : IVehicleNetworkBackend
{
    public string BackendId => "ngo";
    public bool IsAvailable => false;
    public bool IsInitialized => false;

    public bool Initialize() => false;
    public bool RequestSeatEnter(int playerId, VehicleSeatManager seatManager, VehicleConfig.SeatRole preferredRole) => false;
    public void NotifySeatExit(int playerId, VehicleSeatManager seatManager) { }
    public void Tick(float deltaTime) { }
}
#endif
