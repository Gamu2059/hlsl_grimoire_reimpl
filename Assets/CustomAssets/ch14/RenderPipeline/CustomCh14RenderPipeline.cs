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
        private static readonly int CameraColor = Shader.PropertyToID("CameraColor");
        private static readonly int CameraDepth = Shader.PropertyToID("CameraDepth");

        private CustomCh14RenderPipelineAsset asset;
        private CustomCh14DepthPrePass depthPrePass;
        private CustomCh14ShadowPass shadowPass;
        private CustomCh14GBufferPass gBufferPass;

        public CustomCh14RenderPipeline(CustomCh14RenderPipelineAsset asset)
        {
            this.asset = asset;
            depthPrePass = new CustomCh14DepthPrePass();
            shadowPass = new CustomCh14ShadowPass();
            gBufferPass = new CustomCh14GBufferPass();

            GraphicsSettings.useScriptableRenderPipelineBatching = true;
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
                
                // シャドウの処理
                if (mainLightIndex >= 0)
                {
                    var prop = new CustomCh14ShadowPass.Property
                    {
                        context = context,
                        commandBuffer = cmd,
                        cullingResults = cullingResults,
                        resolution = asset.ShadowResolution,
                        mainLightIndex = mainLightIndex,
                    };
                    shadowPass.SetupProperty(prop);
                    shadowPass.SetupRT();
                    shadowPass.Draw();
                }

                // デプスの処理
                {
                    var prop = new CustomCh14DepthPrePass.Property
                    {
                        context = context,
                        commandBuffer = cmd,
                        cullingResults = cullingResults,
                        camera = camera,
                    };
                    depthPrePass.SetupProperty(prop);
                    depthPrePass.SetupRT();
                    depthPrePass.Draw();
                }
                
                // GBufferの処理
                {
                    var prop = new CustomCh14GBufferPass.Property
                    {
                        context = context,
                        commandBuffer = cmd,
                        cullingResults = cullingResults,
                        camera = camera,
                    };
                    gBufferPass.SetupProperty(prop);
                    gBufferPass.SetupRT();
                    gBufferPass.Draw();
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

                gBufferPass.CleanupRT();
                depthPrePass.CleanupRT();
                if (mainLightIndex >= 0)
                {
                    shadowPass.CleanupRT();
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

        private void DrawOpaque(ScriptableRenderContext context, Camera camera, CommandBuffer cmd,
            CullingResults cullingResults)
        {
            cmd.Clear();
            cmd.SetRenderTarget(color: CameraColor, depth: CameraDepth);
            cmd.SetGlobalMatrix(CustomCh14ShadowPass.LvpMatId, shadowPass.LvpMatrix);
            cmd.SetGlobalTexture(CustomCh14ShadowPass.DepthTexId, CustomCh14ShadowPass.DepthTexId);
            cmd.SetGlobalTexture(CustomCh14DepthPrePass.DepthTexId, CustomCh14DepthPrePass.DepthTexId);
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
    }
}