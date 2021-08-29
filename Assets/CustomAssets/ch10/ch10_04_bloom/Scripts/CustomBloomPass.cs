using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_04 {
    /// <summary>
    /// ブルームの実装
    /// </summary>
    public class CustomBloomPass : ScriptableRenderPass {
        [Serializable]
        public class CreateParam {
            public Shader shader;
        }

        private readonly string renderTag = "CustomBloom";
        private readonly int luminanceId = Shader.PropertyToID("CustomBloomLuminance");
        private readonly int sample1Id = Shader.PropertyToID("CustomBloomSample1");
        private readonly int sample2Id = Shader.PropertyToID("CustomBloomSample2");
        private readonly int lerpId = Shader.PropertyToID("_Lerp");
        private readonly int tintId = Shader.PropertyToID("_Tint");
        private readonly int samplingCountId = Shader.PropertyToID("_SamplingCount");
        private readonly int samplingSpaceId = Shader.PropertyToID("_SamplingSpace");
        private readonly int samplingWeightsId = Shader.PropertyToID("_SamplingWeights");
        private readonly int inverseTextureSizeId = Shader.PropertyToID("_InverseTextureSize");

        private readonly Material material;

        public CustomBloomPass(CreateParam param) {
            profilingSampler = new ProfilingSampler(nameof(CustomBloom));
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
            var component = volumeStack.GetComponent<CustomBloom>();
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
            CustomBloom component) {
            var cmd = CommandBufferPool.Get(renderTag);

            var renderer = renderingData.cameraData.renderer;
            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            // テクスチャを確保
            cmd.GetTemporaryRT(luminanceId, cameraDescriptor.width, cameraDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
            
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraDescriptor.width * downSamplingRate);
            var height = (int) (cameraDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);
            
            // 輝度テクスチャの初期化
            cmd.SetRenderTarget(luminanceId);
            cmd.ClearRenderTarget(false, true, Color.black);
            context.ExecuteCommandBuffer(cmd);

            // パラメータを計算
            var samplingCount = component.samplingCount.value;
            var distribution = component.distribution.value;
            var normalizedGaussianArray = Calc1PassNormalizedGaussianArray(samplingCount, distribution);
            
            // パラメータをシェーダに送信
            cmd.Clear();
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloatArray(samplingWeightsId, normalizedGaussianArray);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));
            cmd.SetGlobalColor(tintId, component.tint.value);
            context.ExecuteCommandBuffer(cmd);
            
            cmd.Clear();
            // 輝度抽出
            cmd.Blit(renderer.cameraColorTarget, luminanceId, material, 0);
            // 1パスで輝度テクスチャにブラーを掛ける
            cmd.Blit(luminanceId, sample1Id, material, 1);
            // ブラーを掛けた結果をカメラのターゲットと加算合成する
            cmd.Blit(sample1Id, renderer.cameraColorTarget, material, 4);
            
            // テクスチャを解放
            cmd.ReleaseTemporaryRT(luminanceId);
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
            CustomBloom component) {
            var cmd = CommandBufferPool.Get(renderTag);

            var renderer = renderingData.cameraData.renderer;
            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            // テクスチャを確保
            cmd.GetTemporaryRT(luminanceId, cameraDescriptor.width, cameraDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
            
            var downSamplingRate = component.downSamplingRate.value;
            var width = (int) (cameraDescriptor.width * downSamplingRate);
            var height = (int) (cameraDescriptor.height * downSamplingRate);
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sample1Id, descriptor);
            cmd.GetTemporaryRT(sample2Id, descriptor);
            
            // 輝度テクスチャの初期化
            cmd.SetRenderTarget(luminanceId);
            cmd.ClearRenderTarget(false, true, Color.black);
            context.ExecuteCommandBuffer(cmd);

            // パラメータを計算
            var samplingCount = component.samplingCount.value;
            var distribution = component.distribution.value;
            var normalizedGaussianArray = Calc2PassNormalizedGaussianArray(samplingCount, distribution);

            // パラメータをシェーダに送信
            cmd.Clear();
            cmd.SetGlobalFloat(lerpId, component.lerp.value);
            cmd.SetGlobalInt(samplingCountId, samplingCount);
            cmd.SetGlobalFloat(samplingSpaceId, component.samplingSpace.value);
            cmd.SetGlobalFloatArray(samplingWeightsId, normalizedGaussianArray);
            cmd.SetGlobalVector(inverseTextureSizeId, new Vector4(1f / width, 1f / height, 0, 0));
            cmd.SetGlobalColor(tintId, component.tint.value);
            context.ExecuteCommandBuffer(cmd);
            
            cmd.Clear();
            // 輝度抽出
            cmd.Blit(renderer.cameraColorTarget, luminanceId, material, 0);
            // 2パスで輝度テクスチャにブラーを掛ける
            cmd.Blit(luminanceId, sample1Id, material, 2);
            cmd.Blit(sample1Id, sample2Id, material, 3);
            // ブラーを掛けた結果をカメラのターゲットと加算合成する
            cmd.Blit(sample2Id, renderer.cameraColorTarget, material, 4);
            
            // テクスチャを解放
            cmd.ReleaseTemporaryRT(luminanceId);
            cmd.ReleaseTemporaryRT(sample1Id);
            cmd.ReleaseTemporaryRT(sample2Id);
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