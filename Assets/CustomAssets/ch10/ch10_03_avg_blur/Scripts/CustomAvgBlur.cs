using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// 平均ブラーのパラメータ.
    /// 参考書ではポストプロセスとして扱われているので,RenderFeatureで設定するのではなく,VolumeComponentとして定義している
    /// </summary>
    [Serializable, VolumeComponentMenu("Custom-Post-Processing/ch10_03/AveBlur")]
    public class CustomAvgBlur : VolumeComponent, IPostProcessComponent {
        [Tooltip("元の画像とブラー画像の補間値")]
        public ClampedFloatParameter lerp = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("サンプリングする周囲のピクセルの数.(2n+1)^2で増加する.つまり,3の時は49回サンプリングする")]
        public ClampedIntParameter samplingCount = new ClampedIntParameter(1, 1, 3);

        [Tooltip("サンプリングする位置の間隔")]
        public MinFloatParameter samplingSpace = new MinFloatParameter(0.5f, 0f);

        public bool IsActive() => lerp.value > Mathf.Epsilon;

        public bool IsTileCompatible() => false;
    }
}