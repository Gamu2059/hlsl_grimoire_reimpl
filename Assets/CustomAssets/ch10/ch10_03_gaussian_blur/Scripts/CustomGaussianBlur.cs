using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_03 {
    /// <summary>
    /// ガウシアンブラーのパラメータ.
    /// 参考書ではポストプロセスとして扱われているので,RenderFeatureで設定するのではなく,VolumeComponentとして定義している
    /// </summary>
    [Serializable, VolumeComponentMenu("Custom-Post-Processing/ch10_03/GaussianBlur")]
    public class CustomGaussianBlur : VolumeComponent, IPostProcessComponent {
        [Tooltip("元の画像とブラー画像の補間値")]
        public ClampedFloatParameter lerp = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("サンプリングする周囲のピクセルの数.(2n+1)^2で増加する.つまり,3の時は49回サンプリングする")]
        public ClampedIntParameter samplingCount = new ClampedIntParameter(1, 1, 3);

        [Tooltip("サンプリングする位置の間隔")]
        public MinFloatParameter samplingSpace = new MinFloatParameter(0.5f, 0f);

        [Tooltip("ブラーの分散")]
        public FloatParameter distribution = new FloatParameter(5f);
        
        [Tooltip("ダウンサンプリングの倍率")]
        public ClampedFloatParameter downSamplingRate = new ClampedFloatParameter(0.5f, 0.1f, 1f);

        [Tooltip("2パス使ってブラーを掛けるかどうか.1パスだと縦横しか考慮されない")]
        public BoolParameter use2pass = new BoolParameter(false);

        public bool IsActive() => lerp.value > Mathf.Epsilon;

        public bool IsTileCompatible() => false;
    }
}