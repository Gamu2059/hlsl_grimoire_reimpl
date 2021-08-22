Shader "Hidden/hlsl_grimoire/sample10_06/depth_of_field"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Library/PackageCache/com.unity.render-pipelines.universal@10.5.0/Shaders/PostProcessing/Common.hlsl"
    CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    CBUFFER_END

    Varyings vert(Attributes i)
    {
        Varyings o;
        o.positionCS = TransformObjectToHClip(i.positionOS);
        o.uv = TRANSFORM_TEX(i.uv, _MainTex);
        return o;
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Off
        Blend One Zero

        Pass
        {
            Name "2pass Depth Of Field X Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[x];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[x];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Depth Of Field Y Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[y];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[y];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Depth Of Field Combine"
            // 加算合成なので、Blendの指定を忘れずに
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // カメラの深度テクスチャを取得(URP固有)
            TEXTURE2D_X(_CameraDepthTexture);

            float _FocalDistance;
            float _FocalWidth;
            float _BlurPower;
            float _Lerp;

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv);
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_LinearClamp, i.uv), _ZBufferParams);
                float blurLerp = saturate(max(0, abs(depth - _FocalDistance) - _FocalWidth) * _BlurPower);
                color.a = blurLerp * _Lerp;
                return color;
            }
            ENDHLSL
        }
    }
}