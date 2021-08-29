using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_02 {
    /// <summary>
    /// モノクロのパラメータ.
    /// 参考書ではポストプロセスとして扱われているので,RenderFeatureで設定するのではなく,VolumeComponentとして定義している
    /// </summary>
    [Serializable, VolumeComponentMenu("Custom-Post-Processing/ch10_02/Monochrome")]
    public class CustomMonochrome : VolumeComponent, IPostProcessComponent {
        [Tooltip("元の画像とモノクロ画像の補間値")]
        public ClampedFloatParameter lerp = new ClampedFloatParameter(0f, 0f, 1f);

        public bool IsActive() => lerp.value > Mathf.Epsilon;

        public bool IsTileCompatible() => false;
    }
}