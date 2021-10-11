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
        [SerializeField]
        private Vector2Int shadowResolution;

        public Vector2Int ShadowResolution => shadowResolution;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomCh14RenderPipeline(this);
        }
    }
}