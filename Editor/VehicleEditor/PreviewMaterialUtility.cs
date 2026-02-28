using System.Collections.Generic;
using UnityEngine;

namespace UVS.Editor.Core
{
    internal static class PreviewMaterialUtility
    {
        public static Material ResolvePreviewMaterial(
            Material source,
            PipelineShaderFallbackProfile.RenderPipelineTarget target,
            List<Material> tempMaterials)
        {
            if (source == null) return null;
            if (IsCompatible(source, target)) return source;

            var fallbackProfile = PipelineShaderFallbackProfile.GetDefault();
            string fallbackName = fallbackProfile != null
                ? fallbackProfile.ResolveFallback(target, source.shader != null ? source.shader.name : string.Empty)
                : GetDefaultFallbackName(target);

            var fallbackShader = Shader.Find(fallbackName);
            if (fallbackShader == null)
                return source;

            var transient = new Material(fallbackShader) { hideFlags = HideFlags.HideAndDontSave };
            CopyCommonProperties(source, transient);
            tempMaterials?.Add(transient);
            return transient;
        }

        public static void CleanupMaterials(List<Material> materials)
        {
            if (materials == null || materials.Count == 0) return;
            foreach (var material in materials)
            {
                if (material != null)
                    Object.DestroyImmediate(material);
            }
            materials.Clear();
        }

        private static bool IsCompatible(Material material, PipelineShaderFallbackProfile.RenderPipelineTarget target)
        {
            if (material == null || material.shader == null) return false;
            if (material.shader.name == "Hidden/InternalErrorShader") return false;
            if (!material.shader.isSupported) return false;

            string shaderName = material.shader.name;
            string rpTag = material.GetTag("RenderPipeline", false, string.Empty);

            return target switch
            {
                PipelineShaderFallbackProfile.RenderPipelineTarget.URP =>
                    string.IsNullOrEmpty(rpTag) ||
                    rpTag.IndexOf("Universal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    shaderName.StartsWith("Universal Render Pipeline/"),

                PipelineShaderFallbackProfile.RenderPipelineTarget.HDRP =>
                    string.IsNullOrEmpty(rpTag) ||
                    rpTag.IndexOf("HD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    shaderName.StartsWith("HDRP/"),

                _ => string.IsNullOrEmpty(rpTag)
            };
        }

        private static string GetDefaultFallbackName(PipelineShaderFallbackProfile.RenderPipelineTarget target)
        {
            return target switch
            {
                PipelineShaderFallbackProfile.RenderPipelineTarget.URP => "Universal Render Pipeline/Lit",
                PipelineShaderFallbackProfile.RenderPipelineTarget.HDRP => "HDRP/Lit",
                _ => "Standard"
            };
        }

        private static void CopyCommonProperties(Material src, Material dst)
        {
            if (src == null || dst == null) return;

            if (src.HasProperty("_BaseMap") && dst.HasProperty("_BaseMap"))
                dst.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
            else if (src.HasProperty("_MainTex") && dst.HasProperty("_BaseMap"))
                dst.SetTexture("_BaseMap", src.GetTexture("_MainTex"));
            else if (src.HasProperty("_MainTex") && dst.HasProperty("_MainTex"))
                dst.SetTexture("_MainTex", src.GetTexture("_MainTex"));

            if (src.HasProperty("_BaseColor") && dst.HasProperty("_BaseColor"))
                dst.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            else if (src.HasProperty("_Color") && dst.HasProperty("_BaseColor"))
                dst.SetColor("_BaseColor", src.GetColor("_Color"));
            else if (src.HasProperty("_Color") && dst.HasProperty("_Color"))
                dst.SetColor("_Color", src.GetColor("_Color"));

            if (src.HasProperty("_Metallic") && dst.HasProperty("_Metallic"))
                dst.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (src.HasProperty("_Glossiness") && dst.HasProperty("_Smoothness"))
                dst.SetFloat("_Smoothness", src.GetFloat("_Glossiness"));
            else if (src.HasProperty("_Smoothness") && dst.HasProperty("_Smoothness"))
                dst.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));

            if (src.HasProperty("_BumpMap") && dst.HasProperty("_BumpMap"))
                dst.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
        }
    }
}
