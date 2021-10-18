using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14 {
    /// <summary>
    /// フォワードライティングパス
    /// </summary>
    public class CustomCh14ForwardLightingPass {
        private CustomCh14Property.Property property;

        /// <summary>
        /// プロパティのセットアップ
        /// </summary>
        public void SetupProperty(CustomCh14Property.Property property) {
            this.property = property;
        }

        /// <summary>
        /// レンダーテクスチャのセットアップ
        /// </summary>
        public void SetupRT() {
            var cmd = property.commandBuffer;
            var context = property.context;
            var w = property.cameraResolution.x;
            var h = property.cameraResolution.y;

            cmd.Clear();
            cmd.GetTemporaryRT(CustomCh14Property.OpaqueColorTexId, w, h, 0, FilterMode.Bilinear,
                RenderTextureFormat.ARGB32);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// レンダーテクスチャのクリーンアップ
        /// </summary>
        public void CleanupRT() {
            var cmd = property.commandBuffer;
            var context = property.context;

            cmd.Clear();
            cmd.ReleaseTemporaryRT(CustomCh14Property.OpaqueColorTexId);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// 不透明オブジェクトの描画
        /// </summary>
        public void DrawOpaque() {
            var cmd = property.commandBuffer;
            var context = property.context;
            var cullingResults = property.cullingResults;
            var camera = property.camera;

            cmd.Clear();
            cmd.SetRenderTarget(CustomCh14Property.OffCameraColorTex, CustomCh14Property.GBufferDepthTex);
            context.ExecuteCommandBuffer(cmd);

            // 描画
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(CustomCh14Property.ForwardLightingTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }

        /// <summary>
        /// 半透明オブジェクトの描画
        /// </summary>
        public void DrawTransparent() {
            var cmd = property.commandBuffer;
            var context = property.context;
            var cullingResults = property.cullingResults;
            var camera = property.camera;

            cmd.Clear();
            cmd.SetRenderTarget(CustomCh14Property.OffCameraColorTex, CustomCh14Property.GBufferDepthTex);
            context.ExecuteCommandBuffer(cmd);

            // 描画
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonTransparent};
            var settings = new DrawingSettings(CustomCh14Property.ForwardLightingTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange((int) RenderQueue.GeometryLast, (int) RenderQueue.Transparent),
                camera.cullingMask
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }

        /// <summary>
        /// 不透明物体の一時保存テクスチャにコピー
        /// </summary>
        public void BlitCameraColorToOpaqueColor() {
            var cmd = property.commandBuffer;
            var context = property.context;

            cmd.Clear();
            cmd.Blit(CustomCh14Property.OffCameraColorTex, CustomCh14Property.OpaqueColorTex);
            cmd.SetGlobalTexture(CustomCh14Property.OpaqueColorTexId, CustomCh14Property.OpaqueColorTex);
            context.ExecuteCommandBuffer(cmd);
        }
    }
}