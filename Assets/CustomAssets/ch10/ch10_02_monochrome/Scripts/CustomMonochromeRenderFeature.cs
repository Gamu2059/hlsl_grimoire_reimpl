using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_02 {
    /// <summary>
    /// モノクロのRenderFeature
    /// </summary>
    public class CustomMonochromeRenderFeature : ScriptableRendererFeature {
        [SerializeField]
        private CustomMonochromePass.CreateParam settings;

        private CustomMonochromePass pass;

        public override void Create() {
            if (pass == null) {
                pass = new CustomMonochromePass(settings);
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