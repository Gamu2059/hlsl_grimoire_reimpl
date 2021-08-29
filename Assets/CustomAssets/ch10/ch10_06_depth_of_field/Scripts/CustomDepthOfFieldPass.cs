using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_06 {
    /// <summary>
    /// 被写界深度の実装
    /// </summary>
    public class CustomDepthOfFieldPass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomDoF";
        private readonly int sample1Id = Shader.PropertyToID("CustomDoFSample1");
        private readonly int sample2Id = Shader.PropertyToID("CustomDoFSample2");
        private readonly int focalDistanceId = Shader.PropertyToID("_FocalDistance");
        private readonly int focalWidthId = Shader.PropertyToID("_FocalWidth");
        private readonly int blurPowerId = Shader.PropertyToID("_BlurPower");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int samplingWeightsId = Shader.PropertyToID("_SamplingWeights");
        private readonly int inverseTextureSizeId = Shader.PropertyToID("_InverseTextureSize");

        private readonly Material material;

        public CustomDepthOfFieldPass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomDepthOfField));
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
            var component = volumeStack.GetComponent<CustomDepthOfField>();
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
            var distribution = component.distribution.value;
            var normalizedGaussianArray = Calc2PassNormalizedGaussianArray(samplingCount, distribution);

            // パラメータをシェーダに送信
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalFloat(focalDistanceId, component.focalDistance.value);
            cmd.SetGlobalFloat(focalWidthId, component.focalWidth.value);
            cmd.SetGlobalFloat(blurPowerId, component.blurPower.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloatArray(samplingWeightsId, normalizedGaussianArray);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));

            // 2パスでブラーを掛ける
            cmd.Blit(renderer.cameraColorTarget, sample1Id, material, 0);
            cmd.Blit(sample1Id, sample2Id, material, 1);

            // 深度値を使ってブラーを掛けた画像を合成する
            cmd.Blit(sample2Id, renderer.cameraColorTarget, material, 2);

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

        /// <summary>
        /// 等間隔でサンプリングした正規化したガウス関数の配列を計算する(2パス用)
        /// </summary>
        /// <param name="samplingCount">サンプリング回数</param>
        /// <param name="dist">分布</param>
        private float[] Calc2PassNormalizedGaussianArray(int samplingCount, float dist) {
            var array = CalcGaussianArray(samplingCount, dist);
            var weight = array[0];
            for (var i = 1; i < array.Length; i++) {
                weight += array[i] * 2;
            }

            for (var i = 0; i < array.Length; i++) {
                array[i] /= weight;
            }

            return array;
        }

        /// <summary>
        /// 等間隔でサンプリングしたガウス関数の配列を計算する
        /// </summary>
        /// <param name="samplingCount">サンプリング回数</param>
        /// <param name="dist">分布</param>
        private float[] CalcGaussianArray(int samplingCount, float dist) {
            var array = new float[samplingCount + 1];
            // 分布の中心から10離れた場所までをサンプリングする
            var gaussWeightSamplingInterval = 10f / samplingCount;
            for (var i = 0; i < array.Length; i++) {
                array[i] = CalcGaussian(gaussWeightSamplingInterval * i, dist);
            }

            return array;
        }

        /// <summary>
        /// 正規化されていないガウス関数を計算する
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="dist">分布</param>
        private float CalcGaussian(float x, float dist) {
            return Mathf.Exp(-x * x / (2 * dist * dist));
        }
    }
}