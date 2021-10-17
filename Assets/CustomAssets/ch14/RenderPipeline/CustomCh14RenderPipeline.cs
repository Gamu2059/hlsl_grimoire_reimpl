using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14 {
    /// <summary>
    /// Chapter14で使うレンダーパイプライン
    /// </summary>
    public class CustomCh14RenderPipeline : RenderPipeline {
        private CustomCh14RenderPipelineAsset asset;
        private CustomCh14PreDepthPass depthPreDepthPass;
        private CustomCh14ShadowPass shadowPass;
        private CustomCh14GBufferPass gBufferPass;
        private CustomCh14DeferredLightingPass deferredLightingPass;
        private CustomCh14ForwardLightingPass forwarLightingPass;

        public CustomCh14RenderPipeline(CustomCh14RenderPipelineAsset asset) {
            this.asset = asset;
            depthPreDepthPass = new CustomCh14PreDepthPass();
            shadowPass = new CustomCh14ShadowPass();
            gBufferPass = new CustomCh14GBufferPass();
            deferredLightingPass = new CustomCh14DeferredLightingPass();
            forwarLightingPass = new CustomCh14ForwardLightingPass();

            GraphicsSettings.useScriptableRenderPipelineBatching = true;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            
            // 影のパラメータセットアップ
            switch (asset.ShadowType) {
                case ShadowType.Hard:
                    Shader.DisableKeyword(CustomCh14Property.PcfKeyword);
                    Shader.DisableKeyword(CustomCh14Property.VsmKeyword);
                    break;
                case ShadowType.PcfSoft:
                    Shader.EnableKeyword(CustomCh14Property.PcfKeyword);
                    Shader.DisableKeyword(CustomCh14Property.VsmKeyword);
                    Shader.SetGlobalInt(CustomCh14Property.PcfSampleCountId, asset.PcfSampleCount);
                    Shader.SetGlobalFloat(CustomCh14Property.PcfSampleSpaceId, asset.PcfSampleSpace);
                    break;
                case ShadowType.VsmSoft:
                    Shader.EnableKeyword(CustomCh14Property.VsmKeyword);
                    Shader.DisableKeyword(CustomCh14Property.PcfKeyword);
                    break;
            }
            
            for (int i = 0; i < cameras.Length; i++) {
                var cmd = CommandBufferPool.Get("CustomCh14");
                var camera = cameras[i];

                // カメラプロパティ設定
                context.SetupCameraProperties(camera);

                // Culling
                if (!camera.TryGetCullingParameters(false, out var cullingParameters)) {
                    continue;
                }

                var cullingResults = context.Cull(ref cullingParameters);

                // ライト情報のセットアップ
                var mainLightIndex = SetupLights(context, cmd, cullingResults);

                // プロパティデータの作成
                var prop = new CustomCh14Property.Property {
                    asset = asset,
                    context = context,
                    commandBuffer = cmd,
                    camera = camera,
                    cullingResults = cullingResults,
                    shadowResolution = asset.ShadowResolution,
                    cameraResolution = GetCameraResolution(camera),
                    mainLightIndex = mainLightIndex,
                };

                // シャドウの処理
                if (mainLightIndex >= 0) {
                    shadowPass.SetupProperty(prop);
                    shadowPass.SetupRT();
                    shadowPass.Draw();
                }

                // デプスの処理
                depthPreDepthPass.SetupProperty(prop);
                depthPreDepthPass.SetupRT();
                depthPreDepthPass.Draw();

                // GBufferの処理
                gBufferPass.SetupProperty(prop);
                gBufferPass.SetupRT();
                gBufferPass.Draw();

                // ディファードライティングの処理
                deferredLightingPass.SetupProperty(prop);
                deferredLightingPass.SetupRT();
                deferredLightingPass.Draw();

                // フォワードライティングの処理
                forwarLightingPass.SetupProperty(prop);
                forwarLightingPass.SetupRT();
                forwarLightingPass.DrawOpaque();

                // Skybox描画
                if (camera.clearFlags == CameraClearFlags.Skybox) {
                    cmd.Clear();
                    cmd.SetRenderTarget(CustomCh14Property.OffCameraColorTex, CustomCh14Property.GBufferDepthTex);
                    context.ExecuteCommandBuffer(cmd);
                    context.DrawSkybox(camera);
                }

                // フォワードライティングの処理
                forwarLightingPass.BlitCameraColorToOpaqueColor();
                forwarLightingPass.DrawTransparent();

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos()) {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                }
#endif

                // PostProcessing

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos()) {
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }
#endif

                // CameraTargetに描画
                RestoreCameraTarget(context, cmd);

                forwarLightingPass.CleanupRT();
                deferredLightingPass.CleanupRT();
                gBufferPass.CleanupRT();
                depthPreDepthPass.CleanupRT();
                if (mainLightIndex >= 0) {
                    shadowPass.CleanupRT();
                }

                CommandBufferPool.Release(cmd);
            }

            context.Submit();
        }

        private int SetupLights(ScriptableRenderContext context, CommandBuffer cmd, CullingResults cullingResults) {
            cmd.Clear();

            // DirectionalLightの探索
            int lightIndex = -1;
            for (int i = 0; i < cullingResults.visibleLights.Length; i++) {
                var tempVisibleLight = cullingResults.visibleLights[i];
                var tempLight = tempVisibleLight.light;

                if (tempLight == null || tempLight.shadows == LightShadows.None || tempLight.shadowStrength <= 0f ||
                    tempLight.type != LightType.Directional) {
                    continue;
                }

                lightIndex = i;
                break;
            }

            if (lightIndex < 0) {
                context.ExecuteCommandBuffer(cmd);
                return -1;
            }

            // ライトのパラメータ設定
            var visibleLight = cullingResults.visibleLights[lightIndex];
            var light = visibleLight.light;

            cmd.SetGlobalColor(CustomCh14Property.LightColorId, light.color * light.intensity);
            cmd.SetGlobalVector(CustomCh14Property.LightDirId, -light.transform.forward);
            cmd.SetGlobalVector(CustomCh14Property.AmbientLightColorId, asset.AmbientLightColor);
            context.ExecuteCommandBuffer(cmd);

            return lightIndex;
        }

        private void RestoreCameraTarget(ScriptableRenderContext context, CommandBuffer cmd) {
            var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            cmd.Clear();
            cmd.SetRenderTarget(cameraTarget);
            cmd.Blit(CustomCh14Property.OffCameraColorTex, cameraTarget);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// カメラの解像度を取得する
        /// </summary>
        private Vector2Int GetCameraResolution(Camera camera) {
            var width = Display.main.renderingWidth;
            var height = Display.main.renderingHeight;
            var targetTexture = camera.targetTexture;
            if (targetTexture != null) {
                width = targetTexture.width;
                height = targetTexture.height;
            }

            return new Vector2Int(width, height);
        }
    }
}