using UnityEngine;

[CreateAssetMenu(menuName = "UVS/AI/Driver Profile", fileName = "DriverProfile")]
public class DriverProfile : ScriptableObject
{
    public string profileName = "Default";
    public Vector2 reactionTimeRange = new Vector2(0.2f, 0.8f);
    [Range(0f, 1f)] public float aggression = 0.5f;
    [Range(0f, 1f)] public float driftTendency = 0.2f;
    [Range(0f, 1f)] public float parkingSkill = 0.5f;

    public float SampleReactionTime()
    {
        return Random.Range(reactionTimeRange.x, reactionTimeRange.y);
    }
}
