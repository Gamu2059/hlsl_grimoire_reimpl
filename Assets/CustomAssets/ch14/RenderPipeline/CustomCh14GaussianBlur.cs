using Gamu2059.hlsl_grimoire.ch10_03;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch14 {
    public class CustomCh14GaussianBlur {
        
        private readonly int Sample1Id = Shader.PropertyToID("_GaussianSample1");
        private readonly int Sample2Id = Shader.PropertyToID("_GaussianSample2");
        private readonly int SamplingCountId = Shader.PropertyToID("_GaussianSamplingCount");
        private readonly int SamplingSpaceId = Shader.PropertyToID("_GaussianSamplingSpace");
        private readonly int SamplingWeightsId = Shader.PropertyToID("_GaussianSamplingWeights");

        private Material material;
        
        public CustomCh14GaussianBlur() {
            material = CoreUtils.CreateEngineMaterial("Hidden/hlsl_grimoire/ch14/gaussian_blur");
        }

        public void Dispose() {
            GameObject.DestroyImmediate(material);
        }
        
        /// <summary>
        /// 2パスでガウシアンブラーを掛ける
        /// </summary>
        public void ExecBlur(
            ref ScriptableRenderContext context, 
            CommandBuffer cmd, 
            RenderTargetIdentifier source,
            RenderTargetIdentifier dest,
            Vector2Int resolution,
            int samplingCount,
            float samplingSpace,
            float distribution) {
            // テクスチャを確保
            var width = resolution.x;
            var height = resolution.y;
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(Sample1Id, descriptor);
            cmd.GetTemporaryRT(Sample2Id, descriptor);

            // パラメータを計算
            var normalizedGaussianArray = Calc2PassNormalizedGaussianArray(samplingCount, distribution);

            // パラメータをシェーダに送信
            cmd.SetGlobalInt(SamplingCountId, samplingCount);
            cmd.SetGlobalFloat(SamplingSpaceId, samplingSpace);
            cmd.SetGlobalFloatArray(SamplingWeightsId, normalizedGaussianArray);
            context.ExecuteCommandBuffer(cmd);

            // 2パスでブラーを掛けた後にカメラのテクスチャに出力する
            cmd.Clear();
            cmd.Blit(source, Sample1Id, material, 0);
            cmd.Blit(Sample1Id, Sample2Id, material, 1);
            cmd.Blit(Sample2Id, dest);

            // テクスチャを解放
            cmd.ReleaseTemporaryRT(Sample1Id);
            cmd.ReleaseTemporaryRT(Sample2Id);
            context.ExecuteCommandBuffer(cmd);
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