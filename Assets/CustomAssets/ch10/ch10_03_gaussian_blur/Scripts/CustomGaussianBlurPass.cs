using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// ガウシアンブラーの実装
    /// </summary>
    public class CustomGaussianBlurPass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomGaussianBlur";
        private readonly int sample1Id = Shader.PropertyToID("CustomGaussianSample1");
        private readonly int sample2Id = Shader.PropertyToID("CustomGaussianSample2");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int samplingWeightsId = Shader.PropertyToID("_SamplingWeights");
        private readonly int inverseTextureSizeId = Shader.PropertyToID("_InverseTextureSize");

        private readonly Material material;

        public CustomGaussianBlurPass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomAvgBlurPass));
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
            var component = volumeStack.GetComponent<CustomGaussianBlur>();
            if (component == null || !component.IsActive()) {
                return;
            }

            if (component.use2pass.value) {
                Exec2PassBlur(ref context, ref renderingData, component);
            } else {
                Exec1PassBlur(ref context, ref renderingData, component);
            }
        }

        /// <summary>
        /// 描画後にクリーンアップ
        /// </summary>
        public override void OnCameraCleanup(CommandBuffer cmd) {
        }

        /// <summary>
        /// 1パスでガウシアンブラーを掛ける
        /// </summary>
        private void Exec1PassBlur(
            ref ScriptableRenderContext context,
            ref RenderingData renderingData,
            CustomGaussianBlur component) {
            var cmd = CommandBufferPool.Get(renderTag);

            // テクスチャを確保
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraTargetDescriptor.width * downSamplingRate);
            var height = (int) (cameraTargetDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);

            // パラメータを計算
            var samplingCount = component.samplingCount.value;
            var distribution = component.distribution.value;
            var normalizedGaussianArray = Calc1PassNormalizedGaussianArray(samplingCount, distribution);

            // パラメータをシェーダに送信
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloatArray(samplingWeightsId, normalizedGaussianArray);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));
            context.ExecuteCommandBuffer(cmd);

            // 1パスでブラーを掛けた後にカメラのテクスチャに出力する
            var renderer = renderingData.cameraData.renderer;
            cmd.Clear();
            cmd.Blit(renderer.cameraColorTarget, sample1Id, material, 0);
            cmd.Blit(sample1Id, renderer.cameraColorTarget);

            // テクスチャを解放
            cmd.ReleaseTemporaryRT(sample1Id);
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 2パスでガウシアンブラーを掛ける
        /// </summary>
        private void Exec2PassBlur(
            ref ScriptableRenderContext context,
            ref RenderingData renderingData,
            CustomGaussianBlur component) {
            var cmd = CommandBufferPool.Get(renderTag);

            // テクスチャを確保
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraTargetDescriptor.width * downSamplingRate);
            var height = (int) (cameraTargetDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);
            cmd.GetTemporaryRT(sample2Id, descriptor);

            // パラメータを計算
            var samplingCount = component.samplingCount.value;
            var distribution = component.distribution.value;
            var normalizedGaussianArray = Calc2PassNormalizedGaussianArray(samplingCount, distribution);

            // パラメータをシェーダに送信
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloatArray(samplingWeightsId, normalizedGaussianArray);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));
            context.ExecuteCommandBuffer(cmd);

            // 2パスでブラーを掛けた後にカメラのテクスチャに出力する
            var renderer = renderingData.cameraData.renderer;
            cmd.Clear();
            cmd.Blit(renderer.cameraColorTarget, sample1Id, material, 1);
            cmd.Blit(sample1Id, sample2Id, material, 2);
            cmd.Blit(sample2Id, renderer.cameraColorTarget, material, 3);

            // テクスチャを解放
            cmd.ReleaseTemporaryRT(sample1Id);
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 等間隔でサンプリングした正規化したガウス関数の配列を計算する(1パス用)
        /// </summary>
        /// <param name="samplingCount">サンプリング回数</param>
        /// <param name="dist">分布</param>
        private float[] Calc1PassNormalizedGaussianArray(int samplingCount, float dist) {
            var array = CalcGaussianArray(samplingCount, dist);
            var weight = array[0];
            for (var i = 1; i < array.Length; i++) {
                weight += array[i] * 4;
            }

            for (var i = 0; i < array.Length; i++) {
                array[i] /= weight;
            }

            return array;
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