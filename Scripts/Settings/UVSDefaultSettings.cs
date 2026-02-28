using UnityEngine;

public class UVSDefaultSettings : UVSSettingsBase
{
    [UVSSetting("Master Volume", "Audio", 0f, 1f)]
    public float masterVolume = 0.8f;

    [UVSSetting("Show Debug", "General")]
    public bool showDebug = false;

    [UVSSetting("Quality Level", "Graphics")]
    public int qualityLevel = 2;
}
