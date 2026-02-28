using System.Collections.Generic;
using UnityEngine;

public class VehicleSeatManager : MonoBehaviour
{
    public VehicleConfig config;
    public Transform seatRoot;
    public bool autoConfigureOnAwake = true;

    private readonly Dictionary<string, SeatRuntime> _seats = new();
    private readonly Dictionary<int, string> _occupantToSeat = new();
    private float _lastSeatSwapTime;

    public int OccupantCount => _occupantToSeat.Count;

    private void Awake()
    {
        if (autoConfigureOnAwake)
        {
            if (seatRoot == null) seatRoot = transform;
            RebuildSeats();
        }
    }

    public void RebuildSeats()
    {
        _seats.Clear();
        if (config == null || config.seats == null) return;
        if (seatRoot == null) seatRoot = transform;

        for (int i = 0; i < config.seats.Count; i++)
        {
            var seat = config.seats[i];
            if (seat == null) continue;
            if (string.IsNullOrWhiteSpace(seat.id))
                seat.id = $"{seat.role.ToString().ToLowerInvariant()}_{i + 1}";
            _seats[seat.id] = new SeatRuntime(seat);
        }
    }

    public bool TryEnterSeat(int playerId, VehicleConfig.SeatRole preferredRole)
    {
        return TryEnterSeat(playerId, preferredRole, seatRoot != null ? seatRoot.position : transform.position, false);
    }

    public bool TryEnterSeat(int playerId, VehicleConfig.SeatRole preferredRole, Vector3 playerWorldPosition, bool enforceDistance = true)
    {
        if (config == null) return false;
        if (Time.time - _lastSeatSwapTime < config.seatSettings.seatSwapCooldown) return false;

        if (_occupantToSeat.ContainsKey(playerId))
            return false;

        if (!config.seatSettings.allowMultipleOccupants && _occupantToSeat.Count > 0)
            return false;
        if (!config.multiplayer.allowSharedVehicleOccupancy && _occupantToSeat.Count > 0)
            return false;

        if (_occupantToSeat.Count >= Mathf.Max(1, config.seatSettings.maxOccupants))
            return false;

        var seatId = FindAvailableSeat(preferredRole, playerWorldPosition, enforceDistance);
        if (string.IsNullOrEmpty(seatId))
            return false;

        _occupantToSeat[playerId] = seatId;
        if (_seats.TryGetValue(seatId, out var seat))
            seat.occupantId = playerId;

        _lastSeatSwapTime = Time.time;
        return true;
    }

    public bool TryEnterSeatById(int playerId, string seatId, Vector3 playerWorldPosition, bool enforceDistance = true)
    {
        if (config == null || string.IsNullOrWhiteSpace(seatId)) return false;
        if (!_seats.TryGetValue(seatId, out var seat) || seat.occupantId >= 0) return false;
        if (_occupantToSeat.ContainsKey(playerId)) return false;
        if (!config.seatSettings.allowMultipleOccupants && _occupantToSeat.Count > 0) return false;
        if (!config.multiplayer.allowSharedVehicleOccupancy && _occupantToSeat.Count > 0) return false;
        if (_occupantToSeat.Count >= Mathf.Max(1, config.seatSettings.maxOccupants)) return false;
        if (enforceDistance && !IsWithinEnterDistance(seatId, playerWorldPosition)) return false;

        _occupantToSeat[playerId] = seatId;
        seat.occupantId = playerId;
        _lastSeatSwapTime = Time.time;
        return true;
    }

    public void ExitSeat(int playerId)
    {
        if (!_occupantToSeat.TryGetValue(playerId, out var seatId))
            return;

        _occupantToSeat.Remove(playerId);
        if (_seats.TryGetValue(seatId, out var seat))
            seat.occupantId = -1;

        _lastSeatSwapTime = Time.time;
    }

    public bool TryGetSeatIdForPlayer(int playerId, out string seatId)
    {
        return _occupantToSeat.TryGetValue(playerId, out seatId);
    }

    public bool GetSeatWorldPose(string seatId, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (config == null || config.seats == null) return false;
        if (!_seats.TryGetValue(seatId, out var seat)) return false;

        var root = seatRoot != null ? seatRoot : transform;
        var data = seat.data;

        if (data.overrideTransform != null)
        {
            position = data.overrideTransform.position;
            rotation = data.overrideTransform.rotation;
            return true;
        }

        position = root.TransformPoint(data.localPosition);
        rotation = root.rotation * Quaternion.Euler(data.localEuler);
        return true;
    }

    public bool HasDriver()
    {
        foreach (var seat in _seats.Values)
        {
            if (seat.data.role == VehicleConfig.SeatRole.Driver && seat.occupantId >= 0)
                return true;
        }
        return false;
    }

    public bool IsSeatOccupied(string seatId)
    {
        return _seats.TryGetValue(seatId, out var seat) && seat.occupantId >= 0;
    }

    private string FindAvailableSeat(VehicleConfig.SeatRole preferredRole, Vector3 playerWorldPosition, bool enforceDistance)
    {
        foreach (var seat in _seats.Values)
        {
            if (seat.data.role == preferredRole && seat.occupantId < 0 &&
                (!enforceDistance || IsWithinEnterDistance(seat.data.id, playerWorldPosition)))
                return seat.data.id;
        }

        foreach (var seat in _seats.Values)
        {
            if (seat.occupantId < 0 &&
                (!enforceDistance || IsWithinEnterDistance(seat.data.id, playerWorldPosition)))
                return seat.data.id;
        }

        return null;
    }

    private bool IsWithinEnterDistance(string seatId, Vector3 playerWorldPosition)
    {
        if (!GetSeatWorldPose(seatId, out var seatPos, out _))
            return false;

        float maxDist = Mathf.Max(0f, config.seatSettings.enterDistance);
        return Vector3.Distance(playerWorldPosition, seatPos) <= maxDist;
    }

    private class SeatRuntime
    {
        public readonly VehicleConfig.SeatAnchor data;
        public int occupantId = -1;

        public SeatRuntime(VehicleConfig.SeatAnchor data)
        {
            this.data = data;
        }
    }
}
