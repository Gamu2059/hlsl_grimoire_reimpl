using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// ガウシアンブラーのRenderFeature
    /// </summary>
    public class CustomGaussianBlurRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomGaussianBlurPass.CreateParam settings;

        private CustomGaussianBlurPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomGaussianBlurPass(settings);
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