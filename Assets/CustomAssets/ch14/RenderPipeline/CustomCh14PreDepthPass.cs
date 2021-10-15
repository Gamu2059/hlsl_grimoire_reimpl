using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// 画面全体のデプステクスチャの描画パス
    /// </summary>
    public class CustomCh14PreDepthPass
    {
        private CustomCh14Property.Property property;
        
        /// <summary>
        /// デプスプロパティのセットアップ
        /// </summary>
        public void SetupProperty(CustomCh14Property.Property property)
        {
            this.property = property;
        }
        
        /// <summary>
        /// デプスのレンダーテクスチャのセットアップ
        /// </summary>
        public void SetupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var w = property.cameraResolution.x;
            var h = property.cameraResolution.y;
            
            cmd.Clear();
            cmd.GetTemporaryRT(CustomCh14Property.PreDepthTexId, w, h, 16, FilterMode.Bilinear, RenderTextureFormat.RFloat);
            cmd.SetRenderTarget(CustomCh14Property.PreDepthTex);
            cmd.ClearRenderTarget(false, true, Color.black);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// デプスのレンダーテクスチャのクリーンアップ
        /// </summary>
        public void CleanupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            
            cmd.Clear();
            cmd.ReleaseTemporaryRT(CustomCh14Property.PreDepthTexId);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// デプスの描画
        /// </summary>
        public void Draw()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var cullingResults = property.cullingResults;
            var camera = property.camera;
            
            cmd.Clear();
            cmd.SetRenderTarget(CustomCh14Property.PreDepthTex);
            context.ExecuteCommandBuffer(cmd);

            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(CustomCh14Property.PreDepthTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }
    }
}