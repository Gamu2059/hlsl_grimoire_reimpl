using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_07 {
    /// <summary>
    /// 六角形被写界深度のRenderFeature
    /// </summary>
    public class CustomHexBokehBlurRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomHexBokehBlurPass.CreateParam settings;

        private CustomHexBokehBlurPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomHexBokehBlurPass(settings);
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