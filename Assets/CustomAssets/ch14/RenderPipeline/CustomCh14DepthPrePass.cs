using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// 画面全体のデプステクスチャの描画パス
    /// </summary>
    public class CustomCh14DepthPrePass
    {
        public struct Property
        {
            public ScriptableRenderContext context;
            public CommandBuffer commandBuffer;
            public CullingResults cullingResults;
            public Camera camera;
        }
        
        public static readonly ShaderTagId DepthTag = new ShaderTagId("CustomCh14Depth");
        public static readonly int DepthTexId = Shader.PropertyToID("Depth");

        private Property property;
        
        /// <summary>
        /// デプスプロパティのセットアップ
        /// </summary>
        public void SetupProperty(Property property)
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
            cmd.GetTemporaryRT(DepthTexId, w, h, 16, FilterMode.Bilinear, RenderTextureFormat.RFloat);
            cmd.SetRenderTarget(DepthTexId);
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
            cmd.ReleaseTemporaryRT(DepthTexId);
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
            cmd.SetRenderTarget(DepthTexId);
            context.ExecuteCommandBuffer(cmd);

            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(DepthTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }
    }
}