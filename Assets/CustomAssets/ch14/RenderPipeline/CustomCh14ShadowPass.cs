using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// シャドウ描画パス
    /// </summary>
    public class CustomCh14ShadowPass
    {
        private CustomCh14Property.Property property;
        private Matrix4x4 lightViewMatrix;
        private Matrix4x4 lightProjectionMatrix;

        /// <summary>
        /// シャドウプロパティのセットアップ
        /// </summary>
        public void SetupProperty(CustomCh14Property.Property property)
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
            var cullingResults = property.cullingResults;
            var w = property.shadowResolution.x;
            var h = property.shadowResolution.y;
            var mainLightIndex = property.mainLightIndex;
            
            if (mainLightIndex < 0)
            {
                return;
            }
            
            var light = cullingResults.visibleLights[mainLightIndex];
            lightProjectionMatrix = Matrix4x4.Ortho(-25, 25, -25, 25, 0f, 100f);
            lightProjectionMatrix = GL.GetGPUProjectionMatrix(lightProjectionMatrix, true);
            lightViewMatrix = Matrix4x4.Inverse(light.localToWorldMatrix);
            lightViewMatrix.m20 *= -1;
            lightViewMatrix.m21 *= -1;
            lightViewMatrix.m22 *= -1;
            lightViewMatrix.m23 *= -1;

            cmd.Clear();
            cmd.SetGlobalMatrix(CustomCh14Property.LvpMatId, lightProjectionMatrix * lightViewMatrix);
            cmd.SetGlobalFloat(CustomCh14Property.ShadowBiasId, light.light.shadowBias);
            cmd.SetGlobalFloat(CustomCh14Property.ShadowNormalBiasId, light.light.shadowNormalBias);
            cmd.GetTemporaryRT(CustomCh14Property.ShadowDepthTexId, w, h, 32, FilterMode.Point, RenderTextureFormat.ARGBFloat);
            cmd.SetRenderTarget(CustomCh14Property.ShadowDepthTex);
            cmd.ClearRenderTarget(true, true, Color.black, 1f);
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
            cmd.ReleaseTemporaryRT(CustomCh14Property.ShadowDepthTexId);
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

            cmd.Clear();
            cmd.SetRenderTarget(CustomCh14Property.ShadowDepthTex);
            context.ExecuteCommandBuffer(cmd);

            // 描画
            var sortingSettings = new SortingSettings
            {
                criteria = SortingCriteria.CommonOpaque,
                worldToCameraMatrix = lightViewMatrix,
                distanceMetric = DistanceMetric.Orthographic
            };
            var settings = new DrawingSettings(CustomCh14Property.ShadowTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast)
            );

            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
            
            // 反映
            cmd.Clear();
            cmd.SetGlobalTexture(CustomCh14Property.ShadowDepthTexId, CustomCh14Property.ShadowDepthTex);
            context.ExecuteCommandBuffer(cmd);
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