using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UVS/Rendering/Pipeline Shader Fallback Profile", fileName = "PipelineShaderFallbackProfile")]
public class PipelineShaderFallbackProfile : ScriptableObject
{
    public enum RenderPipelineTarget
    {
        BuiltIn,
        URP,
        HDRP
    }

    [Serializable]
    public class ShaderFallbackRule
    {
        public string sourceShaderContains;
        public string fallbackShaderName;
    }

    public const string DefaultResourcePath = "PipelineShaderFallbackProfile_Default";

    public string builtInDefaultShader = "Standard";
    public string urpDefaultShader = "Universal Render Pipeline/Lit";
    public string hdrpDefaultShader = "HDRP/Lit";

    public List<ShaderFallbackRule> builtInRules = new();
    public List<ShaderFallbackRule> urpRules = new();
    public List<ShaderFallbackRule> hdrpRules = new();

    public static PipelineShaderFallbackProfile GetDefault()
    {
        return Resources.Load<PipelineShaderFallbackProfile>(DefaultResourcePath);
    }

    public string ResolveFallback(RenderPipelineTarget target, string sourceShaderName)
    {
        var rules = GetRules(target);
        if (!string.IsNullOrWhiteSpace(sourceShaderName))
        {
            foreach (var rule in rules)
            {
                if (rule == null ||
                    string.IsNullOrWhiteSpace(rule.sourceShaderContains) ||
                    string.IsNullOrWhiteSpace(rule.fallbackShaderName))
                {
                    continue;
                }

                if (sourceShaderName.IndexOf(rule.sourceShaderContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return rule.fallbackShaderName;
            }
        }

        return target switch
        {
            RenderPipelineTarget.URP => urpDefaultShader,
            RenderPipelineTarget.HDRP => hdrpDefaultShader,
            _ => builtInDefaultShader
        };
    }

    private List<ShaderFallbackRule> GetRules(RenderPipelineTarget target)
    {
        return target switch
        {
            RenderPipelineTarget.URP => urpRules,
            RenderPipelineTarget.HDRP => hdrpRules,
            _ => builtInRules
        };
    }
}
