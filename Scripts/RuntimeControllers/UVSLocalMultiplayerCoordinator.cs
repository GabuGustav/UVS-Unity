using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UVSLocalMultiplayerCoordinator : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private PlayerInputManager playerInputManager;

    [Header("Vehicles")]
    [SerializeField] private bool autoDiscoverSeatManagers = true;
    [SerializeField] private List<VehicleSeatManager> seatManagers = new();

    [Header("Seat Join")]
    [SerializeField] private bool autoSeatOnJoin = true;
    [SerializeField] private bool followSeatPose = true;

    private readonly List<PlayerInput> _players = new();
    private IVehicleNetworkBackend _networkBackend;

    private void OnEnable()
    {
        if (playerInputManager == null)
            playerInputManager = PlayerInputManager.instance ?? FindFirstObjectByType<PlayerInputManager>();

        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
        }

        if (autoDiscoverSeatManagers)
            RefreshSeatManagers();

        ConfigureBackend();

        foreach (var player in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            RegisterPlayer(player, autoSeat: false);

        UpdateSplitScreenLayout();
    }

    private void OnDisable()
    {
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    private void Update()
    {
        _networkBackend?.Tick(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!followSeatPose) return;

        foreach (var player in _players)
        {
            if (player == null) continue;
            var occupant = player.GetComponent<VehicleSeatOccupant>();
            if (occupant == null || occupant.CurrentSeatManager == null || string.IsNullOrEmpty(occupant.CurrentSeatId))
                continue;

            if (occupant.CurrentSeatManager.GetSeatWorldPose(occupant.CurrentSeatId, out var seatPos, out var seatRot))
            {
                player.transform.SetPositionAndRotation(seatPos, seatRot);
            }
        }
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        RegisterPlayer(player, autoSeatOnJoin);
        UpdateSplitScreenLayout();
    }

    private void OnPlayerLeft(PlayerInput player)
    {
        if (player == null) return;

        var occupant = player.GetComponent<VehicleSeatOccupant>();
        occupant?.ExitVehicle();

        _players.Remove(player);
        UpdateSplitScreenLayout();
    }

    private void RegisterPlayer(PlayerInput player, bool autoSeat)
    {
        if (player == null || _players.Contains(player)) return;
        _players.Add(player);

        var occupant = player.GetComponent<VehicleSeatOccupant>();
        if (occupant == null) occupant = player.gameObject.AddComponent<VehicleSeatOccupant>();
        occupant.playerId = player.playerIndex;

        if (autoSeat)
            TrySeatPlayer(player, occupant);
    }

    private void TrySeatPlayer(PlayerInput player, VehicleSeatOccupant occupant)
    {
        if (occupant == null || player == null || seatManagers == null || seatManagers.Count == 0)
            return;

        var orderedManagers = seatManagers
            .Where(m => m != null && m.config != null)
            .OrderBy(m => Vector3.Distance(player.transform.position, m.transform.position))
            .ToList();

        foreach (var manager in orderedManagers)
        {
            if (!manager.config.multiplayer.allowSharedVehicleOccupancy && manager.OccupantCount > 0)
                continue;

            var desiredRole = manager.HasDriver() ? VehicleConfig.SeatRole.Passenger : VehicleConfig.SeatRole.Driver;
            bool entered = false;

            bool onlineEnabled = manager.config.multiplayer.enableOnline && _networkBackend != null && _networkBackend.IsAvailable;
            if (onlineEnabled)
            {
                entered = _networkBackend.RequestSeatEnter(player.playerIndex, manager, desiredRole);
            }
            else
            {
                entered = manager.TryEnterSeat(player.playerIndex, desiredRole, player.transform.position, true);
            }

            if (!entered) continue;

            if (!manager.TryGetSeatIdForPlayer(player.playerIndex, out var seatId))
                continue;

            occupant.AttachSeatState(manager, seatId);
            occupant.preferredRole = desiredRole;

            if (manager.GetSeatWorldPose(seatId, out var seatPos, out var seatRot))
            {
                player.transform.SetPositionAndRotation(seatPos, seatRot);
            }

            return;
        }
    }

    public void RefreshSeatManagers()
    {
        seatManagers = FindObjectsByType<VehicleSeatManager>(FindObjectsSortMode.None)
            .Where(m => m != null)
            .ToList();
    }

    private void ConfigureBackend()
    {
        string backendId = "local";
        bool onlineRequested = false;

        var firstConfig = seatManagers.FirstOrDefault(m => m != null && m.config != null)?.config;
        if (firstConfig != null)
        {
            onlineRequested = firstConfig.multiplayer.enableOnline;
            backendId = onlineRequested ? firstConfig.multiplayer.onlineProviderId : "local";
        }

        _networkBackend = VehicleNetworkBackendFactory.Create(backendId);
        if (_networkBackend == null)
            _networkBackend = new UVSLocalOnlyBackend();

        bool initialized = _networkBackend.Initialize();
        if (onlineRequested && (!initialized || !_networkBackend.IsAvailable))
        {
            Debug.LogWarning($"[UVS] Online backend '{backendId}' unavailable. Using local backend.");
            _networkBackend = new UVSLocalOnlyBackend();
            _networkBackend.Initialize();
        }
    }

    public void UpdateSplitScreenLayout()
    {
        var config = seatManagers.FirstOrDefault(m => m != null && m.config != null)?.config;
        int maxPlayers = Mathf.Clamp(config != null ? config.multiplayer.localMaxPlayers : 4, 1, 4);
        bool splitEnabled = config == null || config.multiplayer.enableLocalSplitScreen;

        int activePlayers = Mathf.Min(_players.Count, maxPlayers);
        int total = Mathf.Clamp(activePlayers, 1, 4);

        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            if (player == null) continue;

            var cam = player.camera;
            if (cam == null)
                cam = player.GetComponentInChildren<Camera>(true);

            if (cam == null) continue;

            bool enabled = i < activePlayers;
            cam.enabled = enabled;
            if (!enabled) continue;

            cam.rect = splitEnabled ? GetViewportRect(i, total) : new Rect(0f, 0f, 1f, 1f);
        }
    }

    private static Rect GetViewportRect(int index, int total)
    {
        if (total <= 1)
            return new Rect(0f, 0f, 1f, 1f);

        if (total == 2)
        {
            return index == 0
                ? new Rect(0f, 0f, 0.5f, 1f)
                : new Rect(0.5f, 0f, 0.5f, 1f);
        }

        if (total == 3)
        {
            return index switch
            {
                0 => new Rect(0f, 0.5f, 1f, 0.5f),
                1 => new Rect(0f, 0f, 0.5f, 0.5f),
                _ => new Rect(0.5f, 0f, 0.5f, 0.5f)
            };
        }

        float x = (index % 2) * 0.5f;
        float y = index < 2 ? 0.5f : 0f;
        return new Rect(x, y, 0.5f, 0.5f);
    }
}
