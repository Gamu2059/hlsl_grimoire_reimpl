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
        private static readonly ShaderTagId opaqueTag = new ShaderTagId("CustomCh14Opaque");
        private static readonly ShaderTagId transparentTag = new ShaderTagId("CustomCh14Transparent");

        private static readonly int cameraColor = Shader.PropertyToID("CameraColor");
        private static readonly int cameraDepth = Shader.PropertyToID("CameraDepth");

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            for (var i = 0; i < cameras.Length; i++)
            {
                var cmd = CommandBufferPool.Get("CustomCh14RenderPipeline" + i);

                var camera = cameras[i];

                // カメラプロパティをシェーダーのグローバル変数にセット
                // ビュー行列、プロジェクション行列などもセットされる
                context.SetupCameraProperties(camera);

                // カリングパラメータを取得する
                if (!camera.TryGetCullingParameters(out var cullingParameters))
                {
                    // 取得に失敗した場合は描画スキップ
                    continue;
                }

                // カリング
                var cullingResults = context.Cull(ref cullingParameters);

                SetupCameraRT(context, camera, cmd);

                DrawOpaque(context, camera, cullingResults, cmd);

                if (camera.clearFlags == CameraClearFlags.Skybox)
                {
                    context.DrawSkybox(camera);
                }

                DrawTransparent(context, camera, cullingResults, cmd);

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                }
#endif

                // Do Something Post Effect

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                }
#endif

                CleanupCameraRT(context, cmd);

                CommandBufferPool.Release(cmd);
            }
            
            context.Submit();
        }

        /// <summary>
        /// 不透明物体を描画する
        /// </summary>
        private void DrawOpaque(ScriptableRenderContext context, Camera camera, CullingResults cullingResults,
            CommandBuffer cmd)
        {
            cmd.Clear();

            cmd.SetRenderTarget(0);
            context.ExecuteCommandBuffer(cmd);

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            var drawSettings = new DrawingSettings(opaqueTag, sortingSettings);
            var filterSettings = new FilteringSettings
            {
                renderQueueRange = new RenderQueueRange(0, (int) RenderQueue.GeometryLast),
                layerMask = camera.cullingMask,
            };

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        /// <summary>
        /// カメラ用RTをセットアップする
        /// </summary>
        private void SetupCameraRT(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            cmd.Clear();

            var width = Display.main.renderingWidth;
            var height = Display.main.renderingHeight;
            var targetTexture = camera.targetTexture;
            if (targetTexture != null)
            {
                width = targetTexture.width;
                height = targetTexture.height;
            }

            cmd.GetTemporaryRT(cameraColor, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(cameraDepth, width, height, 0, FilterMode.Point, RenderTextureFormat.Depth);

            cmd.SetRenderTarget(color: cameraColor, depth: cameraDepth);
            switch (camera.clearFlags)
            {
                case CameraClearFlags.Depth:
                case CameraClearFlags.Skybox:
                    cmd.ClearRenderTarget(true, false, Color.black, 1f);
                    break;
                case CameraClearFlags.SolidColor:
                    cmd.ClearRenderTarget(true, true, camera.backgroundColor, 1f);
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// カメラ用RTをクリーンアップする
        /// </summary>
        private void CleanupCameraRT(ScriptableRenderContext context, CommandBuffer cmd)
        {
            cmd.Clear();

            cmd.ReleaseTemporaryRT(cameraDepth);
            cmd.ReleaseTemporaryRT(cameraColor);

            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// 半透明物体を描画する
        /// </summary>
        private void DrawTransparent(ScriptableRenderContext context, Camera camera, CullingResults cullingResults,
            CommandBuffer cmd)
        {
            cmd.Clear();

            cmd.SetRenderTarget(0);
            context.ExecuteCommandBuffer(cmd);

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            var drawSettings = new DrawingSettings(transparentTag, sortingSettings);
            var filterSettings = new FilteringSettings
            {
                renderQueueRange =
                    new RenderQueueRange((int) RenderQueue.GeometryLast + 1, (int) RenderQueue.Transparent),
                layerMask = camera.cullingMask,
            };

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }
    }
}