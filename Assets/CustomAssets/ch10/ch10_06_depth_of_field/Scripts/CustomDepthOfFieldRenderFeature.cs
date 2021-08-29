using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_06 {
    /// <summary>
    /// 被写界深度のRenderFeature
    /// </summary>
    public class CustomDepthOfFieldRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomDepthOfFieldPass.CreateParam settings;

        private CustomDepthOfFieldPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomDepthOfFieldPass(settings);
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