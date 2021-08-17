using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// 平均ブラーの実装
    /// </summary>
    public class CustomAvgBlurPass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomAvgBlur";
        private readonly int copyId = Shader.PropertyToID("Copy");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int avgDivFactorId = Shader.PropertyToID("_AvgDivFactor");

        private readonly Material material;

        public CustomAvgBlurPass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomAvgBlurPass));
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
            var component = volumeStack.GetComponent<CustomAvgBlur>();
            if (component == null || !component.IsActive()) {
                return;
            }

            var cmd = CommandBufferPool.Get(renderTag);
            
            // RTを確保
            cmd.GetTemporaryRT(copyId, renderingData.cameraData.cameraTargetDescriptor);
            cmd.SetRenderTarget(copyId);
            cmd.ClearRenderTarget(false, true, Color.black);
            context.ExecuteCommandBuffer(cmd);

            // パラメータをシェーダに送信
            var sampling = component.samplingCount.value;
            var samplingFactor = (2 * sampling + 1) * (2 * sampling + 1);
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, component.samplingCount.value);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloat(avgDivFactorId, 1f / samplingFactor);

            // カメラのテクスチャをブラー加工しながら同じテクスチャにコピーする
            cmd.Blit(cameraData.renderer.cameraColorTarget, copyId, material);
            cmd.Blit(copyId, cameraData.renderer.cameraColorTarget);
            cmd.ReleaseTemporaryRT(copyId);
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