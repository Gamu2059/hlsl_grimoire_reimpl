using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_07 {
    /// <summary>
    /// 六角形被写界深度の実装
    /// </summary>
    public class CustomHexBokehBlurPass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomHexBokehBlur";
        private readonly int sample1Id = Shader.PropertyToID("CustomHexBokehBlur1");
        private readonly int sample2Id = Shader.PropertyToID("CustomHexBokehBlur");
        private readonly int blurTex1Id = Shader.PropertyToID("_BlurTex1");
        private readonly int blurTex2Id = Shader.PropertyToID("_BlurTex2");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int inverseTextureSizeId = Shader.PropertyToID("_InverseTextureSize");

        private readonly Material material;

        public CustomHexBokehBlurPass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomHexBokehBlur));
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            material = CoreUtils.CreateEngineMaterial(param.shader);
        }

        /// <summary>
        /// 描画前にセットアップ
        /// </summary>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
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
            var component = volumeStack.GetComponent<CustomHexBokehBlur>();
            if (component == null || !component.IsActive()) {
                return;
            }

            var cmd = CommandBufferPool.Get(renderTag);

            var renderer = renderingData.cameraData.renderer;
            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            // テクスチャを確保
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraDescriptor.width * downSamplingRate);
            var height = (int) (cameraDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);
            cmd.GetTemporaryRT(sample2Id, descriptor);

            // パラメータを計算
            var samplingCount = component.samplingCount.value;

            // パラメータをシェーダに送信
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));

            // MRTで1パスで2枚のテクスチャにブラーを掛ける
            cmd.SetRenderTarget(new RenderTargetIdentifier[] {sample1Id, sample2Id}, sample1Id);
            cmd.Blit(renderer.cameraColorTarget, BuiltinRenderTextureType.CurrentActive, material, 0);
            
            // 2枚のテクスチャを使って六角形ブラーを掛ける
            cmd.SetGlobalTexture(blurTex1Id, sample1Id);
            cmd.SetGlobalTexture(blurTex2Id, sample2Id);
            cmd.Blit(-1, renderer.cameraColorTarget, material, 1);

            // 深度値を使ってブラーを掛けた画像を合成する
            // cmd.Blit(sample2Id, renderer.cameraColorTarget);

            cmd.ReleaseTemporaryRT(sample1Id);
            cmd.ReleaseTemporaryRT(sample2Id);
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