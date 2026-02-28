public class UVSLocalOnlyBackend : IVehicleNetworkBackend
{
    public string BackendId => "local";
    public bool IsAvailable => true;
    public bool IsInitialized { get; private set; }

    public bool Initialize()
    {
        IsInitialized = true;
        return true;
    }

    public bool RequestSeatEnter(int playerId, VehicleSeatManager seatManager, VehicleConfig.SeatRole preferredRole)
    {
        if (seatManager == null) return false;
        return seatManager.TryEnterSeat(playerId, preferredRole);
    }

    public void NotifySeatExit(int playerId, VehicleSeatManager seatManager)
    {
        seatManager?.ExitSeat(playerId);
    }

    public void Tick(float deltaTime) { }
}
