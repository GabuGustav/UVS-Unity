using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class UVSSettingAttribute : Attribute
{
    public readonly string Label;
    public readonly string Group;
    public readonly float Min;
    public readonly float Max;
    public readonly bool HasRange;

    public UVSSettingAttribute(string label = null, string group = null)
    {
        Label = label;
        Group = group;
        HasRange = false;
        Min = 0f;
        Max = 0f;
    }

    public UVSSettingAttribute(string label, string group, float min, float max)
    {
        Label = label;
        Group = group;
        Min = min;
        Max = max;
        HasRange = true;
    }
}

public abstract class UVSSettingsBase : ScriptableObject
{
}
