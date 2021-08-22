using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.hlsl_grimoire.ch10_06 {
    /// <summary>
    /// 被写界深度のパラメータ.
    /// 参考書ではポストプロセスとして扱われているので,RenderFeatureで設定するのではなく,VolumeComponentとして定義している
    /// </summary>
    [Serializable, VolumeComponentMenu("Custom-Post-Processing/ch10_06/DepthOfField")]
    public class CustomDepthOfField : VolumeComponent, IPostProcessComponent {
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

        [Tooltip("焦点距離")]
        public MinFloatParameter focalDistance = new MinFloatParameter(5f, 0f);
        
        [Tooltip("焦点が合っているとする幅")]
        public MinFloatParameter focalWidth = new MinFloatParameter(1f, 0f);

        [Tooltip("距離によってブラーが掛かる強さ")]
        public MinFloatParameter blurPower = new MinFloatParameter(0.5f, 0f);
        
        public bool IsActive() => lerp.value > Mathf.Epsilon;

        public bool IsTileCompatible() => false;
    }
}