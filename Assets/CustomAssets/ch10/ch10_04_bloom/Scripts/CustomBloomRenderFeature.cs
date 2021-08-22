using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_04 {
    /// <summary>
    /// ブルームのRenderFeature
    /// </summary>
    public class CustomBloomRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomBloomPass.CreateParam settings;

        private CustomBloomPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomBloomPass(settings);
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