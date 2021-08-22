using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_05 {
    /// <summary>
    /// 川瀬式ブルームのRenderFeature
    /// </summary>
    public class CustomKawaseBloomRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomKawaseBloomPass.CreateParam settings;

        private CustomKawaseBloomPass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomKawaseBloomPass(settings);
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