using UnityEngine;

public class WaterSurface : MonoBehaviour
{
    public struct SampleResult
    {
        public float height;
        public Vector3 normal;
    }

    [Header("Surface")]
    public float baseHeight = 0f;

    [Header("Primary Wave")]
    public Vector2 waveDirection = new(1f, 0f);
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 0.2f;
    public float waveSpeed = 1f;

    [Header("Secondary Wave")]
    public Vector2 secondaryDirection = new(-0.7f, 0.7f);
    public float secondaryAmplitude = 0.25f;
    public float secondaryFrequency = 0.4f;
    public float secondarySpeed = 0.5f;

    [Header("Chop Wave")]
    public Vector2 chopDirection = new(0.35f, 0.9f);
    public float chopAmplitude = 0.12f;
    public float chopFrequency = 0.9f;
    public float chopSpeed = 1.4f;

    [Header("Optional Material Sync")]
    public Material waterMaterial;

    [Header("Optional Runtime Render Settings")]
    public VehicleConfig.WaterRenderSettings renderSettings;

    public float GetHeight(Vector3 worldPos)
    {
        return SampleHeightAndNormal(worldPos).height;
    }

    public SampleResult SampleHeightAndNormal(Vector3 worldPos)
    {
        float t = Time.time;
        float h = baseHeight;

        Vector2 d1 = waveDirection.sqrMagnitude > 0.0001f ? waveDirection.normalized : Vector2.right;
        Vector2 d2 = secondaryDirection.sqrMagnitude > 0.0001f ? secondaryDirection.normalized : new Vector2(-0.7f, 0.7f).normalized;
        Vector2 d3 = chopDirection.sqrMagnitude > 0.0001f ? chopDirection.normalized : new Vector2(0.35f, 0.9f).normalized;

        float dhdx = 0f;
        float dhdz = 0f;

        AddWave(worldPos, t, d1, waveAmplitude, waveFrequency, waveSpeed, ref h, ref dhdx, ref dhdz);
        AddWave(worldPos, t, d2, secondaryAmplitude, secondaryFrequency, secondarySpeed, ref h, ref dhdx, ref dhdz);
        AddWave(worldPos, t, d3, chopAmplitude, chopFrequency, chopSpeed, ref h, ref dhdx, ref dhdz);

        var normal = new Vector3(-dhdx, 1f, -dhdz).normalized;

        return new SampleResult
        {
            height = h,
            normal = normal
        };
    }

    public void ApplyRenderSettings(VehicleConfig.WaterRenderSettings settings)
    {
        if (settings == null) return;

        renderSettings ??= new VehicleConfig.WaterRenderSettings();
        renderSettings.qualityTier = settings.qualityTier;
        renderSettings.foamEnabled = settings.foamEnabled;
        renderSettings.depthColorEnabled = settings.depthColorEnabled;
        renderSettings.causticsEnabled = settings.causticsEnabled;

        ApplyMaterialSettings();
    }

    private void Update()
    {
        ApplyMaterialSettings();
    }

    private void ApplyMaterialSettings()
    {
        if (waterMaterial == null) return;

        var d1 = waveDirection.sqrMagnitude > 0.0001f ? waveDirection.normalized : Vector2.right;
        var d2 = secondaryDirection.sqrMagnitude > 0.0001f ? secondaryDirection.normalized : new Vector2(-0.7f, 0.7f).normalized;
        var d3 = chopDirection.sqrMagnitude > 0.0001f ? chopDirection.normalized : new Vector2(0.35f, 0.9f).normalized;

        waterMaterial.SetFloat("_BaseHeight", baseHeight);

        waterMaterial.SetVector("_WaveDirA", new Vector4(d1.x, d1.y, 0f, 0f));
        waterMaterial.SetFloat("_WaveAmpA", waveAmplitude);
        waterMaterial.SetFloat("_WaveFreqA", waveFrequency);
        waterMaterial.SetFloat("_WaveSpeedA", waveSpeed);

        waterMaterial.SetVector("_WaveDirB", new Vector4(d2.x, d2.y, 0f, 0f));
        waterMaterial.SetFloat("_WaveAmpB", secondaryAmplitude);
        waterMaterial.SetFloat("_WaveFreqB", secondaryFrequency);
        waterMaterial.SetFloat("_WaveSpeedB", secondarySpeed);

        waterMaterial.SetVector("_WaveDirC", new Vector4(d3.x, d3.y, 0f, 0f));
        waterMaterial.SetFloat("_WaveAmpC", chopAmplitude);
        waterMaterial.SetFloat("_WaveFreqC", chopFrequency);
        waterMaterial.SetFloat("_WaveSpeedC", chopSpeed);

        if (renderSettings != null)
        {
            waterMaterial.SetFloat("_QualityTier", (float)renderSettings.qualityTier);
            SetKeyword(waterMaterial, "UVS_WATER_FOAM", renderSettings.foamEnabled);
            SetKeyword(waterMaterial, "UVS_WATER_DEPTH", renderSettings.depthColorEnabled);
            SetKeyword(waterMaterial, "UVS_WATER_CAUSTICS", renderSettings.causticsEnabled);
        }
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    private static void AddWave(
        Vector3 worldPos,
        float t,
        Vector2 direction,
        float amplitude,
        float frequency,
        float speed,
        ref float height,
        ref float dhdx,
        ref float dhdz)
    {
        float phase = (worldPos.x * direction.x + worldPos.z * direction.y) * frequency + t * speed;
        float sin = Mathf.Sin(phase);
        float cos = Mathf.Cos(phase);

        height += sin * amplitude;

        float slope = amplitude * frequency * cos;
        dhdx += slope * direction.x;
        dhdz += slope * direction.y;
    }
}
