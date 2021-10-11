using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// Chapter14で使うレンダーパイプライン
    /// </summary>
    public class CustomCh14RenderPipeline : RenderPipeline
    {
        private static readonly ShaderTagId tag = new ShaderTagId("CustomCh14");
        private static readonly ShaderTagId shadowTag = new ShaderTagId("CustomCh14Shadow");
        private static readonly int CameraColor = Shader.PropertyToID("CameraColor");
        private static readonly int CameraDepth = Shader.PropertyToID("CameraDepth");
        private static readonly int ShadowColor = Shader.PropertyToID("ShadowColor");
        private static readonly int ShadowDepth = Shader.PropertyToID("ShadowDepth");

        private static readonly int LightViewId = Shader.PropertyToID("_LightView");
        private static readonly int LightProjId = Shader.PropertyToID("_LightProj");
        private static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBias");
        private static readonly int ShadowNormalBiasId = Shader.PropertyToID("_ShadowNormalBias");

        private Matrix4x4 projectionMatrix, viewMatrix;

        private CustomCh14RenderPipelineAsset asset;

        public CustomCh14RenderPipeline(CustomCh14RenderPipelineAsset asset)
        {
            this.asset = asset;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                var cmd = CommandBufferPool.Get("CustomCh14");
                var camera = cameras[i];

                // カメラプロパティ設定
                context.SetupCameraProperties(camera);

                // Culling
                if (!camera.TryGetCullingParameters(false, out var cullingParameters))
                {
                    continue;
                }

                var cullingResults = context.Cull(ref cullingParameters);


                // ライト情報のセットアップ
                var mainLightIndex = SetupLights(context, cmd, cullingResults);
                if (mainLightIndex >= 0)
                {
                    SetupShadowRenderTexture(context, cmd);
                    DrawShadow(context, cmd, cullingResults, mainLightIndex);
                }

                // RenderTexture作成
                SetupCameraRenderTexture(context, camera, cmd);

                // 不透明オブジェクト描画
                DrawOpaque(context, camera, cmd, cullingResults);

                // Skybox描画
                if (camera.clearFlags == CameraClearFlags.Skybox)
                {
                    context.DrawSkybox(camera);
                }

                // 半透明オブジェクト描画
                DrawTransparent(context, camera, cmd, cullingResults);

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                }
#endif

                // PostProcessing

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }
#endif

                // CameraTargetに描画
                RestoreCameraTarget(context, cmd);

                if (mainLightIndex >= 0)
                {
                    CleanupShadowRenderTexture(context, cmd);
                }

                // RenderTexture解放
                CleanupCameraRenderTexture(context, cmd);

                CommandBufferPool.Release(cmd);
            }

            context.Submit();
        }

        private void SetupCameraRenderTexture(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            var width = Display.main.renderingWidth;
            var height = Display.main.renderingHeight;
            var targetTexture = camera.targetTexture;
            if (targetTexture != null)
            {
                width = targetTexture.width;
                height = targetTexture.height;
            }

            cmd.Clear();
            cmd.GetTemporaryRT(CameraColor, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(CameraDepth, width, height, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(color: CameraColor, depth: CameraDepth);

            switch (camera.clearFlags)
            {
                case CameraClearFlags.Depth:
                case CameraClearFlags.Skybox:
                    cmd.ClearRenderTarget(true, false, Color.black, 1.0f);
                    break;
                case CameraClearFlags.SolidColor:
                    cmd.ClearRenderTarget(true, true, camera.backgroundColor, 1.0f);
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
        }

        private void CleanupCameraRenderTexture(ScriptableRenderContext context, CommandBuffer cmd)
        {
            cmd.Clear();
            cmd.ReleaseTemporaryRT(CameraDepth);
            cmd.ReleaseTemporaryRT(CameraColor);
            context.ExecuteCommandBuffer(cmd);
        }

        private void SetupShadowRenderTexture(ScriptableRenderContext context, CommandBuffer cmd)
        {
            var width = asset.ShadowResolution.x;
            var height = asset.ShadowResolution.y;
            cmd.Clear();
            cmd.GetTemporaryRT(ShadowColor, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(ShadowDepth, width, height, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(color: ShadowColor, depth: ShadowDepth);
            cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
            context.ExecuteCommandBuffer(cmd);
        }

        private void CleanupShadowRenderTexture(ScriptableRenderContext context, CommandBuffer cmd)
        {
            cmd.Clear();
            cmd.ReleaseTemporaryRT(ShadowDepth);
            cmd.ReleaseTemporaryRT(ShadowColor);
            context.ExecuteCommandBuffer(cmd);
        }

        private int SetupLights(ScriptableRenderContext context, CommandBuffer cmd,
            CullingResults cullingResults)
        {
            cmd.Clear();

            // DirectionalLightの探索
            int lightIndex = -1;
            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var tempVisibleLight = cullingResults.visibleLights[i];
                var tempLight = tempVisibleLight.light;

                if (tempLight == null || tempLight.shadows == LightShadows.None || tempLight.shadowStrength <= 0f ||
                    tempLight.type != LightType.Directional)
                {
                    continue;
                }

                lightIndex = i;
                break;
            }

            if (lightIndex < 0)
            {
                cmd.DisableShaderKeyword("ENABLE_DIRECTIONAL_LIGHT");
                context.ExecuteCommandBuffer(cmd);
                return -1;
            }

            // ライトのパラメータ設定
            var visibleLight = cullingResults.visibleLights[lightIndex];
            var light = visibleLight.light;

            cmd.EnableShaderKeyword("ENABLE_DIRECTIONAL_LIGHT");
            cmd.SetGlobalColor("_LightColor", light.color * light.intensity);
            cmd.SetGlobalVector("_LightVector", -light.transform.forward);
            context.ExecuteCommandBuffer(cmd);

            return lightIndex;
        }

        /// <summary>
        /// 影を描画する
        /// </summary>
        private void DrawShadow(ScriptableRenderContext context, CommandBuffer cmd,
            CullingResults cullingResults, int mainLightIndex)
        {
            if (mainLightIndex < 0)
            {
                return;
            }

            var light = cullingResults.visibleLights[mainLightIndex];
            if (!cullingResults.GetShadowCasterBounds(mainLightIndex, out var bound))
            {
                return;
            }

            projectionMatrix = Matrix4x4.Ortho(-10, 10, -10, 10, 0f, 50f);
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
            viewMatrix = Matrix4x4.Inverse(light.localToWorldMatrix);
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;

            cmd.Clear();
            cmd.SetRenderTarget(color: ShadowColor, depth: ShadowDepth);
            cmd.SetGlobalMatrix(LightViewId, viewMatrix);
            cmd.SetGlobalMatrix(LightProjId, projectionMatrix);
            cmd.SetGlobalFloat(ShadowBiasId, light.light.shadowBias);
            cmd.SetGlobalFloat(ShadowNormalBiasId, light.light.shadowNormalBias);
            context.ExecuteCommandBuffer(cmd);

            // Filtering, Sort
            var sortingSettings = new SortingSettings
            {
                criteria = SortingCriteria.CommonOpaque,
                worldToCameraMatrix = viewMatrix,
                distanceMetric = DistanceMetric.Orthographic
            };
            var settings = new DrawingSettings(shadowTag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast)
            );

            // Rendering
            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }

        private void DrawOpaque(ScriptableRenderContext context, Camera camera, CommandBuffer cmd,
            CullingResults cullingResults)
        {
            cmd.Clear();
            cmd.SetRenderTarget(color: CameraColor, depth: CameraDepth);
            cmd.SetGlobalMatrix(LightViewId, viewMatrix);
            cmd.SetGlobalMatrix(LightProjId, projectionMatrix);
            cmd.SetGlobalTexture(ShadowDepth, ShadowDepth);
            context.ExecuteCommandBuffer(cmd);

            // Filtering, Sort
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var settings = new DrawingSettings(tag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                camera.cullingMask
            );

            // Rendering
            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }

        private void DrawTransparent(ScriptableRenderContext context, Camera camera, CommandBuffer cmd,
            CullingResults cullingResults)
        {
            cmd.Clear();
            cmd.SetRenderTarget(color: CameraColor, depth: CameraDepth);
            context.ExecuteCommandBuffer(cmd);

            // Filtering, Sort
            var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonTransparent};
            var settings = new DrawingSettings(tag, sortingSettings);
            var filterSettings = new FilteringSettings(
                new RenderQueueRange((int) RenderQueue.GeometryLast, (int) RenderQueue.Transparent),
                camera.cullingMask
            );

            // 描画
            context.DrawRenderers(cullingResults, ref settings, ref filterSettings);
        }

        private void RestoreCameraTarget(ScriptableRenderContext context, CommandBuffer cmd)
        {
            var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            cmd.Clear();
            cmd.SetRenderTarget(cameraTarget);
            cmd.Blit(CameraColor, cameraTarget);
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