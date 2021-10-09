using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    /// <summary>
    /// Chapter14で使うレンダーパイプラインのアセット
    /// </summary>
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "Custom/ch14/RenderPipelineAsset", fileName = "ch14_render_pipeline_asset.asset")]
    public class CustomCh14RenderPipelineAsset : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomCh14RenderPipeline();
        }
    }
}