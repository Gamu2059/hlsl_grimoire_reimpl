using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// シャドウ描画パス
    /// </summary>
    public class CustomCh14ShadowPass
    {
        /// <summary>
        /// シャドウプロパティ
        /// </summary>
        public struct Property
        {
            public ScriptableRenderContext context;
            public CommandBuffer commandBuffer;
            public CullingResults cullingResults;
            public Vector2Int resolution;
            public int mainLightIndex;
        }
        
        public static readonly ShaderTagId ShadowTag = new ShaderTagId("CustomCh14Shadow");
        public static readonly int ColorTexId = Shader.PropertyToID("ShadowColor");
        public static readonly int DepthTexId = Shader.PropertyToID("ShadowDepth");
        public static readonly int LvpMatId = Shader.PropertyToID("_LvpMat");
        public static readonly int BiasId = Shader.PropertyToID("_ShadowBias");
        public static readonly int NormalBiasId = Shader.PropertyToID("_ShadowNormalBias");

        private Property property;
        public Matrix4x4 LvpMatrix { get; private set; }

        /// <summary>
        /// シャドウプロパティのセットアップ
        /// </summary>
        public void SetupProperty(Property property)
        {
            this.property = property;
        }

        /// <summary>
        /// シャドウのレンダーテクスチャのセットアップ
        /// </summary>
        public void SetupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var w = property.resolution.x;
            var h = property.resolution.y;
            
            cmd.Clear();
            cmd.GetTemporaryRT(ColorTexId, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(DepthTexId, w, h, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(color: ColorTexId, depth: DepthTexId);
            cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// シャドウのレンダーテクスチャのクリーンアップ
        /// </summary>
        public void CleanupRT()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            
            cmd.Clear();
            cmd.ReleaseTemporaryRT(DepthTexId);
            cmd.ReleaseTemporaryRT(ColorTexId);
            context.ExecuteCommandBuffer(cmd);
        }


        /// <summary>
        /// シャドウの描画
        /// </summary>
        public void Draw()
        {
            var cmd = property.commandBuffer;
            var context = property.context;
            var cullingResults = property.cullingResults;
            var mainLightIndex = property.mainLightIndex;
            
            if (mainLightIndex < 0)
            {
                return;
            }

            var light = cullingResults.visibleLights[mainLightIndex];
            if (!cullingResults.GetShadowCasterBounds(mainLightIndex, out var bound))
            {
                return;
            }

            var projectionMatrix = Matrix4x4.Ortho(-10, 10, -10, 10, 0f, 50f);
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
            var viewMatrix = Matrix4x4.Inverse(light.localToWorldMatrix);
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;

            LvpMatrix = projectionMatrix * viewMatrix;

            cmd.Clear();
            cmd.SetRenderTarget(color: ColorTexId, depth: DepthTexId);
            cmd.SetGlobalMatrix(LvpMatId, LvpMatrix);
            cmd.SetGlobalFloat(BiasId, light.light.shadowBias);
            cmd.SetGlobalFloat(NormalBiasId, light.light.shadowNormalBias);
            context.ExecuteCommandBuffer(cmd);

            var sortingSettings = new SortingSettings
            {
                criteria = SortingCriteria.CommonOpaque,
                worldToCameraMatrix = viewMatrix,
                distanceMetric = DistanceMetric.Orthographic
            };
            var settings = new DrawingSettings(ShadowTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast)
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }
        
        private Matrix4x4 CreateLvpMatrix(VisibleLight visibleLight, Vector2 size, float near, float far)
        {
            var width = size.x * 0.5f;
            var height = size.y * 0.5f;
            var projectionMatrix = Matrix4x4.Ortho(-width, width, height, -height, near, far);
            var viewMatrix = Matrix4x4.Inverse(visibleLight.localToWorldMatrix);
            return GetShadowTransform(projectionMatrix, viewMatrix);
        }

        private Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * worldToShadow;
        }
    }
}