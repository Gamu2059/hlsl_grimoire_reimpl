using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14 {
    /// <summary>
    /// ディファードライティングパス
    /// </summary>
    public class CustomCh14DeferredLightingPass {
        private CustomCh14Property.Property property;
        private Material deferredMaterial;

        public CustomCh14DeferredLightingPass() {
            deferredMaterial = CoreUtils.CreateEngineMaterial("Hidden/hlsl_grimoire/ch14/deferred");
        }

        /// <summary>
        /// ディファードライティングプロパティのセットアップ
        /// </summary>
        public void SetupProperty(CustomCh14Property.Property property) {
            this.property = property;
        }

        /// <summary>
        /// ディファードライティングのレンダーテクスチャのセットアップ
        /// </summary>
        public void SetupRT() {
            var cmd = property.commandBuffer;
            var context = property.context;
            var w = property.cameraResolution.x;
            var h = property.cameraResolution.y;

            cmd.Clear();
            cmd.GetTemporaryRT(CustomCh14Property.OffCameraColorTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// ディファードライティングのレンダーテクスチャのクリーンアップ
        /// </summary>
        public void CleanupRT() {
            var cmd = property.commandBuffer;
            var context = property.context;

            cmd.Clear();
            cmd.ReleaseTemporaryRT(CustomCh14Property.OffCameraColorTexId);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// ディファードライティングの描画
        /// </summary>
        public void Draw() {
            var cmd = property.commandBuffer;
            var context = property.context;

            cmd.Clear();
            cmd.SetRenderTarget(CustomCh14Property.OffCameraColorTex);
            cmd.Blit(CustomCh14Property.GBufferAlbedo, CustomCh14Property.OffCameraColorTex, deferredMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
        }
    }
}