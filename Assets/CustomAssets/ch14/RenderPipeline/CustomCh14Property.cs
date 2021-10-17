using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    public class CustomCh14Property
    {
        public static readonly ShaderTagId PreDepthTag = new ShaderTagId("CustomCh14PreDepth");
        public static readonly ShaderTagId ShadowTag = new ShaderTagId("CustomCh14Shadow");
        public static readonly ShaderTagId GBufferTag = new ShaderTagId("CustomCh14GBuffer");
        public static readonly ShaderTagId ForwardLightingTag = new ShaderTagId("CustomCh14Forward");
        
        public static readonly int PreDepthTexId = Shader.PropertyToID("_PreDepthTex");
        public static readonly int ShadowDepthTexId = Shader.PropertyToID("_ShadowDepthTex");
        public static readonly int ShadowVsmDepthTexId = Shader.PropertyToID("_ShadowVsmDepthTex");
        public static readonly int GBufferAlbedoTexId = Shader.PropertyToID("_GBufferAlbedoTex");
        public static readonly int GBufferNormalTexId = Shader.PropertyToID("_GBufferNormalTex");
        public static readonly int GBufferWorldPosTexId = Shader.PropertyToID("_GBufferWorldPosTex");
        public static readonly int GBufferMetallicTexId = Shader.PropertyToID("_GBufferMetallicTex");
        public static readonly int GBufferShadowTexId = Shader.PropertyToID("_GBufferShadowTex");
        public static readonly int GBufferDepthTexId = Shader.PropertyToID("_GBufferDepthTex");
        public static readonly int OpaqueColorTexId = Shader.PropertyToID("_OpaqueColorTex");
        public static readonly int OffCameraColorTexId = Shader.PropertyToID("_OffCameraColorTex");

        public static readonly RenderTargetIdentifier PreDepthTex = new RenderTargetIdentifier(PreDepthTexId);
        public static readonly RenderTargetIdentifier ShadowDepthTex = new RenderTargetIdentifier(ShadowDepthTexId);
        public static readonly RenderTargetIdentifier ShadowVsmDepthTex = new RenderTargetIdentifier(ShadowVsmDepthTexId);
        public static readonly RenderTargetIdentifier GBufferAlbedo = new RenderTargetIdentifier(GBufferAlbedoTexId);
        public static readonly RenderTargetIdentifier GBufferNormal = new RenderTargetIdentifier(GBufferNormalTexId);
        public static readonly RenderTargetIdentifier GBufferWorldPos = new RenderTargetIdentifier(GBufferWorldPosTexId);
        public static readonly RenderTargetIdentifier GBufferMetallic = new RenderTargetIdentifier(GBufferMetallicTexId);
        public static readonly RenderTargetIdentifier GBufferShadow = new RenderTargetIdentifier(GBufferShadowTexId);
        public static readonly RenderTargetIdentifier GBufferDepthTex = new RenderTargetIdentifier(GBufferDepthTexId);
        public static readonly RenderTargetIdentifier OpaqueColorTex = new RenderTargetIdentifier(OpaqueColorTexId);
        public static readonly RenderTargetIdentifier OffCameraColorTex = new RenderTargetIdentifier(OffCameraColorTexId);
        public static readonly RenderTargetIdentifier OnCameraColorTex = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        public static readonly RenderTargetIdentifier OnCameraDepthTex = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);

        public static readonly RenderTargetIdentifier[] GBufferColorLayout = new RenderTargetIdentifier[]
        {
            GBufferAlbedo,
            GBufferNormal,
            GBufferWorldPos,
            GBufferMetallic,
            GBufferShadow,
        };
        
        public static readonly int LvpMatId = Shader.PropertyToID("_WorldToLightViewProjection");
        public static readonly int LightViewMatId = Shader.PropertyToID("_LightView");
        public static readonly int LightZParamId = Shader.PropertyToID("_LightZParam");
        public static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBias");
        public static readonly int ShadowNormalBiasId = Shader.PropertyToID("_ShadowNormalBias");
        public static readonly int LightDirId = Shader.PropertyToID("_LightDir");
        public static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        public static readonly int AmbientLightColorId = Shader.PropertyToID("_AmbientLightColor");

        public static readonly int PcfSampleCountId = Shader.PropertyToID("_PcfSampleCount");
        public static readonly int PcfSampleSpaceId = Shader.PropertyToID("_PcfSampleSpace");

        public const string PcfKeyword = "PCF_SHADOW";
        public const string VsmKeyword = "VSM_SHADOW";
        
        public struct Property {
            public CustomCh14RenderPipelineAsset asset;
            public ScriptableRenderContext context;
            public CommandBuffer commandBuffer;
            public CullingResults cullingResults;
            public Camera camera;
            public Vector2Int shadowResolution;
            public Vector2Int cameraResolution;
            public int mainLightIndex;
        }
    }
}