using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_02 {
    /// <summary>
    /// モノクロの実装
    /// </summary>
    public class CustomMonochromePass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomMonochrome";
        private readonly int lerpId = Shader.PropertyToID("_Lerp");

        private readonly Material material;

        public CustomMonochromePass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomMonochromePass));
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            material = CoreUtils.CreateEngineMaterial(param.shader);
        }

        /// <summary>
        /// 描画前にセットアップ
        /// </summary>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            var renderer = renderingData.cameraData.renderer;

            // colorAttachmentとdepthAttachmentにカメラのテクスチャを設定
            ConfigureTarget(renderer.cameraColorTarget, renderer.cameraDepthTarget);
        }

        /// <summary>
        /// 描画
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            var cameraData = renderingData.cameraData;
            if (material == null || !cameraData.postProcessEnabled || cameraData.isSceneViewCamera) {
                return;
            }

            var volumeStack = VolumeManager.instance.stack;
            var component = volumeStack.GetComponent<CustomMonochrome>();
            if (component == null || !component.IsActive()) {
                return;
            }

            var cmd = CommandBufferPool.Get(renderTag);

            // 補間値をシェーダに送信
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            
            // カメラのテクスチャをモノクロ加工しながら同じテクスチャにコピーする
            cmd.Blit(colorAttachment, colorAttachment, material);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 描画後にクリーンアップ
        /// </summary>
        public override void OnCameraCleanup(CommandBuffer cmd) {
        }
    }
}