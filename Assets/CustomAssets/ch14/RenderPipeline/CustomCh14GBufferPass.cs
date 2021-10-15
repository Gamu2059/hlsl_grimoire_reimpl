using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// G-Buffer描画パス
    /// </summary>
    public class CustomCh14GBufferPass
    {
        public struct Property
        {
            public ScriptableRenderContext context;
            public CommandBuffer commandBuffer;
            public CullingResults cullingResults;
            public Camera camera;
        }
        
        public static readonly ShaderTagId GBufferTag = new ShaderTagId("CustomCh14GBuffer");
        public static readonly int GBufferAlbedoTexId = Shader.PropertyToID("GBufferAlbedo");
        public static readonly int GBufferNormalTexId = Shader.PropertyToID("GBufferNormal");
        public static readonly int GBufferWorldPosTexId = Shader.PropertyToID("GBufferWorldPos");
        public static readonly int GBufferMetallicTexId = Shader.PropertyToID("GBufferMetallic");
        public static readonly int GBufferShadowTexId = Shader.PropertyToID("GBufferShadow");
        public static readonly int GBufferDepthTexId = Shader.PropertyToID("GBufferDepth");

        private static readonly RenderTargetIdentifier GBufferAlbedo = new RenderTargetIdentifier(GBufferAlbedoTexId);
        private static readonly RenderTargetIdentifier GBufferNormal = new RenderTargetIdentifier(GBufferNormalTexId);
        private static readonly RenderTargetIdentifier GBufferWorldPos = new RenderTargetIdentifier(GBufferWorldPosTexId);
        private static readonly RenderTargetIdentifier GBufferMetallic = new RenderTargetIdentifier(GBufferMetallicTexId);
        private static readonly RenderTargetIdentifier GBufferShadow = new RenderTargetIdentifier(GBufferShadowTexId);
        private static readonly RenderTargetIdentifier GBufferDepth = new RenderTargetIdentifier(GBufferDepthTexId);

        private static readonly RenderTargetIdentifier[] GBufferColorLayout = new RenderTargetIdentifier[]
        {
            GBufferAlbedo,
            GBufferNormal,
            // GBufferWorldPos,
        };

        private Property property;

        /// <summary>
        /// G-Bufferプロパティのセットアップ
        /// </summary>
        public void SetupProperty(Property property)
        {
            this.property = property;
        }

        /// <summary>
        /// G-Bufferのレンダーテクスチャのセットアップ
        /// </summary>
        public void SetupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var camera = property.camera;
            
            var w = Display.main.renderingWidth;
            var h = Display.main.renderingHeight;
            var targetTexture = camera.targetTexture;
            if (targetTexture != null)
            {
                w = targetTexture.width;
                h = targetTexture.height;
            }
            
            cmd.Clear();
            cmd.GetTemporaryRT(GBufferAlbedoTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(GBufferNormalTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(GBufferWorldPosTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);
            cmd.GetTemporaryRT(GBufferMetallicTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.RG16);
            cmd.GetTemporaryRT(GBufferShadowTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
            cmd.GetTemporaryRT(GBufferDepthTexId, w, h, 16, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(GBufferColorLayout, GBufferDepth);
            cmd.ClearRenderTarget(true, true, Color.black, 1f);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// G-Bufferのレンダーテクスチャのクリーンアップ
        /// </summary>
        public void CleanupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            
            cmd.Clear();
            cmd.ReleaseTemporaryRT(GBufferDepthTexId);
            cmd.ReleaseTemporaryRT(GBufferShadowTexId);
            cmd.ReleaseTemporaryRT(GBufferMetallicTexId);
            cmd.ReleaseTemporaryRT(GBufferWorldPosTexId);
            cmd.ReleaseTemporaryRT(GBufferNormalTexId);
            cmd.ReleaseTemporaryRT(GBufferAlbedoTexId);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// G-Bufferの描画
        /// </summary>
        public void Draw()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var cullingResults = property.cullingResults;
            var camera = property.camera;
            
            cmd.Clear();
            cmd.SetRenderTarget(GBufferColorLayout, GBufferDepth);
            context.ExecuteCommandBuffer(cmd);

            // Filtering, Sort
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(GBufferTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            // Rendering
            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }
    }
}