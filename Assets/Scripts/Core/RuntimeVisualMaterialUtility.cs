using UnityEngine;
using UnityEngine.Rendering;

namespace PVZ3D.Core
{
    public static class RuntimeVisualMaterialUtility
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock sharedBlock;
        private static Material runtimeFallbackMaterial;

        public static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            EnsureValidBaseMaterial(renderer);

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            int colorPropertyId = ResolveColorPropertyId(material);
            if (colorPropertyId == -1)
            {
                return;
            }

            if (sharedBlock == null)
            {
                sharedBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(sharedBlock);
            sharedBlock.SetColor(colorPropertyId, color);
            renderer.SetPropertyBlock(sharedBlock);
        }

        public static void EnsureRendererHasValidMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            EnsureValidBaseMaterial(renderer);
        }

        private static void EnsureValidBaseMaterial(Renderer renderer)
        {
            Material current = renderer.sharedMaterial;
            if (IsUsableMaterial(current))
            {
                return;
            }

            Material fallback = GetPipelineDefaultMaterial();
            if (fallback != null)
            {
                renderer.sharedMaterial = fallback;
            }
        }

        private static Material GetPipelineDefaultMaterial()
        {
            RenderPipelineAsset activeRp = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline
                : GraphicsSettings.defaultRenderPipeline;

            if (activeRp != null && activeRp.defaultMaterial != null)
            {
                return activeRp.defaultMaterial;
            }

            // In edit mode, avoid assigning transient materials into scene references.
            if (!Application.isPlaying)
            {
                return null;
            }

            if (runtimeFallbackMaterial != null && IsUsableMaterial(runtimeFallbackMaterial))
            {
                return runtimeFallbackMaterial;
            }

            string[] shaderCandidates =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Standard",
                "Unlit/Color",
            };

            for (int i = 0; i < shaderCandidates.Length; i++)
            {
                Shader shader = Shader.Find(shaderCandidates[i]);
                if (shader == null || !shader.isSupported)
                {
                    continue;
                }

                runtimeFallbackMaterial = new Material(shader)
                {
                    name = "PVZ_RuntimeFallbackMaterial",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                return runtimeFallbackMaterial;
            }

            return null;
        }

        private static bool IsUsableMaterial(Material material)
        {
            if (material == null)
            {
                return false;
            }

            Shader shader = material.shader;
            if (shader == null || !shader.isSupported)
            {
                return false;
            }

            string shaderName = shader.name ?? string.Empty;
            if (shaderName.Contains("Hidden/InternalErrorShader"))
            {
                return false;
            }

            return true;
        }

        private static int ResolveColorPropertyId(Material material)
        {
            if (material.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }

            if (material.HasProperty(ColorId))
            {
                return ColorId;
            }

            return -1;
        }
    }
}
