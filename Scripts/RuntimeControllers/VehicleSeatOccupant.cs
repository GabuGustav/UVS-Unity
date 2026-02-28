using UnityEngine;

public class VehicleSeatOccupant : MonoBehaviour
{
    public int playerId;
    public VehicleConfig.SeatRole preferredRole = VehicleConfig.SeatRole.Driver;

    public VehicleSeatManager CurrentSeatManager { get; private set; }
    public string CurrentSeatId { get; private set; }

    public bool TryEnterVehicle(VehicleSeatManager manager)
    {
        if (manager == null) return false;
        if (manager.TryEnterSeat(playerId, preferredRole, transform.position, true))
        {
            CurrentSeatManager = manager;
            if (!manager.TryGetSeatIdForPlayer(playerId, out var seatId))
                seatId = null;
            CurrentSeatId = seatId;
            return true;
        }
        return false;
    }

    public bool TryEnterSpecificSeat(VehicleSeatManager manager, string seatId)
    {
        if (manager == null || string.IsNullOrWhiteSpace(seatId)) return false;
        if (manager.TryEnterSeatById(playerId, seatId, transform.position, true))
        {
            CurrentSeatManager = manager;
            CurrentSeatId = seatId;
            return true;
        }
        return false;
    }

    public void ExitVehicle()
    {
        if (CurrentSeatManager == null) return;
        CurrentSeatManager.ExitSeat(playerId);
        ClearSeatState();
    }

    public void AttachSeatState(VehicleSeatManager manager, string seatId)
    {
        CurrentSeatManager = manager;
        CurrentSeatId = seatId;
    }

    public void ClearSeatState()
    {
        CurrentSeatManager = null;
        CurrentSeatId = null;
    }
}
