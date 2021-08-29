using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// 平均ブラーのRenderFeature
    /// </summary>
    public class CustomAvgBlurRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomAvgBlurPass.CreateParam settings;

        private CustomAvgBlurPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomAvgBlurPass(settings);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) {
            pass = null;
            base.Dispose(disposing);
        }
    }
}