using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class SplitScreenCamManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Camera cameraPrefab;
    [SerializeField] private CinemachineFreeLook vcamPrefab;

    private readonly List<Camera> activeCameras = new();
    private readonly List<PlayerInput> activePlayers = new();
    private readonly List<CinemachineFreeLook> activeVCams = new();
    private PlayerInputManager _inputManager;

    private void OnEnable()
    {
        _inputManager = PlayerInputManager.instance;
        if (_inputManager == null)
            _inputManager = FindFirstObjectByType<PlayerInputManager>();

        if (_inputManager != null)
            _inputManager.onPlayerJoined += OnPlayerJoined;
        else
            Debug.LogError("SplitScreenCamManager: No PlayerInputManager found in scene.");
    }

    private void OnDisable()
    {
        if (_inputManager != null)
            _inputManager.onPlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        if (player == null) return;
        if (cameraPrefab == null || vcamPrefab == null)
        {
            Debug.LogError("SplitScreenCamManager: Missing cameraPrefab or vcamPrefab.");
            return;
        }

        activePlayers.Add(player);
        var channel = GetChannel(player.playerIndex);

        // Create camera
        Camera cam = Instantiate(cameraPrefab);
        cam.name = $"PlayerCamera_{player.playerIndex}";

        CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.ChannelMask = channel;

        // Create virtual camera
        CinemachineFreeLook vcam = Instantiate(vcamPrefab);
        vcam.name = $"PlayerVCam_{player.playerIndex}";
        vcam.OutputChannel = channel;

        // Bind player transform
        vcam.Follow = player.transform;
        vcam.LookAt = player.transform;

        activeCameras.Add(cam);
        activeVCams.Add(vcam);

        UpdateSplitScreen();
    }

    /// <summary>
    /// Call this whenever player count changes OR split screen is toggled
    /// </summary>
    public void UpdateSplitScreen()
    {
        int count = activeCameras.Count;

        for (int i = 0; i < count; i++)
        {
            activeCameras[i].rect = GetSplitRect(i, count);
        }
    }

    private Rect GetSplitRect(int index, int total)
    {
        if (total == 1)
            return new Rect(0, 0, 1, 1);

        if (total == 2)
        {
            // Horizontal split
            return index == 0
                ? new Rect(0, 0.5f, 1, 0.5f)
                : new Rect(0, 0, 1, 0.5f);
        }

        if (total <= 4)
        {
            float x = index % 2 == 0 ? 0 : 0.5f;
            float y = index < 2 ? 0.5f : 0f;
            return new Rect(x, y, 0.5f, 0.5f);
        }

        // Fallback
        return new Rect(0, 0, 1, 1);
    }

    private static OutputChannels GetChannel(int playerIndex)
    {
        int idx = Mathf.Clamp(playerIndex, 0, 15);
        return (OutputChannels)(1 << idx);
    }
}
