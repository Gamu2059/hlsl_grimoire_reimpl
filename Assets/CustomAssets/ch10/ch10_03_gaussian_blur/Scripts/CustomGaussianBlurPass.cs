using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03
{
    /// <summary>
    /// ガウシアンブラーの実装
    /// </summary>
    public class CustomGaussianBlurPass : ScriptableRenderPass
    {
        [Serializable]
        public class CreateParam
        {
            public Shader shader;
        }

        private readonly string renderTag = "CustomGaussianBlur";
        private readonly int sample1Id = Shader.PropertyToID("CustomGaussianSample1");
        private readonly int sample2Id = Shader.PropertyToID("CustomGaussianSample2");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int avgDivFactorId = Shader.PropertyToID("_AvgDivFactor");

        private readonly Material material;

        public CustomGaussianBlurPass(CreateParam param)
        {
            profilingSampler = new ProfilingSampler(nameof(CustomAvgBlurPass));
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            material = CoreUtils.CreateEngineMaterial(param.shader);
        }

        /// <summary>
        /// 描画前にセットアップ
        /// </summary>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;

            // colorAttachmentとdepthAttachmentにカメラのテクスチャを設定
            ConfigureTarget(renderer.cameraColorTarget, renderer.cameraDepthTarget);
        }

        /// <summary>
        /// 描画
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (material == null || !cameraData.postProcessEnabled || cameraData.isSceneViewCamera)
            {
                return;
            }

            var volumeStack = VolumeManager.instance.stack;
            var component = volumeStack.GetComponent<CustomGaussianBlur>();
            if (component == null || !component.IsActive())
            {
                return;
            }

            if (component.use2pass.value)
            {
                Exec2PassBlur(ref context, ref renderingData, component);
            }
            else
            {
                Exec1PassBlur(ref context, ref renderingData, component);
            }
        }

        /// <summary>
        /// 描画後にクリーンアップ
        /// </summary>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        /// <summary>
        /// 1パスでガウシアンブラーを掛ける
        /// </summary>
        private void Exec1PassBlur(
            ref ScriptableRenderContext context,
            ref RenderingData renderingData,
            CustomGaussianBlur component)
        {
            var cmd = CommandBufferPool.Get(renderTag);

            // テクスチャを確保
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraTargetDescriptor.width * downSamplingRate);
            var height = (int) (cameraTargetDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);

            // パラメータを計算
            var sampling = component.samplingCount.value;
            var samplingFactor = (2 * sampling + 1) * (2 * sampling + 1);
            
            
            // パラメータをシェーダに送信
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
        /// 2パスでガウシアンブラーを掛ける
        /// </summary>
        private void Exec2PassBlur(
            ref ScriptableRenderContext context,
            ref RenderingData renderingData,
            CustomGaussianBlur component)
        {
        }
    }
}