public interface IVehicleNetworkBackend
{
    string BackendId { get; }
    bool IsAvailable { get; }
    bool IsInitialized { get; }

    bool Initialize();
    bool RequestSeatEnter(int playerId, VehicleSeatManager seatManager, VehicleConfig.SeatRole preferredRole);
    void NotifySeatExit(int playerId, VehicleSeatManager seatManager);
    void Tick(float deltaTime);
}
