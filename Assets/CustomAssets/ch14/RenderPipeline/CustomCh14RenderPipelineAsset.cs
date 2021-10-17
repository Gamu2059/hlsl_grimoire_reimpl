using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.hlsl_grimoire.ch14
{
    public enum ShadowType {
        Hard,
        PcfSoft,
        VsmSoft,
    }
    
    /// <summary>
    /// Chapter14で使うレンダーパイプラインのアセット
    /// </summary>
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "Custom/ch14/RenderPipelineAsset", fileName = "ch14_render_pipeline_asset.asset")]
    public class CustomCh14RenderPipelineAsset : RenderPipelineAsset {
        [SerializeField]
        private Color ambientLightColor;

        public Color AmbientLightColor => ambientLightColor;
        
        [SerializeField]
        private Vector2Int shadowResolution;

        public Vector2Int ShadowResolution => shadowResolution;

        [SerializeField]
        private ShadowType shadowType;

        public ShadowType ShadowType => shadowType;

        [SerializeField, Range(1, 3)]
        private int pcfSampleCount;

        public int PcfSampleCount => pcfSampleCount;

        [SerializeField]
        private float pcfSampleSpace;

        public float PcfSampleSpace => pcfSampleSpace;

        [SerializeField]
        private int vsmSampleCount;

        public int VsmSampleCount => vsmSampleCount;

        [SerializeField]
        private float vsmSampleSpace;

        public float VsmSampleSpace => vsmSampleSpace;

        [SerializeField]
        private float vsmDistribution;

        public float VsmDistribution => vsmDistribution;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomCh14RenderPipeline(this);
        }
    }
}