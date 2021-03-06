using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// G-Buffer描画パス
    /// </summary>
    public class CustomCh14GBufferPass
    {
        private CustomCh14Property.Property property;

        /// <summary>
        /// G-Bufferプロパティのセットアップ
        /// </summary>
        public void SetupProperty(CustomCh14Property.Property property)
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
            var w = property.cameraResolution.x;
            var h = property.cameraResolution.y;
            
            cmd.Clear();
            cmd.GetTemporaryRT(CustomCh14Property.GBufferAlbedoTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(CustomCh14Property.GBufferNormalTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(CustomCh14Property.GBufferWorldPosTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);
            cmd.GetTemporaryRT(CustomCh14Property.GBufferMetallicTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(CustomCh14Property.GBufferShadowTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(CustomCh14Property.GBufferDepthTexId, w, h, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(CustomCh14Property.GBufferColorLayout, CustomCh14Property.GBufferDepthTex);
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
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferDepthTexId);
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferShadowTexId);
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferMetallicTexId);
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferWorldPosTexId);
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferNormalTexId);
            cmd.ReleaseTemporaryRT(CustomCh14Property.GBufferAlbedoTexId);
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
            cmd.SetRenderTarget(CustomCh14Property.GBufferColorLayout, CustomCh14Property.GBufferDepthTex);
            context.ExecuteCommandBuffer(cmd);

            // 描画
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(CustomCh14Property.GBufferTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
            
            // 反映
            cmd.Clear();
            cmd.SetGlobalTexture(CustomCh14Property.GBufferAlbedoTexId, CustomCh14Property.GBufferAlbedo);
            cmd.SetGlobalTexture(CustomCh14Property.GBufferNormalTexId, CustomCh14Property.GBufferNormal);
            cmd.SetGlobalTexture(CustomCh14Property.GBufferWorldPosTexId, CustomCh14Property.GBufferWorldPos);
            cmd.SetGlobalTexture(CustomCh14Property.GBufferMetallicTexId, CustomCh14Property.GBufferMetallic);
            cmd.SetGlobalTexture(CustomCh14Property.GBufferShadowTexId, CustomCh14Property.GBufferShadow);
            cmd.SetGlobalTexture(CustomCh14Property.GBufferDepthTexId, CustomCh14Property.GBufferDepthTex);
            context.ExecuteCommandBuffer(cmd);
        }
    }
}